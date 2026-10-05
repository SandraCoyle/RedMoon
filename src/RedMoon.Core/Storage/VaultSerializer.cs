using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RedMoon.Core.Common;
using RedMoon.Core.Models;

namespace RedMoon.Core.Storage
{
    /// <summary>
    /// Omsætter UserVault til/fra bytes i et simpelt, versioneret binært format.
    ///
    /// Hvorfor ikke JSON? Et eksplicit binært format kræver ingen tredjepartsbiblioteker
    /// (virker identisk i MAUI og Unity), er typesikkert og gemmer præcis de felter der står her – intet andet.
    /// Alt valideres ved indlæsning, så en ødelagt fil afvises i stedet for at give forkerte data.
    /// </summary>
    public static class VaultSerializer
    {
        private const int FormatVersion = 1;
        private const int MaxUsernameBytes = 256;
        private const int MaxEntries = 100_000;

        /// <summary>Serialiserer en vault. Når <paramref name="includeSecurityState"/> er false, udelades session og spærring.</summary>
        public static byte[] Serialize(UserVault vault, bool includeSecurityState = true)
        {
            if (vault == null) throw new ArgumentNullException(nameof(vault));

            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory, Encoding.UTF8))
            {
                writer.Write(FormatVersion);

                // --- Konto ---
                var account = vault.Account;
                writer.Write(account.Username);
                WriteBytes(writer, account.PasswordHash.Salt);
                WriteBytes(writer, account.PasswordHash.Hash);
                writer.Write(account.PasswordHash.Iterations);
                writer.Write(account.BirthYear);
                writer.Write(account.BirthMonth);

                // --- Sikkerhedstilstand ---
                var security = vault.Security;
                writer.Write(includeSecurityState ? security.FailedLoginAttempts : 0);
                writer.Write(includeSecurityState ? security.LockoutUntilUtc.Ticks : DateTime.MinValue.Ticks);
                WriteBytes(writer, includeSecurityState ? security.SessionTokenHash : Array.Empty<byte>());

                // --- Daglige registreringer ---
                writer.Write(vault.EntryCount);
                foreach (var entry in vault.Entries)
                {
                    writer.Write(entry.Date.DayNumber);
                    writer.Write((byte)entry.Mood);
                    writer.Write((byte)entry.MenstruationStatus);
                    writer.Write((byte)entry.FlowIntensity);
                }

                // --- Menstruationer (startdato, slutdato, længde) ---
                writer.Write(vault.Periods.Count);
                foreach (var period in vault.Periods)
                {
                    writer.Write(period.Start.DayNumber);
                    writer.Write(period.End.DayNumber);
                    writer.Write(period.LengthDays);
                    writer.Write(period.EndConfirmed);
                }

                writer.Flush();
                return memory.ToArray();
            }
        }

        /// <summary>Genskaber en vault. Kaster VaultCorruptedException ved ugyldigt indhold.</summary>
        public static UserVault Deserialize(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                using (var memory = new MemoryStream(data))
                using (var reader = new BinaryReader(memory, Encoding.UTF8))
                {
                    var version = reader.ReadInt32();
                    if (version != FormatVersion) throw new VaultCorruptedException($"Ukendt dataversion: {version}.");

                    var username = reader.ReadString();
                    if (Encoding.UTF8.GetByteCount(username) > MaxUsernameBytes) throw new VaultCorruptedException("Ugyldigt brugernavn i data.");
                    var salt = ReadBytes(reader, 1024);
                    var hash = ReadBytes(reader, 1024);
                    var iterations = reader.ReadInt32();
                    var birthYear = reader.ReadInt32();
                    var birthMonth = reader.ReadInt32();
                    if (birthMonth < 1 || birthMonth > 12 || birthYear < 1900 || birthYear > 9999)
                    {
                        throw new VaultCorruptedException("Ugyldig fødselsdato i data.");
                    }

                    var account = new UserAccount(username, new Models.PasswordHash(salt, hash, iterations), birthYear, birthMonth);
                    var vault = new UserVault(account);

                    vault.Security.FailedLoginAttempts = Math.Max(0, reader.ReadInt32());
                    var lockoutTicks = reader.ReadInt64();
                    if (lockoutTicks < DateTime.MinValue.Ticks || lockoutTicks > DateTime.MaxValue.Ticks) throw new VaultCorruptedException("Ugyldig spærretid.");
                    vault.Security.LockoutUntilUtc = new DateTime(lockoutTicks, DateTimeKind.Utc);
                    vault.Security.SessionTokenHash = ReadBytes(reader, 64);

                    var entryCount = reader.ReadInt32();
                    if (entryCount < 0 || entryCount > MaxEntries) throw new VaultCorruptedException("Ugyldigt antal registreringer.");
                    for (var i = 0; i < entryCount; i++)
                    {
                        var date = ReadDate(reader);
                        var mood = ReadEnum<Mood>(reader.ReadByte());
                        var status = ReadEnum<MenstruationStatus>(reader.ReadByte());
                        var flow = ReadEnum<FlowIntensity>(reader.ReadByte());
                        vault.SetEntry(new DailyEntry(date, mood, status, flow));
                    }

                    var periodCount = reader.ReadInt32();
                    if (periodCount < 0 || periodCount > MaxEntries) throw new VaultCorruptedException("Ugyldigt antal menstruationer.");
                    var periods = new List<MenstruationPeriod>(periodCount);
                    for (var i = 0; i < periodCount; i++)
                    {
                        var start = ReadDate(reader);
                        var end = ReadDate(reader);
                        var length = reader.ReadInt32();
                        var endConfirmed = reader.ReadBoolean();
                        if (end < start || length != end.DaysSince(start) + 1) throw new VaultCorruptedException("Ugyldig menstruationsperiode.");
                        periods.Add(new MenstruationPeriod(start, end, endConfirmed));
                    }
                    vault.SetPeriods(periods);

                    if (memory.Position != memory.Length) throw new VaultCorruptedException("Uventede ekstra data i filen.");
                    return vault;
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new VaultCorruptedException("Data er afkortet.", ex);
            }
            catch (ArgumentException ex)
            {
                throw new VaultCorruptedException("Data indeholder ugyldige værdier.", ex);
            }
            catch (FormatException ex)
            {
                throw new VaultCorruptedException("Data har et ugyldigt format.", ex);
            }
        }

        private static void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static byte[] ReadBytes(BinaryReader reader, int maxLength)
        {
            var length = reader.ReadInt32();
            if (length < 0 || length > maxLength) throw new VaultCorruptedException("Ugyldig feltlængde.");
            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new VaultCorruptedException("Data er afkortet.");
            return bytes;
        }

        private static LocalDate ReadDate(BinaryReader reader)
        {
            var dayNumber = reader.ReadInt32();
            if (dayNumber < 0 || dayNumber > LocalDate.MaxDayNumber) throw new VaultCorruptedException("Ugyldig dato.");
            return LocalDate.FromDayNumber(dayNumber);
        }

        private static TEnum ReadEnum<TEnum>(byte value) where TEnum : struct, Enum
        {
            var boxed = (TEnum)Enum.ToObject(typeof(TEnum), value);
            if (!Enum.IsDefined(typeof(TEnum), boxed)) throw new VaultCorruptedException("Ugyldig værdi i data.");
            return boxed;
        }
    }
}
