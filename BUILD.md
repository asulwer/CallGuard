# Building Call Guard

How to build, test, sign and install Call Guard yourself. How to *use* the app is in [README.md](README.md).

## Solution layout
| Project | What it is |
|---|---|
| `CallGuard` | The Android app: Avalonia UI (`Views/`), the call-screening service (`BlockerService.cs`), contacts lookup, app icon (`Resources/`). |
| `CallGuard.Core` | Platform-independent logic: number and pattern matching (`RuleEngine`), the block/allow decision (`ScreeningDecider`), the fail-open time limit (`FailOpen`), and the EF Core + SQLite database for patterns and the call log (`Data/`). |
| `CallGuard.Tests` | xUnit tests for everything in Core, including the database against a real SQLite file. |

Package versions are managed centrally in `Directory.Packages.props`. `global.json` opts `dotnet test`
into Microsoft.Testing.Platform, which the .NET 10 SDK requires for xUnit v3.

## Requirements
- **Visual Studio 2026** with the **.NET Multi-platform App UI development** workload. Call Guard doesn't
  use MAUI, but that workload is how Visual Studio installs the .NET Android tooling (Android SDK, Java).
- Or, for the command line only: the **.NET 10 SDK** with the `android` workload (`dotnet workload install android`).

## Build and run
In Visual Studio: open `CallGuard.slnx`, set `CallGuard` as the startup project, pick a device and press
**F5**. Run the tests from **Test Explorer**.

To run on a phone, enable USB debugging once: Settings → About phone → tap **Build number** 7 times, then
Settings → System → Developer options → **USB debugging**. Connect by USB and accept the prompt.

Command line, from the solution folder:
```
dotnet build CallGuard.slnx
```
```
dotnet test --project CallGuard.Tests
```
```
dotnet build CallGuard/CallGuard.csproj -t:Install
```
The last one installs a Debug build on the connected phone or emulator.

### Emulator tips
- Debug builds run in the (x64) emulator. Release builds target the phone's 64-bit ARM CPU only; add
  `-p:ForEmulator=true` to build a Release for the emulator.
- Fake incoming calls: `adb emu gsm call 6025551234` and `adb emu gsm cancel 6025551234`.
- Skip the setup dialogs: `adb shell cmd role add-role-holder android.app.role.CALL_SCREENING com.acclscompany.callguard`
  and `adb shell pm grant com.acclscompany.callguard android.permission.READ_CONTACTS`.
- The emulator's phone network can't withhold caller ID, so hidden-number calls can't be simulated.

## Signing and installing
Android only installs an update if it's signed with the same key as the installed app. Call Guard's key was
created by Visual Studio's Archive Manager (signing identity `callguard`) and lives at
`%LOCALAPPDATA%\Xamarin\Mono for Android\Keystore\callguard\callguard.keystore`.

**Back up that `Keystore\callguard` folder and its password** somewhere other than this PC. Without them an
installed copy can't be updated; it would have to be uninstalled, which deletes its patterns and call log.

### Visual Studio Archive
1. Set the configuration to **Release**.
2. Right-click `CallGuard` → **Archive…**, then **Distribute… → Ad Hoc**.
3. Pick the `callguard` signing identity, enter its password, and **Save As** an `.apk`.

### Command line
Release builds sign with the same key when the `CALLGUARD_KEYSTORE_PASS` environment variable holds its
password; otherwise they use the shared debug key, and the build warns that a phone with a signed copy won't
accept it. The password is passed to the signing tool by reference (`env:CALLGUARD_KEYSTORE_PASS`), so it
never appears in the project or in build logs. For the current PowerShell window:
```
$env:CALLGUARD_KEYSTORE_PASS = Read-Host "Keystore password"
```
Then:
```
dotnet publish CallGuard/CallGuard.csproj -c Release
```
The `.apk` ends up in `CallGuard\bin\Release\net10.0-android\publish\com.acclscompany.callguard-Signed.apk`.

To check which key signed an `.apk` (no password needed):
```
& "C:\Program Files (x86)\Android\android-sdk\build-tools\36.0.0\apksigner.bat" verify --print-certs <file>.apk
```
Call Guard's key has SHA-256 `b8bbb5cf3313f7abd85b16f125f4cb525138aa6e9b973451f07dfd13ae6f1563`.

### Installing
With the phone connected by USB, from the solution folder:
```
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -d install -r <file>.apk
```
`-d` picks the USB phone when an emulator is also running. Copying the `.apk` to the phone (USB, Bluetooth,
Google Drive) and opening it in the Files app works too.

## Versioning
Set in `CallGuard.csproj`, every build:
- **versionName** (what Android shows): `VersionPrefix.yyMMdd`, e.g. `1.0.261021` — the build date. Change
  `VersionPrefix` in the project for a new major/minor version.
- **versionCode** (what Android compares for updates): whole minutes since 2020-01-01 UTC, so every build is
  newer than the one before. Builds within the same minute share a code; Android still accepts reinstalling it.
- .NET assembly versions are pinned to `1.0.0.0`, because the date part exceeds their 65535 limit.

## Changing the database
The schema is managed with EF Core migrations in `CallGuard.Core/Data/Migrations`. After changing the model:
```
dotnet tool restore
```
```
dotnet ef migrations add <Name> --project CallGuard.Core --output-dir Data/Migrations
```
Then set `CallGuardDatabase.SchemaVersion` to the new migration's name. Tests fail if either step is
forgotten (`Migrations_MatchTheModel`, `SchemaVersion_IsTheLatestMigration`). Check the generated migration's
default values: a new column's default is what existing rows get on upgrade (e.g. `IsEnabled` defaults to
`true` so existing patterns stay switched on).

The app upgrades its database the first time it needs it after an update — in the app or when screening a
call — and remembers the schema version, so later starts skip the check.

## Design notes
- **Screening runs without the UI.** Android starts the app process just to screen a call. The UI framework
  (Avalonia) only starts when a screen opens (`MainApplication.StartUi`), not on every process start.
- **Fast cold starts.** Android gives screening apps about 5 seconds in total. The EF Core migration check
  only runs when the schema changed, and patterns are read for screening with one plain SQL query
  (`ScreeningPatternReader`) because EF Core's first query in a process takes over a second on a phone.
  Measured on the emulator: a call that has to start the app is answered in about 0.3–1 s.
- **Fail open.** `FailOpen` gives the decision 2.5 s; an exception or a timeout lets the call ring. A failed
  contacts lookup counts as "unknown", which also lets the call ring.
- **Numbers.** US-zone numbers (+1) are compared as 10 digits; other countries keep a leading `+` and their
  country code, so a pattern like `+44*` can never match a US number.

## Continuous integration
Every pull request to `master` runs **Build and test** (`.github/workflows/build-test.yml`): the unit tests
and a Release build of the app, signed with the debug key. Both are skipped (and count as passed) when the
pull request changes nothing the app is built from — only docs, the README, the license, the release
workflow or Dependabot's settings — so such a pull request can be merged straight away.
