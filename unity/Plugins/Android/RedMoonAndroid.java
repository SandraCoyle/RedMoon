package dk.roedmaane.unity;

import android.app.Activity;
import android.content.Context;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.view.WindowManager;

import java.security.KeyStore;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Rød Måne – Android-hjælpefunktioner til Unity-versionen.
 *
 * - encrypt/decrypt: AES-256-GCM med en nøgle der ligger i Android Keystore og aldrig forlader telefonen.
 *   Bruges til at beskytte appens krypteringsnøgle og session-token.
 * - internalFilesDir: appens private mappe (andre apps kan ikke læse den).
 * - enableSecureWindow: FLAG_SECURE (ingen skærmbilleder, skjult i "seneste apps").
 *
 * Kræver Android 6.0 (API 23) eller nyere.
 */
public final class RedMoonAndroid {
    private static final String KEY_ALIAS = "redmoon.wrap.v1";
    private static final String PROVIDER = "AndroidKeyStore";
    private static final String TRANSFORMATION = "AES/GCM/NoPadding";
    private static final int TAG_BITS = 128;

    private RedMoonAndroid() {
    }

    /** Appens private mappe, fx /data/user/0/dk.roedmaane.app/files. */
    public static String internalFilesDir(Context context) {
        return context.getFilesDir().getAbsolutePath();
    }

    /** Slår FLAG_SECURE til på aktivitetens vindue. */
    public static void enableSecureWindow(final Activity activity) {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                activity.getWindow().addFlags(WindowManager.LayoutParams.FLAG_SECURE);
            }
        });
    }

    /** Krypterer. Resultat: [IV-længde (1 byte)] [IV] [ciffertekst + GCM-tag]. */
    public static byte[] encrypt(byte[] plain) throws Exception {
        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.ENCRYPT_MODE, getOrCreateKey());
        byte[] iv = cipher.getIV();
        byte[] cipherText = cipher.doFinal(plain);

        byte[] result = new byte[1 + iv.length + cipherText.length];
        result[0] = (byte) iv.length;
        System.arraycopy(iv, 0, result, 1, iv.length);
        System.arraycopy(cipherText, 0, result, 1 + iv.length, cipherText.length);
        return result;
    }

    /** Dekrypterer data lavet af encrypt. Kaster en fejl hvis data er ændret eller nøglen mangler. */
    public static byte[] decrypt(byte[] blob) throws Exception {
        if (blob == null || blob.length < 2) {
            throw new IllegalArgumentException("Ugyldige data");
        }
        int ivLength = blob[0] & 0xFF;
        if (blob.length < 1 + ivLength) {
            throw new IllegalArgumentException("Ugyldige data");
        }
        Cipher cipher = Cipher.getInstance(TRANSFORMATION);
        cipher.init(Cipher.DECRYPT_MODE, getOrCreateKey(), new GCMParameterSpec(TAG_BITS, blob, 1, ivLength));
        return cipher.doFinal(blob, 1 + ivLength, blob.length - 1 - ivLength);
    }

    /** Sletter nøglen i Android Keystore (bruges ikke til daglig – sletning af data fjerner de krypterede filer). */
    public static void deleteKey() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(PROVIDER);
        keyStore.load(null);
        if (keyStore.containsAlias(KEY_ALIAS)) {
            keyStore.deleteEntry(KEY_ALIAS);
        }
    }

    private static synchronized SecretKey getOrCreateKey() throws Exception {
        KeyStore keyStore = KeyStore.getInstance(PROVIDER);
        keyStore.load(null);
        if (keyStore.containsAlias(KEY_ALIAS)) {
            return (SecretKey) keyStore.getKey(KEY_ALIAS, null);
        }

        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, PROVIDER);
        generator.init(new KeyGenParameterSpec.Builder(KEY_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)
                .build());
        return generator.generateKey();
    }
}
