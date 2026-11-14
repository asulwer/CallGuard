---
layout: default
title: Install
nav_order: 2
---

# Install
{: .no_toc }

Get Call Guard from [Google Play](https://play.google.com/store/apps/details?id=com.acclscompany.callguard), or install the `.apk` directly from its
[GitHub releases](https://github.com/asulwer/CallGuard/releases).
{: .fs-6 .fw-300 }

1. TOC
{:toc}

---

## Install from Google Play

You need an Android phone with **Android 10 or later**.

<a href="https://play.google.com/store/apps/details?id=com.acclscompany.callguard"><img alt="Get it on Google Play" src="https://play.google.com/intl/en_us/badges/static/images/badges/en_badge_web_generic.png" height="80"></a>

Open **[Call Guard on Google Play](https://play.google.com/store/apps/details?id=com.acclscompany.callguard)** on your phone and tap **Install**. Play installs updates
automatically.

Then [set Call Guard up](setup.html) — it does nothing until you do.

## Install the .apk instead
{: #install-the-apk-instead }

If you'd rather not use Google Play, install the same app directly from GitHub:

1. On the phone, open **[the latest release](https://github.com/asulwer/CallGuard/releases/latest)**.
2. Under **Assets**, tap the file named `CallGuard-<version>.apk` to download it.
3. Open the download — from the browser's download notification, or from the **Files** app's
   **Downloads**.
4. Android asks whether to allow your browser (or Files) to **install unknown apps**. Allow it, go back,
   and tap **Install**.
5. **Google Play Protect** may warn that the app is from an unknown developer or hasn't been scanned
   before. That's expected for any app installed outside the Play Store: tap **More details → Install
   anyway** (the wording varies by Android version).

Then [set Call Guard up](setup.html) — it does nothing until you do.

{: .note }
After installing you can switch "install unknown apps" off again: Settings → Apps → Special app access →
**Install unknown apps** → your browser or Files → off.

## Which version do I have?

The version is shown at the bottom of Call Guard's **Home** tab, for example `Call Guard 1.0.261020 (build
3578484)`. Versions follow the date they were built: `1.0.261020` was built on 2026-10-20. Each
[release](https://github.com/asulwer/CallGuard/releases) on GitHub is named after its version.

## Update

- **From Google Play:** updates install automatically, or tap **Update** on Call Guard's Play page.
- **The .apk:** install the newer `.apk` the same way, over the old one — no need to uninstall first.

Either way your patterns, their on/off state and the call log are kept, and Call Guard stays set up.

The Google Play version and the `.apk` are the same app signed with the same key, so you can switch
between them at any time by installing one over the other.

{: .warning }
If Android says **App not installed** when updating, the new file was signed with a different key than
the copy on your phone — for example, a copy you built yourself. Export your patterns
([Backup](using.html#backup)), uninstall the old copy, install the new one and import your patterns again.

## Uninstall

1. Settings → Apps → **Call Guard** → **Uninstall**. This deletes your patterns and call log, so export
   them first if you might want them again ([Backup](using.html#backup)).
2. To get Google's caller ID & spam protection back, choose **Phone by Google** again under Settings →
   Apps → **Default apps** → **Caller ID & spam app**.
