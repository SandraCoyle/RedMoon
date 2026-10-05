using System;
using System.IO;
using System.Security.Cryptography;
using RedMoon.Core.Common;

namespace RedMoon.Core.Security
{
    /// <summary>
    /// Autentificeret kryptering: AES-256-CBC + HMAC-SHA256 (encrypt-then-MAC).
    ///
    /// Hvorfor ikke AES-GCM? AesGcm findes ikke i .NET Standard 2.1 / Unity.
    /// Encrypt-then-MAC med separate nøgler er en veldokumenteret, sikker konstruktion
    /// (se fx RFC 7518 afsnit 5.2, "AES_CBC_HMAC_SHA2").
    ///
    /// Format: [header (valgfri, autentificeret)] [IV 16 bytes] [ciffertekst] [HMAC 32 bytes]
    /// HMAC beregnes over header + IV + ciffertekst og verificeres FØR dekryptering.
    /// </summary>
    public sealed class AuthenticatedCipher
    {
        public const int KeyLength = 32;
        private const int IvLength = 16;
        private const int MacLength = 32;

        private readonly byte[] _encryptionKey;
        private readonly byte[] _macKey;

        /// <summary>Opretter en cipher med to uafhængige 256-bit nøgler.</summary>
        public AuthenticatedCipher(byte[] encryptionKey, byte[] macKey)
        {
            if (encryptionKey == null || encryptionKey.Length != KeyLength) throw new ArgumentException("Krypteringsnøglen skal være 32 bytes.", nameof(encryptionKey));
            if (macKey == null || macKey.Length != KeyLength) throw new ArgumentException("MAC-nøglen skal være 32 bytes.", nameof(macKey));
            _encryptionKey = (byte[])encryptionKey.Clone();
            _macKey = (byte[])macKey.Clone();
        }

        /// <summary>
        /// Afleder krypterings- og MAC-nøgle fra én 256-bit hovednøgle med HMAC-SHA256
        /// og forskellige formålsetiketter (simpel KDF; HKDF findes ikke i .NET Standard 2.1).
        /// </summary>
        public static AuthenticatedCipher FromMasterKey(byte[] masterKey)
        {
            if (masterKey == null || masterKey.Length != KeyLength) throw new ArgumentException("Hovednøglen skal være 32 bytes.", nameof(masterKey));
            using (var hmac = new HMACSHA256(masterKey))
            {
                var encKey = hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes("redmoon-vault-enc-v1"));
                var macKey = hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes("redmoon-vault-mac-v1"));
                return new AuthenticatedCipher(encKey, macKey);
            }
        }

        /// <summary>Opretter en cipher fra 64 bytes nøglemateriale (fx PBKDF2-output).</summary>
        public static AuthenticatedCipher FromKeyMaterial(byte[] keyMaterial)
        {
            if (keyMaterial == null || keyMaterial.Length != KeyLength * 2) throw new ArgumentException("Nøglematerialet skal være 64 bytes.", nameof(keyMaterial));
            var encKey = new byte[KeyLength];
            var macKey = new byte[KeyLength];
            Buffer.BlockCopy(keyMaterial, 0, encKey, 0, KeyLength);
            Buffer.BlockCopy(keyMaterial, KeyLength, macKey, 0, KeyLength);
            return new AuthenticatedCipher(encKey, macKey);
        }

        /// <summary>Genererer en ny tilfældig 256-bit nøgle med kryptografisk sikker tilfældighed.</summary>
        public static byte[] GenerateKey()
        {
            var key = new byte[KeyLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        /// <summary>Krypterer <paramref name="plaintext"/>. Header'en krypteres ikke, men beskyttes mod ændringer.</summary>
        public byte[] Encrypt(byte[] plaintext, byte[] header)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
            header = header ?? Array.Empty<byte>();

            byte[] iv = new byte[IvLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            byte[] cipherText;
            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                {
                    cipherText = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                }
            }

            using (var output = new MemoryStream())
            {
                output.Write(header, 0, header.Length);
                output.Write(iv, 0, iv.Length);
                output.Write(cipherText, 0, cipherText.Length);
                var mac = ComputeMac(output.ToArray());
                output.Write(mac, 0, mac.Length);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Verificerer og dekrypterer. Kaster VaultCorruptedException hvis data er ændret,
        /// nøglen er forkert, eller header ikke matcher.
        /// </summary>
        public byte[] Decrypt(byte[] data, byte[] header)
        {
            header = header ?? Array.Empty<byte>();
            if (data == null || data.Length < header.Length + IvLength + 16 + MacLength)
            {
                throw new VaultCorruptedException("Filen er for kort eller ødelagt.");
            }

            for (var i = 0; i < header.Length; i++)
            {
                if (data[i] != header[i]) throw new VaultCorruptedException("Filen har et ukendt format.");
            }

            var authenticatedLength = data.Length - MacLength;
            var authenticated = new byte[authenticatedLength];
            Buffer.BlockCopy(data, 0, authenticated, 0, authenticatedLength);
            var mac = new byte[MacLength];
            Buffer.BlockCopy(data, authenticatedLength, mac, 0, MacLength);

            if (!ConstantTime.AreEqual(ComputeMac(authenticated), mac))
            {
                throw new VaultCorruptedException("Data kunne ikke verificeres. Forkert nøgle/kode, eller filen er ændret.");
            }

            var iv = new byte[IvLength];
            Buffer.BlockCopy(data, header.Length, iv, 0, IvLength);
            var cipherOffset = header.Length + IvLength;
            var cipherLength = authenticatedLength - cipherOffset;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(data, cipherOffset, cipherLength);
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new VaultCorruptedException("Data kunne ikke dekrypteres.", ex);
            }
        }

        private byte[] ComputeMac(byte[] data)
        {
            using (var hmac = new HMACSHA256(_macKey))
            {
                return hmac.ComputeHash(data);
            }
        }
    }
}
