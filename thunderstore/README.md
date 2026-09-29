# Angler's Eye

Angler's Eye is a fishing assistant for Valheim. It tells you which fish is in the water and
whether you carry the bait it wants, puts the right bait on your hook when you cast, tells you
the moment a fish bites, shows whether a hooked fish is struggling or calm, and estimates whether
you have the stamina to land it. It doesn't change how fishing works: stamina costs, fish spawns,
bait rules and catches are all vanilla. Client-side, works on vanilla servers, BepInEx only.

![A hooked fish struggling, with the panel above the stamina bar showing WAIT and a forecast](https://raw.githubusercontent.com/jumpingmushroom/AnglersEye/main/docs/images/hooked.jpg)

*Hooked: WAIT while the fish struggles, with the bar underneath draining as the struggle runs
out, and the forecast beside it. The panel sits right above the stamina bar.*

![A cast float with a label naming the fish at it, and the panel below](https://raw.githubusercontent.com/jumpingmushroom/AnglersEye/main/docs/images/float.jpg)

*Waiting for a bite: the label above the float names the fish and says the bait on the hook
works, and the panel repeats the fish and the forecast.*

## Features

1. **Fish identification.** Look at a fish and its hover text gains its stars and a bait line:
   the bait it wants and how many you carry (e.g. `+ (x12)`), or ✖ if you carry none. The HUD font
   has no star glyph, so stars appear as `*` in hover text. This only works for fish your crosshair
   can reach — in practice fish at or near the surface; the hover raycast doesn't reach fish
   further underwater. A small label above your own float names the fish nibbling or heading for
   it, and says whether the bait on your hook works on it ("bait ok" or "needs <bait>").
2. **Smart bait.** When you cast, Angler's Eye equips the carried bait that works best on the
   fish you're aiming at, instead of whatever the game would otherwise pick (the bait you last
   equipped if you still have it, otherwise the stack nearest the top-left of your inventory). If
   you carry none of its bait, it names the bait you need and still casts with vanilla's own
   choice.
3. **Bite cue.** A hookable nibble makes the panel flash **BITE!** for exactly as long as you
   have to hook it, with a short sound.
4. **Struggle indicator.** While a fish is hooked, the panel shows **REEL** when it's calm and
   **WAIT** when it's struggling, with a bar under WAIT that drains as the struggle runs out.
   Reeling during a struggle costs more stamina per second and gains only half the line, so
   waiting it out is the cheaper way to land a fish.
5. **Catch forecast.** The panel shows `can land`, `tight`, or `unlikely`, worked out from the
   line length, the fish's level, your Fishing skill and your current stamina.
6. **Optional assists, off by default:**
   - **Smart reel**: while you hold Block, it reels only when the fish is calm. Stamina costs
     stay vanilla — it just does the waiting for you. Fishing skill only rises while the game's
     own reel code is running, and smart reel skips that during a struggle, so your Fishing skill
     won't rise for the time it spends waiting one out.
   - **Extended hook window**: up to 1.5 s to hook a nibble instead of vanilla's 0.5 s.

## How it works

Everything comes from data the game already has on your machine: each fish's bait table, its
level, and the state of your own float and hooked fish. Nothing is sent to the server, and no
fish, bait or drop table is changed.

In vanilla, the right bait always works on a fish that nibbles your float — what's rare is a fish
choosing your float at all (roughly a 1-in-10 chance each time it picks a waypoint). That's why
Angler's Eye shows bait advice rather than odds by default. Turn on `ShowOdds` to also show the
bait's chance per nibble as a percentage; it's 100% for every vanilla fish, but can differ for a
modded one.

The forecast assumes you reel only while the fish is calm and uses average struggle lengths, so
it's an estimate. It ignores stamina regeneration, which makes it slightly cautious.

In multiplayer, the fish near your float are simulated by whoever owns that zone. If that's
another player, the float label falls back to showing the nearest fish within 10 m of your float,
rather than the one actually heading for it.

## Configuration

Everything is in `BepInEx/config/com.jumpingmushroom.anglerseye.cfg`, or in-game with
ConfigurationManager (F1). Changes apply immediately.

| Section | Setting | Default | What it does |
|---|---|---|---|
| General | `Enabled` | `true` | Master switch for everything Angler's Eye shows or does. |
| Identify | `HoverInfo` | `true` | On a fish's hover text, the bait it takes and how many you carry, or ✖ if none. |
| Identify | `FloatLabel` | `true` | Label above your float naming the fish and the bait status. |
| Identify | `ShowOdds` | `false` | Also show the bait's chance per nibble as a percentage. |
| Bait | `SmartBait` | `true` | Equip the best carried bait on cast; say what's needed if you carry none. |
| Bait | `AimConeDegrees` | `10` | How far off your aim (degrees) a fish still counts as the target (3–30). |
| Cues | `BiteCue` | `true` | BITE! flash while a nibble can be hooked. |
| Cues | `BiteSound` / `BiteVolume` | `true` / `0.7` | Play a sound on a hookable nibble, and its volume (0–1). Needs BiteCue on. |
| Cues | `StruggleIndicator` | `true` | REEL / WAIT with a struggle bar while a fish is hooked. |
| Forecast | `Forecast` | `true` | Estimate whether you can land the fish: can land, tight or unlikely. |
| Assists | `SmartReel` | `false` | While holding Block, reel only while the fish is calm. |
| Assists | `ExtendedHookWindow` / `HookWindowSeconds` | `false` / `1.0` | Longer hook window (0.5–1.5 s). |
| UI | `Scale` | `1` | Size of the fishing panel and the float label (0.5–2). |
| UI | `OffsetX` / `OffsetY` | `0` / `0` | Nudge the panel from its spot above the stamina bar, in pixels. |
| Logging | `Verbose` | `false` | Log smart bait and compatibility decisions to the BepInEx log. |

Console: `anglerseye` shows what's on and why; `anglerseye fish` lists every fish's bait and fight
numbers.

## Compatibility

Angler's Eye steps aside for mods that take over fishing, and says so in the log and in the
`anglerseye` command:

- **Hooked**: smart bait, the forecast, both assists and the bite and struggle cues turn off.
  Fish identification stays.
- **Trolling Fishing**: smart bait, the forecast and both assists turn off. Fish identification
  and the cues stay.
- Any other mod that patches the float's reeling turns off smart reel; one that patches hooking
  turns off the extended hook window; one that changes fish stamina costs (e.g. Reely Good Rod)
  turns off the forecast, since its stamina maths would be wrong.

## Installation

Install with r2modman or Thunderstore Mod Manager, or copy `AnglersEye.dll` into
`BepInEx/plugins/`. Requires BepInExPack Valheim. Client-side only; nothing is needed on the
server.

