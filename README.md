# Call Guard

A small Android app (Avalonia UI, .NET 10) that blocks unwanted incoming calls by number pattern
(e.g. a whole area code), **except** for numbers saved in your contacts.

## What it does
The app has three tabs:
- **Home**: setup status, how many calls were blocked today / this week / this month, and a
  **Test a number** box that shows what would happen if a number called you.
- **Patterns**: numbers to **block** or **always allow**, each with a check box to switch it on or off
  without deleting it, plus export/import to a text file.
- **Calls**: the recent-calls log. Tap **Block** or **Allow** on a row to add that exact number.

## How it decides
Only patterns that are ticked count. To pause blocking, untick your blocked patterns (Home then warns
that every call rings).

1. Hidden / private number → ring. Block those with the Phone app instead: Phone → Settings →
   Blocked numbers → **Unknown** (Android blocks them before Call Guard is asked, so they appear in the
   Phone app's Recents, not in Call Guard's Calls tab)
2. Number matches an **always allow** pattern → ring
3. Number matches no **blocked** pattern → ring
4. Number is a saved contact → ring, even though it matches a blocked pattern
5. Otherwise → rejected: your phone stays silent, the caller goes to voicemail, and the call shows as
   blocked in call history

### If something goes wrong, calls ring
Call Guard never blocks a call because of a problem:
- an error while deciding, or no decision within 2.5 s → the call rings and the Calls tab logs `ERROR … (call allowed)`
- contacts access revoked, or the contacts lookup fails → calls that would be blocked ring
- Call Guard not answering Android at all (crash, disabled, not set as the Caller ID & spam app) → Android lets the call ring
- before the first unlock after a reboot, Android doesn't start Call Guard, so calls ring

### What happens when a call comes in
Android starts Call Guard's screening service in the background, even if the app is closed and the phone
is locked. No Call Guard screen appears; a blocked call simply never rings, and an allowed call shows the
normal Phone app screen.

Starting from scratch for a call is kept fast (about 0.3–1 s on the emulator, well inside Android's limit):
- the UI framework (Avalonia) only starts when you open the app, not when Android starts it for a call
- the database schema is only checked with EF Core migrations when it has changed
  (`CallGuardDatabase.SchemaVersion`; update it when you add a migration — a test checks this)
- patterns are read for screening with one plain SQL query (`ScreeningPatternReader`); EF Core's first
  query in a process takes over a second on a phone

Only **calls** are screened. SMS/MMS are not affected (see Notes).

## Patterns
US numbers, compared without the leading 1.
- `602*`: whole area code
- `602555*`: one exchange
- `602555????`: same, using single-digit wildcards (`?` = exactly one digit)
- `6025550123`: one exact number
- `+44*`: every number from one **country** (`+` and its country code), whatever comes after it
- `+4420*`: one region of that country, the same way as US area codes (here: London, area code 20)
- A pattern starting with `+` only ever matches international calls, and a pattern without it only
  US-zone numbers, so `+44*` (UK) and `440*` (Ohio) can never catch each other's calls. `011 44*` works too.
- Spaces, dashes, brackets and a leading `+1` are ignored, so `+1 (602) 555-****` works too.

Canada and some Caribbean islands share +1 with the US, so block those with area-code patterns
(e.g. `876*` for Jamaica).

A pattern with no digits (`*`, `??????????`) would match every caller, so the app asks you to tap Add twice.

### Export file format
Plain text, one pattern per line, so you can also write or edit it by hand:
```
block 602*          # Phoenix spam
block +44*          # every UK number
block 480* (off)    # kept, but switched off
allow 6025550123    # doctor's office
480555*             # no keyword means block
```

## Solution layout
| Project | What it is |
|---|---|
| `CallGuard` | The Android app: Avalonia UI (`Views/`), the call-screening service (`BlockerService.cs`), contacts lookup, app icon (`Resources/`). |
| `CallGuard.Core` | Platform-independent logic: pattern matching (`RuleEngine`), the block/allow decision (`ScreeningDecider`), and the EF Core + SQLite database for patterns and the call log (`Data/`). |
| `CallGuard.Tests` | xUnit tests for everything in Core, including the database against a real SQLite file. |

Package versions are managed centrally in `Directory.Packages.props`.

## Build and install (Visual Studio 2026)
1. In the Visual Studio Installer, make sure the **.NET Multi-platform App UI development** workload is installed.
   Call Guard doesn't use MAUI, but that workload is how Visual Studio installs the .NET Android tooling.
2. Open `CallGuard.slnx`.
3. On the Pixel: Settings → About phone → tap **Build number** 7 times, then
   Settings → System → Developer options → turn on **USB debugging**. Connect by USB and accept the prompt.
4. Set `CallGuard` as the startup project, pick the Pixel in the device dropdown and press **F5**.
5. Run the tests from **Test Explorer**.

Command line:
```
dotnet build CallGuard.slnx
dotnet test --project CallGuard.Tests
dotnet build CallGuard/CallGuard.csproj -t:Install     # install a Debug build on the connected phone/emulator
```

## Release signing and installing on the phone
Android only installs an update if it's signed with the same key as the installed app. Call Guard's key
was created by Visual Studio's Archive Manager (signing identity `callguard`) and lives at
`%LOCALAPPDATA%\Xamarin\Mono for Android\Keystore\callguard\callguard.keystore`. Every update must be
signed with it.

**Back up that `Keystore\callguard` folder and its password** somewhere other than this PC. Without them
you can't update the installed app; you'd have to uninstall it, which deletes your patterns and call log
(export your patterns first).

### Option 1: Visual Studio Archive (the way the first release was made)
1. Set the configuration to **Release**.
2. Right-click `CallGuard` → **Archive…**, then **Distribute… → Ad Hoc**.
3. Pick the `callguard` signing identity, enter its password, and **Save As** an `.apk`.

### Option 2: command line
Release builds sign with the same key when the `CALLGUARD_KEYSTORE_PASS` environment variable holds its
password; otherwise they use the shared debug key and the build warns that the phone won't accept it.
The password is passed to the signing tool by reference, so it never appears in the project or build logs.
Set it once for your Windows account (PowerShell), then restart Visual Studio and terminals:
```
[Environment]::SetEnvironmentVariable("CALLGUARD_KEYSTORE_PASS", (Read-Host "Keystore password"), "User")
```
Then build the `.apk`:
```
dotnet publish CallGuard/CallGuard.csproj -c Release
```
It ends up in `CallGuard\bin\Release\net10.0-android\publish\com.acclscompany.callguard-Signed.apk`.
Release builds target the phone's 64-bit ARM CPU; add `-p:ForEmulator=true` to build one for the (x64) emulator.

To check which key signed an `.apk` (no password needed), compare the certificate fingerprint:
```
& "C:\Program Files (x86)\Android\android-sdk\build-tools\36.0.0\apksigner.bat" verify --print-certs <file>.apk
```
Call Guard's key: SHA-256 `b8bbb5cf3313f7abd85b16f125f4cb525138aa6e9b973451f07dfd13ae6f1563`.

### Installing
With the Pixel connected by USB (USB debugging on), from the solution folder:
```
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" -d install -r <file>.apk
```
`-d` picks the USB phone when an emulator is also running. Or copy the `.apk` to the phone and open it in
the Files app. Google Play Protect may warn about an app from an unknown developer; choose **Install anyway**.
Updates keep your patterns and log as long as they're signed with the same key.

## Releases on GitHub
Signed APKs are published at **https://github.com/asulwer/CallGuard/releases**. To install one, open
that page on an Android phone (Android 10 or later), download the `.apk` from the latest release and open
it; allow the browser or Files app to install apps when asked. Installing a newer release over an older
one keeps your patterns and call log.

### Making a release
Tag the commit with its version and push the tag:
```
git tag v1.1
```
```
git push origin v1.1
```
The **Release APK** workflow (`.github/workflows/release.yml`) then runs the tests, builds the APK
signed with the `callguard` key, checks the signature against the key's fingerprint, and publishes a
release named after the tag with the APK attached. The tag is the version: the app reports exactly
what follows the `v` (`v1.1` → version `1.1`, shown in Android's app info and on the Home tab), and the
workflow refuses to publish if the built APK reports anything else. Use numeric tags such as `v1.1` or
`v1.0.3`. **Run workflow** in the Actions tab builds the same APK (with the usual date-based version) as a
downloadable workflow artifact without publishing a release.

Every push and pull request to `master` also runs **Build and test** (`.github/workflows/build-test.yml`):
the unit tests and a Release build, so problems show up before you tag a release.

### One-time setup: the signing key
The release workflow signs with the same key as your Visual Studio Archive builds, stored as encrypted
repository secrets. Run these once in PowerShell (the second command asks for the keystore password):
```
[Convert]::ToBase64String([IO.File]::ReadAllBytes("$env:LOCALAPPDATA\Xamarin\Mono for Android\Keystore\callguard\callguard.keystore")) | gh secret set CALLGUARD_KEYSTORE_BASE64 --repo asulwer/CallGuard
```
```
gh secret set CALLGUARD_KEYSTORE_PASS --repo asulwer/CallGuard
```
The repository variable `SIGNING_CERT_SHA256` holds the key's public fingerprint (see above); the
workflow refuses to publish an APK signed with anything else, because phones won't accept it as an update.

## Changing the database
The schema is managed with EF Core migrations (in `CallGuard.Core/Data/Migrations`). The app upgrades its
database automatically on start. After changing the model:
```
dotnet tool restore
dotnet ef migrations add <Name> --project CallGuard.Core --output-dir Data/Migrations
```
The test `Migrations_MatchTheModel` fails if you forget.

## One-time setup on the phone
1. Open Call Guard → **Set as Caller ID & spam app** → choose Call Guard → **Set as default**.
   (This replaces Phone by Google in that slot; Google's spam labels on your personal line go away.)
2. **Allow contacts access**.
3. Add your patterns on the **Patterns** tab, then check a few numbers with **Test a number**.

## Google Voice setup (required)
Voice calls must reach the phone as normal carrier calls showing the caller's real number:
- Voice app → Settings → Making and receiving calls → **Use carrier only**
- Voice settings → Calls → show the **caller's number** (not your Voice number) on forwarded calls
- Your Pixel's number linked and enabled for forwarding

## Testing on the phone
- Call your Voice number from a phone that's in your contacts and in the blocked area code → should ring.
- Call from a non-contact in that area code → should not ring; the **Calls** tab shows `BLOCKED`.
- If the log shows your Voice number for every call, Voice is set to display your own number; fix that setting.

In the emulator you can fake incoming calls with `adb emu gsm call 6025551234` (and `adb emu gsm cancel 6025551234`).

## Notes
- Android does pass calls from saved contacts to the screening app, so the app's own contacts check is
  what lets them through.
- Call Guard deliberately doesn't repeat what your phone and Google Voice already do: block private numbers
  with the Phone app (Blocked numbers → Unknown), and spoofed or spam calls with Google Voice
  (Settings → Security → Filter spam).
- **SMS/MMS are not blocked.** Android only lets the *default SMS app* filter messages, and its system
  block list only takes exact numbers, not patterns. Use the spam/blocking settings in Google Messages
  (or Google Voice for Voice texts).
