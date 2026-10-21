# Call Guard

An Android app that blocks unwanted incoming calls by number pattern — a whole US area code, an
exchange, a country, or a region of a country — **except** calls from people in your contacts.

Blocked calls never ring: your phone stays silent and the caller goes to voicemail. Everything else rings
as usual.

**Full documentation, with screenshots: https://asulwer.github.io/CallGuard/** — building the app yourself
is described in [BUILD.md](BUILD.md).

## Install
1. On your Android phone (Android 10 or later), open **https://github.com/asulwer/CallGuard/releases**.
2. Download the `.apk` from the latest release and open it.
3. When Android asks, allow your browser or Files app to install apps. Google Play Protect may warn about
   an app from an unknown developer; choose **Install anyway**.

**Updating:** install the newer `.apk` the same way, over the old one. Your patterns and call log are kept.
Afterwards you can switch "install unknown apps" off again under Settings → Apps → Special app access →
**Install unknown apps**.

## First-time setup
Open Call Guard. The **Home** tab shows what still needs doing:

1. **Set as Caller ID & spam app** → choose **Call Guard** → **Set as default**.
   This is what lets Call Guard see incoming calls. It takes the place of Phone by Google's caller ID & spam
   feature, so Google's spam labels on calls go away (see *Settings to change* for what covers spam instead).
2. **Allow contacts access.** Call Guard checks every number it would block against your contacts, so the
   people you know always get through. Without this access it blocks nothing, to be safe.
3. On the **Patterns** tab, add what you want blocked (see *Using the app*).

Both setup lines on the Home tab should then be green ✔.

## Settings to change
Call Guard only handles what your phone and Google Voice don't already do, so a few settings belong
elsewhere.

### Google Voice (required if you use a Voice number)
Calls to your Voice number must reach the phone as normal calls that show the **caller's** number,
otherwise Call Guard can't match them:
- Voice app → Settings → **Making and receiving calls** → **Use carrier only**
- Voice settings → **Calls** → show the **caller's number** (not your Voice number) on forwarded calls
- Your phone's number is linked to Voice and turned on for forwarding
- Recommended: Voice → Settings → Security → **Filter spam**, which catches spoofed and spam calls before
  they're forwarded to your phone

### Phone app
- **Private and hidden numbers:** Phone app → ⋮ → Settings → **Blocked numbers** → turn on **Unknown** if
  you don't want calls without caller ID. Android blocks these before Call Guard sees them, so they show in
  the Phone app's **Recents**, not in Call Guard's Calls tab.

## Using the app
The app has three tabs.

### Home
- **Setup:** whether Call Guard is the Caller ID & spam app and has contacts access, with buttons to fix
  either. A red line also warns when no blocked pattern is switched on (every call rings).
- **Blocked calls:** how many calls Call Guard blocked today, in the last 7 days and in the last 30 days.
- **Test a number:** type any number and tap **Check** to see what Call Guard would do if it called you now,
  and why — for example "would be BLOCKED: matches 602*" or "would ring: contact (matches 602*)".
- At the bottom: the installed version.

### Patterns
Type a pattern, an optional note, choose **Block** or **Always allow**, and tap **Add**.

| Pattern | Matches |
|---|---|
| `602*` | every number in US area code 602 |
| `602555*` | every number starting 602-555 |
| `602555????` | the same, written with single-digit wildcards |
| `6025550123` | one exact number |
| `+44*` | every number in one **country** — `+` and its country code (here the UK) |
| `+4420*` | one region of that country, like an area code (here London) |

- `*` means any number of digits, `?` exactly one digit. A pattern without `*` must match the whole number.
- Spaces, dashes and brackets are ignored, and so is a leading `+1` or `1`: `+1 (602) 555-****` works.
  `011 44*` (the US way of dialing abroad) means the same as `+44*`.
- Patterns starting with `+` only match calls from other countries, and patterns without it only US
  numbers — so `+44*` (UK) and `440*` (Ohio) never catch each other's calls.
- Canada and some Caribbean islands share +1 with the US: block those with area codes, e.g. `876*` for Jamaica.
- A pattern with no digits (such as `*`) would block every caller who isn't a contact, so Call Guard asks
  you to tap **Add** a second time to confirm.

**Always allow** patterns ring even when they also match a blocked pattern — useful for, say, a doctor's
office inside a blocked area code.

**Switching patterns on and off:** each pattern has a check box. Untick it to switch the pattern off without
deleting it; tick it to switch it back on. To pause blocking altogether, untick all your blocked patterns.
**Remove** deletes a pattern.

**Backup:** **Export…** saves your patterns to a text file (e.g. on Google Drive); **Import…** loads them,
for example on a new phone. Patterns you already have are skipped. The file is plain text, so you can also
write or edit it by hand:
```
block 602*          # Phoenix spam
block +44*          # every UK number
block 480* (off)    # kept, but switched off
allow 6025550123    # doctor's office
480555*             # no keyword means block
```

### Calls
The recent calls Call Guard screened, newest first, with what it did and why:
- **BLOCKED** (red) — the call was rejected, e.g. `matches 602*`.
- **allowed** — the call rang, e.g. `contact (matches 602*)`, `always allow 6025550123`, or no reason when no
  pattern matched.

Tap **Block** on a call that got through, or **Allow** on one that was blocked, to add that exact number
to your patterns. **Clear log** empties the list. Call Guard keeps the newest 500 calls.

## How it decides
For each incoming call, only patterns that are ticked count:

1. Hidden or private number → rings (leave these to the Phone app's **Unknown** setting).
2. Matches an **always allow** pattern → rings.
3. Matches no **blocked** pattern → rings.
4. The caller is a saved contact → rings, even though the number matches a blocked pattern.
5. Otherwise → **blocked**: your phone stays silent, the caller goes to voicemail, and the call shows as
   blocked in the Phone app's Recents and in Call Guard's Calls tab.

This happens in the background, even when Call Guard is closed and the phone is locked. No Call Guard
screen appears during a call.

**If something goes wrong, calls ring.** Call Guard never blocks a call because of a problem: an error, a
decision that takes too long, missing contacts access or a failed contacts lookup all let the call through.
Errors appear in the Calls tab as `ERROR … (call allowed)`.

## Testing it
- Use **Home → Test a number** with a number your pattern covers — it shows exactly what would happen.
- Live test: have someone who **isn't** in your contacts (or WhatsApp, Signal, etc.) call you from a number
  your pattern covers. Your phone shouldn't ring, and the **Calls** tab shows `BLOCKED`.
- A call from a contact with a blocked area code should ring, and shows as `allowed … contact` in the Calls tab.
- If every call in the Calls tab shows your own Google Voice number, Voice is set to show your number on
  forwarded calls — change that setting (see *Google Voice*).

## Good to know
- **Contacts are found by number across all apps.** If you change someone's number in Contacts but another
  app (WhatsApp, Signal, your Google account, the SIM) still has a copy with the old number, Android still
  treats that number as a contact. Check the contact's **Linked contacts** to see where copies come from.
- **What callers hear:** a blocked caller hears a ring and then voicemail — the same as when you decline a
  call. The phone can't make a number sound "not in service"; if someone keeps calling, block that one
  number in Google Voice, which does.
- **Text messages are not blocked.** Android only lets the default SMS app filter messages. Use the spam
  and blocking settings in Google Messages, or in Google Voice for Voice texts.
