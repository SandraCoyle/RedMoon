# Rød Måne 🌙

En privat dagbog/kalender til iOS og Android, hvor brugeren registrerer dagens humør og sin menstruation.
**Privat. Lokal. Simpel. Offline. Kun de nødvendige data.**

---

## 1. Teknologivalg

| Del | Valg | Hvorfor |
|---|---|---|
| App (iOS + Android) | **.NET MAUI 10 (C#, XAML)** | Kravet er C# og objektorienteret kode, og appen skal være en rigtig, native mobilapp. MAUI er Microsofts officielle framework til netop dét: én kodebase, native kontroller, dark mode og indbygget adgang til Keychain/Keystore (`SecureStorage`). |
| Forretningslogik | **RedMoon.Core** – ren C#-klassebibliotek (`netstandard2.1` + `net10.0`) | Al logik (datamodel, kryptering, login, cyklusberegning) ligger her uden UI og **uden eksterne afhængigheder**. Det gør koden testbar og gør det muligt at importere den i **Unity**. |
| Tests | xUnit | Standard i .NET. |

**Unity:** Unity er en spilmotor; en dagbogsapp med formularer, kalender og Keychain/Keystore er enklere og mere robust i MAUI. Derfor er MAUI hovedappen. Der findes også en komplet Unity-udgave oven på samme Core-kode (afsnit 12). Ulempen er to brugerflader, der skal holdes ens.

---

## 2. Begrænsninger ved et rent lokalt login (læs før brug)

Fordi der ingen server er, gælder følgende. Det er *fakta om designet*, ikke fejl:

1. **Ingen "glemt adgangskode".** Mønsteret kan ikke gendannes. Glemmer brugeren det, er den eneste vej videre at slette alle data ("Glemt mønster?" på login-skærmen). Den eneste backup er brugerens egen overførselsfil.
2. **Login beskytter mod andre der låner telefonen, ikke mod en angriber med fuld adgang til telefonen.** Mønsteret er en app-lås. Selve krypteringen hviler på en tilfældig nøgle i telefonens Keychain/Keystore (se afsnit 7).
3. **Et 4-punkts mønster er svagt.** Der findes kun 3.024 mønstre med 4 punkter (9·8·7·6). Får en angriber fat i både datafilen og nøglen, kan alle mønstre afprøves på under et sekund, uanset hashing. Jeg har derfor tilladt **4–9 punkter** (minimum 4 som ønsket). Med 6 punkter er der 60.480, med 9 punkter 362.880. Det hjælper, men er stadig langt fra en rigtig adgangskode.
4. **Spærring efter forkerte forsøg kan omgås** ved at stille telefonens ur frem. Den stopper hurtig gætning i appen, men er ikke en kryptografisk garanti.
5. **Én konto per installation.** Flere personer på samme telefon kræver flere installationer/telefonprofiler.
6. **Mistet/ødelagt telefon = mistede data**, medmindre der er lavet en overførselsfil. Data ligger bevidst ikke i iCloud/Google-backup.
7. **Husket login:** Brugeren forbliver logget ind til der trykkes "Log ud". Enhver der kan låse telefonen op, kan så åbne appen. Det er en bevidst afvejning, fordi det blev bedt om.

---

## 3. Mappestruktur

```
RedMoon/
├── RedMoon.sln
├── Directory.Build.props            Fælles build-indstillinger
├── src/
│   ├── RedMoon.Core/                Ren C#: ingen UI, ingen afhængigheder (Unity-kompatibel)
│   │   ├── Common/                  IClock, fejltyper
│   │   ├── Models/                  LocalDate, UserAccount, DailyEntry, MenstruationPeriod, UserVault, enums
│   │   ├── Security/                PatternPassword, PasswordHasher (PBKDF2), AuthenticatedCipher (AES+HMAC), TransferCode
│   │   ├── Storage/                 IFileStore, ISecureKeyStore, VaultSerializer, VaultRepository
│   │   └── Services/                AccountService, DiaryService, CyclePredictor, PeriodBuilder, TransferService
│   └── RedMoon.App/                 .NET MAUI-appen
│       ├── MauiProgram.cs           Dependency injection (her registreres nye sider)
│       ├── AppShell.cs              Bundmenuen (bygges fra AppTabRegistry)
│       ├── Navigation/              AppTabRegistry (menupunkter), NavigationService
│       ├── Views/                   Login, Opret, Import, Hjem, Kalender, Profil, Skift mønster, Overfør
│       ├── ViewModels/              Én ViewModel per skærm (MVVM)
│       ├── Controls/                MoonView, PatternLockView, MonthCalendarView, ChoiceGroupView
│       ├── Infrastructure/          Keychain/Keystore-adapter, dialoger, privatlivsskærm, filbeskyttelse
│       ├── Presentation/            Danske tekster og emojis
│       ├── Theme/Palette.cs         Alle farver (lys + mørk)
│       ├── Resources/               Styles, ikoner, skrifttyper, splash
│       └── Platforms/               Android- og iOS-specifik kode og manifester
├── tests/RedMoon.Core.Tests/        83 xUnit-tests
├── unity/                           Unity-udgaven: App/ (skærme), Plugins/ (Keychain/Keystore), CompileCheck/, LAES-MIG.md
└── tools/                           build-unity-app.sh (Unity-zip), export-unity-package.sh (kun Core)
```

---

## 4. Datastruktur

Alt hvad appen gemmer, ligger i ét objekt (`UserVault`), som krypteres som én fil:

```
UserAccount
  username          string  (valgfrit, må være tomt, maks 24 tegn)
  passwordHash      { salt (16 B), hash (32 B), iterations }   ← aldrig selve mønsteret
  birthYear         int
  birthMonth        int (1-12)

DailyEntry          (én per dato med registrering)
  date              LocalDate (år-måned-dag, ingen tidszone)
  mood              None | Udadvendt | Glad | Midtimellem | Trist | Indadvendt | Powerful
  menstruationStatus None | FirstDay ("Ja, første dag") | Ongoing ("Har mens") | LastDay ("Afslut")
  flowIntensity     None | Lidt | Noget | Meget   (kun når der er menstruation)

MenstruationPeriod  (bygges automatisk ud fra DailyEntry og gemmes)
  start, end, lengthDays, endConfirmed

SecurityState       (nødvendig for sikkert login, ingen personoplysninger)
  failedLoginAttempts, lockoutUntilUtc, sessionTokenHash
```

Bemærk: Du skrev "fx 5 valgmuligheder", men listede 6 humør. Jeg har brugt alle 6.

**Fra dage til menstruationer:** "Første dag" starter altid en ny menstruation, og "Sidste dag" afslutter den. "Efterfølgende dag" forlænger den igangværende menstruation, medmindre der er gået mere end 3 dage siden sidste registrerede menstruationsdag, eller menstruationen allerede har varet 14 dage. Så starter en ny. Op til to glemte dage midt i en menstruation tæller altså stadig med.

**Forudsigelse:** Cykluslængde = gennemsnittet af dagene mellem de seneste op til 6 menstruationsstarter. Menstruationslængde = gennemsnittet af de seneste op til 6 menstruationer, hvor "Sidste dag" er registreret. Uden egne data bruges 28/5 dage. Usikkerheden (± dage) er standardafvigelsen på cykluslængderne, begrænset til 1–7 dage. Kalenderen viser forventede dage (stærk farve) og usikkerhedsmargen (svag farve) 12 cyklusser frem. Cyklusser under 15 eller over 60 dage ignoreres som fejlregistreringer.
> Bemærk: Hos unge varierer cyklussen meget. En cyklus på 21–45 dage regnes som normal de første år efter første menstruation (ACOG Committee Opinion No. 651, *Menstruation in Girls and Adolescents: Using the Menstrual Cycle as a Vital Sign*, 2015). Forudsigelserne er skøn og **ikke prævention**. Det står også i appen.

---

## 5. Installation

### Forudsætninger
- **.NET 10 SDK**: https://dotnet.microsoft.com/download
- MAUI-workload: `dotnet workload install maui` (eller `maui-android` på Linux)
- **Android:** Android SDK + emulator. Nemmest via Visual Studio 2022/2026 (Windows) eller Android Studio. Kør evt. `dotnet build -t:InstallAndroidDependencies -f net10.0-android "-p:AndroidSdkDirectory=<sti>" -p:AcceptAndroidSDKLicenses=True`.
- **iOS:** En Mac med Xcode (den version din .NET MAUI-version kræver) og en Apple-udviklerkonto for at køre på en fysisk iPhone.
- IDE (valgfrit): Visual Studio (Windows), JetBrains Rider (Win/Mac/Linux) eller VS Code med ".NET MAUI"-udvidelsen.

```bash
git clone <repo-url> RedMoon
cd RedMoon
dotnet restore
dotnet test tests/RedMoon.Core.Tests     # kør testene
```

### Kør på Android
```bash
# Start en emulator (eller tilslut en telefon med USB-debugging), og kør:
dotnet build src/RedMoon.App -t:Run -f net10.0-android
```
Release-APK/AAB: `dotnet publish src/RedMoon.App -f net10.0-android -c Release`

### Kør på iOS (kræver Mac)
```bash
dotnet build src/RedMoon.App -t:Run -f net10.0-ios                           # simulator
dotnet build src/RedMoon.App -t:Run -f net10.0-ios -p:RuntimeIdentifier=ios-arm64   # fysisk iPhone (kræver signering)
```
Opsæt signering (Team/Provisioning profile) i Visual Studio/Rider eller med `-p:CodesignKey=... -p:CodesignProvision=...`.

> App-id er `dk.roedmaane.app`. Ret `ApplicationId` i `src/RedMoon.App/RedMoon.App.csproj` til dit eget.

---

## 6. Hvor gemmes data?

| Hvad | Hvor | Beskyttelse |
|---|---|---|
| Al brugerdata (konto, humør, menstruation) | Filen `redmoon.vault` i appens private mappe (`FileSystem.AppDataDirectory`). Android: `/data/data/dk.roedmaane.app/files/`. iOS: appens `Library/`-mappe. | AES-256-CBC + HMAC-SHA256. Udelukket fra backup. iOS: `NSFileProtectionComplete`. |
| 256-bit krypteringsnøgle + session-token | iOS **Keychain** (`AfterFirstUnlockThisDeviceOnly`) / Android **Keystore** via MAUI `SecureStorage` | Hardware-beskyttet. Følger ikke med til andre enheder. |
| Midlertidig overførselsfil | Appens cache-mappe `transfer/` | Krypteret med overførselskoden. Slettes når siden lukkes og ved app-start. |

Intet sendes over netværket. Der er ingen server, ingen cloud-database, ingen analytics, ingen reklamer og ingen tredjeparts-login.

---

## 7. Sådan beskyttes adgangskoden (mønsteret)

1. Mønsteret (fx 1-5-9-6) hashes med **PBKDF2-HMAC-SHA256** (RFC 8018), **600.000 iterationer** (OWASP Password Storage Cheat Sheet's anbefaling for PBKDF2-HMAC-SHA256) og et **tilfældigt 16-byte salt** per bruger. Kun salt, hash og iterationstal gemmes. Iterationstallet gemmes per bruger, så det kan hæves senere.
2. Verifikation sammenligner i **konstant tid**, så svartiden ikke afslører noget.
3. Hashen ligger **inde i den krypterede fil**. En angriber skal altså først have nøglen fra Keychain/Keystore, før der overhovedet kan gættes offline.
4. **Spærring:** Efter 5 forkerte forsøg er login spærret i 30 sek., derefter 1, 2, 4 … op til 15 min. per forsøg. Spærringen overlever genstart af appen.
5. Fejlbeskeden er den samme for forkert brugernavn og forkert mønster.
6. **Ærlig vurdering:** Pga. det lille antal mønstre (afsnit 2, punkt 3) er punkt 1 reelt kun et ekstra lag. Den egentlige beskyttelse er Keychain/Keystore-nøglen og telefonens egen skærmlås/kryptering.

**Skift mønster** kræver det nuværende mønster og erstatter blot hashen lokalt (ingen server nødvendig).

**Overførsel til ny telefon:** Filen krypteres med en nøgle afledt (PBKDF2, 600.000 iterationer) af en **tilfældig 16-tegns kode** (80 bit, fx `K7PQ-3MZX-…`), som kun vises på skærmen. Mønsteret bruges *ikke*, fordi det kan gættes på et sekund. Med 80 bit og 600.000 iterationer er offline-gætning ikke realistisk. Login-session og spærring medtages ikke i filen.

---

## 8. Tilladelser (permissions)

| Platform | Tilladelser |
|---|---|
| **Android** | **Ingen.** `AndroidManifest.xml` erklærer ingen `<uses-permission>`. Debug-builds får automatisk `INTERNET` tilføjet af .NET, så debuggeren kan forbinde. Release-builds får det ikke. Filvælgeren (import) bruger systemets dokumentvælger, og del-funktionen bruger en FileProvider. Ingen af dem kræver tilladelser. **Tjek selv:** Efter et Release-build kan den endelige manifest ses i `src/RedMoon.App/obj/Release/net10.0-android/android/AndroidManifest.xml`. |
| **iOS** | **Ingen** brugsbeskrivelser (`NS…UsageDescription`) i `Info.plist`. Der bruges ikke kamera, lokation, kontakter, fotos osv. `PrivacyInfo.xcprivacy` indeholder kun de "required reason APIs", som .NET MAUI selv bruger, og ingen dataindsamling. |

Andre privatlivstiltag:
- Android: `allowBackup="false"` + `dataExtractionRules` (ingen Google-backup og ingen enhed-til-enhed-kopi). `FLAG_SECURE` skjuler appen i "seneste apps" og blokerer skærmbilleder. Fjern linjen i `Infrastructure/PrivacyScreen.cs`, hvis brugerne skal kunne tage skærmbilleder.
- iOS: Datafilen er udelukket fra iCloud/iTunes-backup. En ensfarvet flade skjuler appen i app-switcheren.

---

## 9. Tests

```bash
dotnet test tests/RedMoon.Core.Tests
```
83 tests dækker bl.a.:
- **Oprettelse af bruger** (gyldig, valgfrit brugernavn, ugyldigt input, findes allerede, intet i klartekst på disken)
- **Login** (korrekt, brugernavn uden forskel på store/små bogstaver, ingen konto)
- **Forkert adgangskode** (forkert mønster, forkert brugernavn, spærring + ophævelse, spærring overlever genstart)
- **Session** (gendannes efter genstart, ugyldig token afvises, log ud)
- **Lagring, hentning og ændring af daglig registrering** (inkl. overlever genstart, kun ændret felt ændres, fremtidige datoer afvises, tilbagerulning ved diskfejl)
- **Sletning** (slet dag, slet alle data inkl. nøgler)
- Kryptering (manipulation opdages, forkert nøgle, PBKDF2-testvektor fra RFC 7914 §11)
- Cyklus/forudsigelse (sammenlægning af dage til menstruationer inkl. glemte dage, standard 28/5, personlige gennemsnit, forsinket, gamle data, igangværende menstruation, årstider)
- Overførsel (eksport → import, forkert kode, eksisterende konto, ugyldig fil, session følger ikke med)
- Mønsterlåsens geometri (hurtige swipes, midterpunkt som på Android), som deles af MAUI- og Unity-appen

Testene bruger lave iterationstal (1.000), så de kører på under et sekund. Algoritmen er den samme som i appen.

**Kompileringstjek af appen uden Android SDK/Xcode** (bruges i CI eller på Linux):
```bash
dotnet build src/RedMoon.App -p:RedMoonCompileCheck=true
```
Dette bygger al delt C# og XAML mod MAUI's platformsneutrale mål. Bindinger til properties, der ikke findes, giver byggefejl.

**Manuel test på telefon (anbefalet tjekliste):**
1. Opret bruger uden brugernavn → log ud → log ind med tomt brugernavn.
2. Forkert mønster 5 gange → spærring vises.
3. Kalender: registrér "Første dag" og "Sidste dag" → dagene imellem farves, og næste forventede menstruation fremhæves.
4. Profil: vælg humør → vises på Hjem og i kalenderen.
5. Skift mønster → log ud → det gamle mønster virker ikke, det nye virker.
6. Overfør: lav fil på telefon A, importér på telefon B med koden.
7. Slet alle data → skriv SLET → appen er tom.
8. Skift telefonen til mørk tilstand → alle skærme kan læses.

---

## 10. Begrænsninger og sikkerhedsrisici du bør kende

- **Hvad der er verificeret:** Core-biblioteket er bygget (`netstandard2.1` som C# 9 og `net10.0`), og alle 83 tests kører grønt. Appens delte C# og XAML er kompileret (bindinger valideret). Den Android-specifikke C#-kode er kompileret mod Androids referenceassembly (`Mono.Android`).
- **iOS:** Hele iOS-appen kompileres uden fejl og advarsler på en Mac i GitHub Actions (`.github/workflows/ios.yml`, iPhone-build uden signering). Den bygges mod iOS 26.0-pakken med Xcode 26.x, fordi .NET for iOS 27 kræver Xcode 27, som GitHub's Macs endnu ikke har.
- **Android:** GitHub Actions bygger en installerbar APK (`.github/workflows/android.yml`), som er testet installeret på en Samsung Galaxy S25 Ultra. APK'en beder ikke om nogen tilladelser (kun AndroidX' interne `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`). Seneste build: `https://github.com/SandraCoyle/RedMoon/releases/download/test-latest/RoedMaane.apk`.
- **Ikke verificeret:** iOS-appen er ikke kørt på en iPhone. Det kræver Apple Developer Program (TestFlight) eller en Mac med Xcode.
- **Telefonen er sikkerhedsgrænsen.** En jailbroken/rootet telefon eller malware med root kan i princippet læse nøglen og data.
- **Svagt mønster** (se afsnit 2). Opfordr brugerne til at bruge flere end 4 punkter og at have skærmlås på telefonen.
- **Skuldersurfing:** Mønsterstregen er synlig mens den tegnes (den ryddes 0,7 sek. efter).
- **Mønsterregel som på Android:** Trækker man fra 1 til 3, kommer 2 automatisk med (punkter man passerer, tages med). Så giver samme tegning altid samme mønster, også ved hurtige swipes.
- **Overførselsfilen:** Hvis brugeren sender fil og kode samme vej (fx samme chat), er beskyttelsen væk. Appen advarer om det.
- **Uret kan manipuleres** for at omgå spærretiden.
- **Unity-udgaven** gemmer nøglen i en almindelig fil i Unity Editor/på computer (kun til test). På telefon bruges Keychain/Keystore, men det er ikke testet på en enhed endnu.
- **Forudsigelser er skøn**, ikke medicinsk rådgivning og ikke prævention.
- **Juridisk:** Data behandles kun lokalt på brugerens egen telefon, og udgiveren modtager intet. Det mindsker GDPR-forpligtelserne væsentligt, men lav alligevel en privatlivspolitik til App Store/Google Play. Begge butikker kræver det, og helbredsdata er en særlig kategori (GDPR art. 9). Få en jurist til at vurdere det, inden appen udgives.

---

## 11. Tilføj en ny underside

1. Lav `Views/MinSide.xaml(.cs)` og `ViewModels/MinSideViewModel.cs` (arv fra `ViewModelBase`).
2. Registrér begge i `MauiProgram.RegisterPages`:
   ```csharp
   services.AddTransient<MinSide>();
   services.AddTransient<MinSideViewModel>();
   ```
3. **I bundmenuen:** tilføj én linje i `Navigation/AppTabRegistry.cs`:
   ```csharp
   new AppTab("Spil", "tab_spil.png", "spil", typeof(MinSide)),
   ```
   (læg et `tab_spil.svg`-ikon i `Resources/Images/`).
   **Som underside** (uden for menuen): `await _navigation.PushAsync<MinSide>();`

---

## 12. Unity-udgaven

Hele appen findes også i en Unity-udgave (UI Toolkit): login, opret bruger, hjem med månen, kalender, profil, skift mønster og overførsel. Logikken er **den samme kode** (RedMoon.Core); kun brugerfladen er lavet til Unity.

**Hent og kør:** Download `RedMoon-Unity.zip` fra `https://github.com/SandraCoyle/RedMoon/releases/download/test-latest/RedMoon-Unity.zip` (eller kør `./tools/build-unity-app.sh`). Træk mappen `RedMoon` ind i `Assets` i et nyt Unity-projekt (2D-skabelon) og tryk **Play**. Hele vejledningen står i [`unity/LAES-MIG.md`](unity/LAES-MIG.md).

| Del | Placering |
|---|---|
| Skærme, navigation, bundmenu | `unity/App/Screens`, `unity/App/Navigation` (ny fane = én linje i `TabRegistry.cs`) |
| Måne og ikoner (tegnet i kode, ingen emoji/billedfiler) | `unity/App/Graphics` |
| Keychain (iOS) / Keystore (Android) / FLAG_SECURE | `unity/App/Platform`, `unity/Plugins/iOS/RedMoonKeychain.mm`, `unity/Plugins/Android/RedMoonAndroid.java` |
| Udseende | `unity/App/Resources/RedMoon/RedMoonStyles.uss` |

**Verificeret:** Unity-koden kompileres i CI mod Unitys officielle referenceassemblies (`UnityEngine.Modules` 2021.3, C# 9) uden fejl og advarsler: `dotnet build unity/CompileCheck`.
**Ikke verificeret:** Den er ikke kørt i Unity Editor eller på en telefon, og Java/Objective-C-plugins er ikke kompileret. Se begrænsningerne i `unity/LAES-MIG.md`.

**Kun logikken (uden brugerflade):** `./tools/export-unity-package.sh` laver en Unity-pakke af RedMoon.Core alene (`unity-export/com.redmoon.core`).
