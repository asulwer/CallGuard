---
layout: default
title: How it works
nav_order: 6
---

# How it works
{: .no_toc }

What happens when a call comes in, and what Call Guard does with your data.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

---

## When a call comes in

Before your phone rings, Android hands the incoming number to the app chosen as **Caller ID & spam app** —
Call Guard. This happens in the background, even when Call Guard is closed and the phone is locked; no Call
Guard screen appears. Call Guard answers in about a second at most — usually much faster — and Android
either lets the call ring or rejects it.

Outgoing calls are never touched.

## How each call is decided

Only patterns that are switched on (ticked) count. In order:

1. **No caller ID** (private or hidden number) → **rings**. Leave these to the Phone app's **Unknown**
   setting ([Setup](setup.html#private-and-hidden-numbers)).
2. Matches an **always allow** pattern → **rings**.
3. Matches **no blocked** pattern → **rings**.
4. The caller is in your **contacts** → **rings**, even though the number matches a blocked pattern.
5. Otherwise → **blocked**.

A blocked call is rejected the same way as when you press **Decline**: your phone doesn't ring or vibrate,
the caller hears a ring and then your voicemail, and the call appears as blocked in the Phone app's
**Recents** and in Call Guard's **Calls** tab.

{: .note }
Your phone can't make a number sound "not in service" — only your phone company or Google Voice can. If one
particular number keeps calling, block it in Google Voice, which plays that message.

## Contacts

Call Guard looks the number up in all of your phone's contacts — the same contacts the Phone app shows,
including people synced from your Google account, the SIM, and apps such as WhatsApp or Signal that add
their own copies. If **any** of them has the number, the call rings.

That's why changing someone's number in the Contacts app may not stop their calls from getting through:
another copy may still have the old number. Open the contact and check its **Linked contacts** (or
**Sources**) to see where the copies come from. The Calls tab helps: it shows which copy let the call
through, e.g. `contact in WhatsApp (matches 602*)`.

## If something goes wrong
{: #if-something-goes-wrong }

Call Guard is designed so that a problem never blocks a call:

- If deciding takes longer than **2.5 seconds** or fails with an error, the call **rings**, and the Calls
  tab shows `ERROR … (call allowed)`.
- If Call Guard **can't read your contacts** — access was revoked, or the contacts weren't ready yet —
  calls that would be blocked **ring**, because the caller might be a contact.
- If Call Guard doesn't answer Android at all, Android lets the call ring by itself.
- Right after the phone restarts, Android doesn't start Call Guard until you've unlocked the phone once, so
  calls ring until then.

## Text messages

Call Guard only screens **calls**. Android only lets the default texting app filter messages, so texts
aren't blocked. Use the spam and blocking settings in Google Messages, or in Google Voice for Voice texts.

## Privacy

- **Permissions:** Call Guard asks for one permission: **read your contacts**, to let your contacts through.
  Being the Caller ID & spam app lets it see each incoming number; Android grants that role separately.
- **No internet access:** Call Guard doesn't request internet access at all, so it can't send your
  contacts, patterns or call history anywhere.
- **Place names** ("Arizona", "London, United Kingdom") come from Google's open-source
  [libphonenumber](https://github.com/google/libphonenumber) data built into the app, so looking them up
  needs no internet either. They're only shown on screen; they play no part in deciding calls.
- **On your phone only:** your patterns and the call log are stored in the app's private storage, which
  other apps can't read. Uninstalling Call Guard deletes them.
- **Phone backups:** if Android's backup to your Google account is turned on, Call Guard's patterns and call
  log may be included in it, like other apps' data, so they can come back on a new phone.
- **Open source:** the complete source code is on [GitHub](https://github.com/asulwer/CallGuard).
