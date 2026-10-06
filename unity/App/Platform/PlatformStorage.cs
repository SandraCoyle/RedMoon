using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RedMoon.UnityApp.Platform
{
    /// <summary>
    /// Hvor appen gemmer filer på hver platform.
    /// - Data (krypteret datafil + nøgler på Android): appens PRIVATE mappe, som andre apps ikke kan læse.
    /// - Overførselsfiler: en mappe brugeren selv kan finde (fx via computerens Stifinder i Unity Editor).
    /// </summary>
    internal static class PlatformStorage
    {
        [DllImport("__Internal")] private static extern void RedMoonFile_Protect(string path);

        /// <summary>Mappe til den krypterede datafil.</summary>
        public static string DataDirectory()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    // Application.persistentDataPath kan pege på "ekstern" lagring. Den interne mappe er mere privat.
                    var internalDir = AndroidInternalFilesDir();
                    return Path.Combine(internalDir ?? Application.persistentDataPath, "RedMoon");

                case RuntimePlatform.IPhonePlayer:
                    // persistentDataPath er "Documents" (synlig i Filer-appen og med i backup). Library er privat.
                    var container = Path.GetDirectoryName(Application.persistentDataPath) ?? Application.persistentDataPath;
                    return Path.Combine(container, "Library", "Application Support", "RedMoon");

                default:
                    return Path.Combine(Application.persistentDataPath, "RedMoon");
            }
        }

        /// <summary>Mappe hvor overførselsfiler lægges (eksport) og findes (import).</summary>
        public static string TransferDirectory() => Path.Combine(Application.persistentDataPath, "Overfoersel");

        /// <summary>Sand hvis platformen kan åbne en mappe i systemets filhåndtering (computer/Editor).</summary>
        public static bool CanOpenFolder =>
            Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer;

        /// <summary>Åbner en mappe i Stifinder/Finder (kun computer/Editor).</summary>
        public static void OpenFolder(string directory)
        {
            Directory.CreateDirectory(directory);
            Application.OpenURL(new Uri(directory).AbsoluteUri);
        }

        /// <summary>
        /// Kaldes efter hver skrivning af datafilen.
        /// iOS: udelukker filen fra iCloud-backup og slår NSFileProtectionComplete til (via RedMoonKeychain.mm).
        /// </summary>
        public static void AfterWrite(string path)
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer) return;
            try
            {
                RedMoonFile_Protect(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Kunne ikke beskytte datafilen: " + ex.Message);
            }
        }

        private static string? AndroidInternalFilesDir()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var plugin = new AndroidJavaClass("dk.roedmaane.unity.RedMoonAndroid"))
                {
                    return plugin.CallStatic<string>("internalFilesDir", activity);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Kunne ikke finde Androids interne mappe: " + ex.Message);
                return null;
            }
        }
    }
}
