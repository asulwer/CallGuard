---
layout: default
title: Troubleshooting & FAQ
nav_order: 7
---

# Troubleshooting & FAQ
{: .no_toc }

1. TOC
{:toc}

---

## A call I wanted blocked still rang

Work through these in order:

1. **Home tab:** are both setup lines green ✔? If **Caller ID & spam app** is red, another app took the
   role — tap the button and choose Call Guard again.
2. **Home tab:** is there a red line saying no blocked pattern is switched on? Tick your patterns on the
   Patterns tab.
3. **Calls tab:** find the call. The reason tells you why it rang:
   - `contact (matches …)` — the number is in your contacts, possibly as a copy you didn't know about
     (see [the next question](#i-changed-someones-number-but-their-calls-still-get-through)).
   - `always allow …` — an always-allow pattern covers it.
   - no reason — no ticked pattern matches the number as it arrived. Compare the number shown with your
     pattern, and try it in **Test a number**.
4. **The call isn't in the Calls tab at all:** Call Guard wasn't asked. Either it isn't the Caller ID & spam
   app, or the call came in through the **Google Voice app** over Wi-Fi or data instead of as a phone call —
   set Voice to **Use carrier only** ([Setup](setup.html#google-voice)).
5. **Every call shows your own Google Voice number:** Voice is showing your number instead of the caller's
   on forwarded calls. Change that in Voice settings ([Setup](setup.html#google-voice)).

## I changed someone's number, but their calls still get through
{: #i-changed-someones-number-but-their-calls-still-get-through }

Their number is still saved somewhere else on your phone. Contacts can be made of several copies — from
your Google account, the SIM, WhatsApp, Signal and other apps — and Call Guard lets a call through if
**any** copy has the number. Open the contact and look at **Linked contacts** (or **Sources**) to find the
other copies.

## Someone in my contacts was blocked

That shouldn't happen. Check that Call Guard has **contacts access** (Home tab), and that the number saved
in the contact is the one they actually call from — including the area code. Calls with **no caller ID**
can't be matched to a contact; those are handled by the Phone app, not Call Guard.

## Private callers aren't in the Calls tab or the counts

If you turned on **Blocked numbers → Unknown** in the Phone app, Android blocks private calls before Call
Guard is asked. They appear in the Phone app's Recents instead. That's expected.

## The blocked-call counts didn't change

The counts only include calls **Call Guard** blocked. Calls blocked by the Phone app (private numbers, or
numbers on the Phone app's own block list) and by Google Voice aren't counted.

## What does a blocked caller hear?

A ring, then your voicemail — the same as when you decline a call. An app on your phone can't make a
number sound "not in service"; if one number keeps calling, block it in Google Voice, which does.

## Are text messages blocked?

No. Android only lets the default texting app filter messages. Use the spam and blocking settings in
Google Messages, or in Google Voice for Voice texts.

## Google's spam labels disappeared

Only one app can be the Caller ID & spam app, and that's now Call Guard. Turn on **Filter spam** in Google
Voice settings to have spam filtered before it reaches your phone ([Setup](setup.html#google-voice)).

## Can I pause blocking?

Untick your blocked patterns on the Patterns tab. Every call then rings, and you can tick them again later.

## Android says "App not installed" when I update

The new `.apk` is signed with a different key than the copy on your phone — for example, one of them was
built by yourself. Export your patterns, uninstall the old copy, install the new one, and import your
patterns. See [Install → Update](install.html#update).

## Play Protect warns about Call Guard

Google Play Protect warns about apps installed from outside the Play Store that it hasn't seen before. Call
Guard is open source — the code that builds each release is on
[GitHub](https://github.com/asulwer/CallGuard). Choose **Install anyway** to continue.

## Does Call Guard drain the battery?

No. It doesn't run in the background between calls; Android starts it briefly for each incoming call.

## How do I switch back to Google's caller ID & spam protection?

Settings → Apps → **Default apps** → **Caller ID & spam app** → **Phone by Google**. Call Guard then stops
blocking calls.
