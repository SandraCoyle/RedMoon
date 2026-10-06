using System;
using UnityEngine;

namespace RedMoon.UnityApp.Platform
{
    /// <summary>
    /// Beskytter skærmindholdet:
    /// - Android: FLAG_SECURE (skjuler appen i "seneste apps" og blokerer skærmbilleder).
    /// - Alle platforme: et dække vises når appen mister fokus (app-switcher på iOS).
    /// </summary>
    internal static class PrivacyGuard
    {
        /// <summary>Slår Androids FLAG_SECURE til. Gør intet på andre platforme.</summary>
        public static void EnableSecureWindow()
        {
            if (Application.platform != RuntimePlatform.Android) return;
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var plugin = new AndroidJavaClass("dk.roedmaane.unity.RedMoonAndroid"))
                {
                    plugin.CallStatic("enableSecureWindow", activity);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Kunne ikke slå FLAG_SECURE til: " + ex.Message);
            }
        }
    }
}
