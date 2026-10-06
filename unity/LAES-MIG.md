# Rød Måne i Unity

Hele appen (login, opret bruger, hjem med månen, kalender, profil, skift mønster, overførsel) i Unity.
Logikken er **præcis den samme kode** som i mobilappen (RedMoon.Core). Kun brugerfladen er lavet til Unity (UI Toolkit).

## Kør appen (ca. 5 minutter)

1. Installér **Unity Hub** og en Unity-version: **Unity 6 (LTS)** anbefales, **2021.3 LTS** er minimum.
2. Unity Hub → **New project** → skabelonen **2D** (eller "Universal 2D") → **Create project**.
3. Pak `RedMoon-Unity.zip` ud, og **træk mappen `RedMoon`** ind i Unitys **Project**-vindue under `Assets`.
   Vent til Unity er færdig med at kompilere (spinneren nederst til højre).
4. Vælg en telefon-størrelse i **Game**-vinduet: klik på størrelses-menuen (fx "Free Aspect") → `+` →
   bredde `1080`, højde `2400`.
5. Tryk **Play**. Appen starter af sig selv – der skal ikke sættes noget op i scenen.

**Reagerer knapperne ikke?** Projektet bruger kun det nye Input System. Gå til
*Edit → Project Settings → Player → Other Settings → Active Input Handling* og vælg **Both**.

## Hvor ligger data?

| Platform | Datafil | Nøgle |
|---|---|---|
| Unity Editor / computer | `Application.persistentDataPath/RedMoon` | Almindelig fil (**kun til test**) |
| Android | Appens interne mappe (`/data/data/<app>/files/RedMoon`) | Krypteret med en nøgle i **Android Keystore** |
| iPhone | `Library/Application Support/RedMoon`, udelukket fra iCloud-backup | **iOS Keychain** (kun denne enhed) |

Profilsiden viser, hvilket nøglelager der bruges. I Editoren står der "Udviklingstilstand".

## Byg til telefon

- **Android:** *File → Build Settings → Android → Switch Platform*. Under *Player Settings*:
  - *Minimum API Level* **23** eller højere (kræves af Android Keystore).
  - *Internet Access* = **Auto** (appen bruger ikke netværk, så tilladelsen tilføjes ikke).
- **iPhone:** Kræver en Mac med Xcode og en Apple-udviklerkonto (se hoved-README).

## Privatliv i Unity – slå det her fra

Unity kan selv indsamle data. Rød Måne må ikke sende noget, så slå det fra før du bygger:

- *Window → General → Services*: sørg for at **Analytics**, **Cloud Diagnostics** og andre tjenester er slået **fra**.
- *Project Settings → Player → Other Settings*: fjern flueben ved **Enable Crash Reporting**/**Cloud Diagnostics**, hvis de findes.
- *Window → Package Manager*: fjern **Analytics**-pakker, hvis skabelonen har dem med.

## Kendte begrænsninger (Unity-udgaven)

- **Ikke testet i Unity af udvikleren.** Koden er kompileret mod Unitys officielle 2021.3-referencer uden fejl,
  men aldrig kørt i Unity Editor eller på en telefon. Den første kørsel er den rigtige test.
- **Android Keystore- og iOS Keychain-koden er ikke testet på telefon.** Fejler Android Keystore, bruger appen en
  almindelig fil og viser en advarsel på profilsiden.
- **Overførsel til ny telefon:** Unity har ingen indbygget filvælger eller "Del"-knap. Filen lægges i appens
  overførselsmappe (stien vises i appen). På computer kan mappen åbnes direkte; på telefon kræver det et
  plugin (fx NativeShare/NativeFilePicker) for at gøre det let.
- **Kun mørkt tema.** Unity kan ikke læse telefonens lys/mørk-indstilling uden plugin.
- **Login kan tage nogle sekunder** første gang: mønsteret hashes med 600.000 PBKDF2-iterationer, og Unitys
  runtime er langsommere end .NET. Er det for langsomt, kan `SecurityOptions.PasswordIterations` sænkes i
  `App/Bootstrap/AppServices.cs` (det svækker beskyttelsen tilsvarende).

## Tilføj en ny skærm

1. Lav en klasse i `App/Screens/` der arver fra `ScreenBase`.
2. **I bundmenuen:** tilføj én linje i `App/Navigation/TabRegistry.cs`.
   **Som underside:** `Context.Navigator.Push(new MinSkærm(Context));`
