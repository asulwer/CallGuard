---
layout: default
title: Home
nav_order: 1
description: "Call Guard blocks unwanted calls on Android by area code, country or region, while the people in your contacts always get through."
---

# Call Guard
{: .fs-9 }

Block unwanted calls by area code, country or region — the people in your contacts always get through.
{: .fs-6 .fw-300 }

[Download the latest version](https://github.com/asulwer/CallGuard/releases/latest){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[Get started](install.html){: .btn .fs-5 .mb-4 .mb-md-0 }

---

Spam calls often come from the same few area codes, or from abroad. Call Guard lets you block a whole area
code, an exchange, a country or a region of a country with one short **pattern** — `602*`, `+44*` — while
every number saved in your contacts still rings. Blocked calls never ring: your phone stays silent and the
caller goes to voicemail.

<p align="center">
  <img src="{{ '/assets/images/home.png' | relative_url }}" alt="Call Guard's Home tab: setup status, blocked-call counts and the Test a number box" width="260">
  <img src="{{ '/assets/images/patterns.png' | relative_url }}" alt="The Patterns tab with blocked and always-allowed patterns" width="260">
  <img src="{{ '/assets/images/calls.png' | relative_url }}" alt="The Calls tab listing blocked and allowed calls with the reason for each" width="260">
</p>

## What it does

- **Blocks by pattern.** A US area code (`602*`), an exchange (`602555*`), one number, a whole country
  (`+234*`) or one region of a country (`+4420*` — London). [All about patterns](patterns.html).
- **Your contacts always ring**, even when their number matches a blocked pattern.
- **Always allow** the exceptions — say, a doctor's office inside a blocked area code.
- **Switch patterns on and off** with a check box, without deleting them.
- **See what happened:** every screened call is listed with the reason it was blocked or allowed, and
  **Test a number** tells you what would happen before anyone calls.
- **Works in the background**, even when the app is closed and the phone is locked — no screen pops up.
- **Fails safe:** if anything goes wrong, the call rings. Call Guard never blocks a call because of a problem.
- **Private:** Call Guard only asks for access to your contacts, has no internet access, and keeps
  everything on your phone. [How it works](how-it-works.html).

## Requirements

- An Android phone with **Android 10 or later**. Built and tested for Google Pixel phones.
- If you use **Google Voice**, a few Voice settings need to change so that Call Guard sees each caller's
  real number — see [Setup](setup.html#google-voice).

## Get started

1. [Install Call Guard](install.html) from the latest release.
2. [Set it up](setup.html): two taps in the app, plus a few phone and Google Voice settings.
3. [Add your patterns](patterns.html) and [check them](using.html#test-a-number) with **Test a number**.

Questions? See [Troubleshooting & FAQ](troubleshooting.html).
