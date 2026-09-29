# Angler's Eye — Technical Plan

**Idea:** a fishing assistant for Valheim. It tells you which fish is in the water and whether
you carry its bait, equips the right bait when you cast, cues the bite, shows whether the hooked
fish is struggling or calm, and forecasts whether you have the stamina to land it. It is an
assistant, not an overhaul: vanilla stamina costs, fish spawns, bait rules and drop tables are
untouched.

**Target build:** Valheim 1.0.16 (`Version.CurrentVersion = new GameVersion(1, 0, 16)`), Unity 6.
The rig's `assembly_valheim.dll` (md5 `7fd7feff8dbe94b582463f1e3d48b474`, checked 2026-09-29) is
identical to the one Earshot decompiled with `ilspycmd` on 2026-09-28. The findings in §1 are
read from that decompile (`Fish.cs`, `FishingFloat.cs`, `Attack.cs`, `Inventory.cs`,
`Humanoid.cs`); §1.7 lists what a runtime probe still has to confirm.

**Scope:** client-side only. No RPCs, no ZDO writes beyond what vanilla already does for the
local player, nothing on the server, no ServerSync; works on vanilla servers. Thunderstore
namespace `Jumpingmushroom`, package `AnglersEye` (display name "Angler's Eye"), GUID
`com.jumpingmushroom.anglerseye`, repo `github.com/jumpingmushroom/AnglersEye`. BepInEx only, no
Jotunn. Repo layout, build scripts, publicizer setup and the pure-model test project follow
Larder.

**Prior art:** Hooked (Azumatt; minigame, auto-hook, fish finder, flexible bait; takes over the
fight; deprecated), Reely Good Rod (halves reeling stamina), Reely SpecTackleLure,
GrindstoneSkills, Spearfishing, TheFisher, FishChum, FishTrap, Trolling Fishing. All change rules,
add content, or need a server install. None is a lightweight vanilla-preserving assistant.

**Decisions agreed (2026-09-29 design review):**
- **Philosophy:** information and cues on by default; smart bait counts as information-grade
  (it only equips an item you own). The two gameplay assists, extended hook window and smart
  reel, ship **off** by default.
- **0.1.0 scope:** all five features: fish ID, smart bait, bite/struggle cues (+ optional hook
  window), smart reel, catch forecast.
- **Fish ID:** vanilla hover text on a fish gains stars and bait ✔/✖ with count carried; plus
  one world-space label above your own float naming the fish that is approaching or nibbling.
  No labels on every fish.
- **Smart bait target:** fish under the crosshair; else the nearest fish in a narrow cone along
  the aim; else the most common species near you. Equip the best carried bait for it. If none is
  carried, a centre message names the bait needed and the cast goes ahead with vanilla's choice.
- **Odds:** ✔/✖ by default; `ShowOdds` adds the per-nibble bait chance as a percentage.
- **HUD:** one compact panel just above the stamina bar, in Valheim's panel style, visible only with a
  rod equipped (redesigned after in-game feedback; it replaced a text strip under the crosshair).
- **Forecast:** expected case (reel only when calm, average struggle timing, current skill),
  three states ✓ / ~ / ✖.
- **Bite sound:** a vanilla UI sound played 2D, on by default, with a volume setting.
- **Hook window** when enabled: 1.0 s default, range 0.5–1.5 s. No auto-hook.
- **Architecture:** polling plus small prefix/postfix patches; no transpilers.

---

## 1. Game behaviour (decompiled 1.0.16)

### 1.1 Fish identity, level and bait table (`Fish`)
- `Fish.m_name` is the localisation token shown as the name (`GetHoverName()`).
- Level ("stars") is the fish's `ItemDrop.m_itemData.m_quality` (`Fish.m_itemDrop`, private;
  `GetComponent<ItemDrop>()`). Stars shown = `quality - 1`, as with creatures.
- Bait rules: `List<Fish.BaitSetting> m_baits`, each `{ ItemDrop m_bait; float m_chance }`
  (`m_chance` in 0..1). This is prefab data, present on every client.
- `TestBate(ff)`: for each setting whose `m_bait.name` equals the float's bait prefab name, pass
  if `Random.value < m_chance`. So the right bait is still a dice roll per nibble.
- A fish only heads for a float at all if `Random.value < m_baseHookChance` (0.5 default) when it
  picks a waypoint (`FindFloat`, within `ff.m_range` = 10 m, float in water, float has no catch).
- A fish that fails the bait test remembers that float in `m_failedBait` and never nibbles it
  again. That's the one-off "the fish is not taking the bait" message.
- Fish AI (`CustomFixedUpdate` past the `IsOwner()` check) only runs on the fish's ZDO owner.
  `m_waypointFF` (the float a fish is heading for) is therefore only meaningful on the owner
  client; in multiplayer the zone owner may be another player.

### 1.2 The float (`FishingFloat`)
- `m_allInstances` / `GetAllInstances()`: every float. The local player's float is the one whose
  ZDO `s_rodOwner` equals the local `ZDOID.UserID` (`FindFloat(Character)` is private).
- Bait on the float: ZDO string `s_bait` (`GetBait()`), set in `Setup` from `ammo.m_dropPrefab.name`.
- Catch: ZDO `s_sessionCatchID` → `GetCatch()`.
- `m_lineLength` (private): current line length; `(rodTop - float).magnitude` is the actual distance.
- Line breaks when `distance - m_lineLength > m_breakDistance` (4) or `distance > m_maxDistance` (30).
- `m_range` = 10: radius in which fish consider this float.

### 1.3 Nibble and hook window
- The fish owner calls `ff.Nibble(fish, correctBait)` → RPC `RPC_Nibble` on the float owner (us).
- `RPC_Nibble` ignores nibbles within 1 s of the last one or when there's already a catch. With
  correct bait it plays `m_nibbleEffect`, jerks the float down, and sets `m_nibbler` and
  `m_nibbleTime = Time.time`. With wrong bait it gives a half jerk and the `$msg_fishing_wrongbait` message.
- `TryToHook()`: hooks if `m_nibbler != null && Time.time - m_nibbleTime < 0.5f && GetCatch() == null`.
  It's called from `FixedUpdate` when the float moves faster than 2 m/s horizontally (a jerk of the
  rod) and every tick while reeling.

### 1.4 Struggle
- `Fish.OnHooked(ff)` claims ZDO ownership for the hooking client, sets ZDO `s_hooked`, and
  immediately calls `Escape()`.
- `Escape()`: `m_escapeTime = Random.Range(m_escapeMin, m_escapeMax + quality * m_escapeMaxPerLevel)`
  (defaults 0.5, 3, 1.5), mirrored to ZDO `s_escape`.
- While `m_escapeTime > 0` it counts down. At 0, the next escape happens after
  `Random.Range(m_escapeWaitMin, m_escapeWaitMax)` (0.75, 4).
- `IsEscaping()` = `m_escapeTime > 0 && IsHooked()`. Because the hooker owns the fish, this is
  accurate on the local client while hooked.

### 1.5 Stamina and reeling (`FishingFloat.FixedUpdate`, float owner only)
With `s = owner.GetSkillFactor(Fishing)` (0..1) and `q` = fish quality:
- Passive drain while hooked: `lerp(m_hookedStaminaPerSec 1, m_hookedStaminaPerSecMaxSkill 0.2, s)` per second.
- Reeling (`owner.IsBlocking() && owner.HaveStamina()`): cost per second
  `c = m_pullStaminaUse (10) + fish.GetStaminaUse() * q`, then `lerp(c, c * 0.2, s)`.
  `GetStaminaUse()` = `m_escapeStaminaUse` (2) while escaping, else `m_staminaUse` (1).
- Reel speed per second: `lerp(m_pullLineSpeed 1, m_pullLineSpeedMaxSkill 2, s)`, **halved** while
  escaping. The line only shortens while `m_lineLength > distance - 0.2`.
- Landing: `m_lineLength <= 0.5`.
- Loss: `!owner.HaveStamina()` with a fish hooked → `$msg_fishing_lost`.
- So reeling during a struggle costs more per second and gains half the distance: waiting it out
  is strictly cheaper per metre. That's the technique smart reel automates.

### 1.6 Bait selection on cast (`Attack`, `Inventory`)
- `Attack.FindAmmo`: uses `Humanoid.GetAmmoItem()` (the equipped ammo, `m_ammoItem`) if it's in
  the inventory and has the weapon's `m_ammoType`; otherwise `Inventory.GetAmmoItem(ammoType)`.
- `Inventory.GetAmmoItem` returns the matching item with the **lowest grid index**
  (`y * width + x`). That's the top-left behaviour players complain about.
- `EquipAmmoItem` equips the found item if its type is `Ammo` or `AmmoNonEquipable`.
- Therefore equipping the chosen bait (`Humanoid.EquipItem`) before the attack resolves ammo makes
  vanilla cast with it.

### 1.7 Runtime probe findings (2026-09-29)
1. Bait prefabs are `ItemType.Ammo` with ammoType `$item_fishingbait`. `EquipItem` on a bait stack
   sticks and the next cast used it (float bait was `FishingBaitForest` while vanilla would have
   picked `FishingBait`). Smart bait's `EquipItem` approach works as designed.
2. No `hover fish` line was logged: the player's hover raycast does not reach fish under the water
   surface. Hover info therefore only helps for fish the crosshair can reach (surfaced or
   near-surface); underwater fish are covered by the float label and the pre-cast strip instead.
3. Bite clip: `UI_Craft_Finish_01` preferred, falling back to `Ui_Click_01`.
4. `m_baits` / `m_chance` / `m_staminaUse` / `m_escapeStaminaUse` / escape timing values for every
   fish in `ZNetScene` (fish table below), and the float's reeling numbers (float values below).
   Every bait's chance is 1.0 in practice; only `m_baseHookChance` = 0.1 (same for every fish)
   gates whether a fish approaches the float at all.
5. A cast calls `Attack.StartDraw` when the draw begins, then `Attack.Start` on release, then
   `FishingFloat.Setup`. Smart bait's prefixes on both run before the float is set up.
6. Of the label glyphs (★ ✔ ✖ ✓ ● ▲ » «), the HUD font (Valheim-AveriaSerifLibre) renders only
   `✖` and `·`; everything else falls back to ASCII via `Glyphs.Resolve`.

**Fish table** (prefab, name token, bait table, stamina, escape and wait timing):

| Prefab | Name token | Bait (chance) | staminaUse / escapeStaminaUse | Escape min–max +/lvl | Wait min–max |
|---|---|---|---|---|---|
| Fish1 | `$animal_fish1` | FishingBait (1.0) | 3 / 10 | 0.5–3 +1.5/lvl | 1.5–5 |
| Fish2 | `$animal_fish2` | FishingBait (1.0) | 5 / 15 | 0.5–3 +1.5/lvl | 1.5–5 |
| Fish3 | `$animal_fish3` | FishingBaitOcean (1.0) | 7 / 20 | 0.5–3 +1.5/lvl | 1.5–5 |
| Fish4_cave | `$animal_fish4` | FishingBaitCave (1.0) | 11 / 28 | 0.5–3 +1.5/lvl | 1.5–5 |
| Fish5 | `$animal_fish5` | FishingBaitForest (1.0) | 9 / 25 | 0.5–3 +1/lvl | 1.5–5 |
| Fish6 | `$animal_fish6` | FishingBaitSwamp (1.0) | 10 / 30 | 0.5–3 +1.5/lvl | 1.5–5 |
| Fish7 | `$animal_fish7` | FishingBaitPlains (1.0) | 12 / 32 | 0.5–3 +1.5/lvl | 1.5–5 |
| Fish8 | `$animal_fish8` | FishingBaitOcean (1.0) | 12 / 34 | 1–4 +1.5/lvl | 1.25–4 |
| Fish9 | `$animal_fish9` | FishingBaitMistlands (1.0) | 14 / 38 | 1–4 +1.5/lvl | 1.25–4 |
| Fish10 | `$animal_fish10` | FishingBaitDeepNorth (1.0) | 20 / 60 | 1–4 +1.5/lvl | 1.25–4 |
| Fish11 | `$animal_fish11` | FishingBaitAshlands (1.0) | 18 / 50 | 1–4 +1.5/lvl | 1.25–4 |
| Fish12 | `$animal_fish12` | FishingBaitMistlands (1.0) | 14 / 40 | 1–4 +1.5/lvl | 1.25–4 |

**Float values** (`FishingRodFloat`):

| Parameter | Value |
|---|---|
| `m_pullStaminaUse` | 0 |
| `m_pullStaminaUseMaxSkillMultiplier` | 0.2 |
| `m_pullLineSpeed` → max skill | 2 → 6 m/s |
| `m_hookedStaminaPerSec` → max skill | 1 → 0.2 |
| `m_range` | **50 m** |
| `m_maxDistance` | 30 m |
| `m_breakDistance` | 10 m |

---

## 2. Design

### 2.1 Architecture
- **`Core/Model/`**: pure C#, no UnityEngine or game types, xUnit-tested on net8.0.
  - `BaitAdvisor`: given a fish's bait table `(baitPrefab, chance)[]` and carried counts per bait
    prefab, returns the best carried bait (highest chance; ties go to the larger stack), whether any
    is carried, and the best bait overall (for the "needs X" message).
  - `CatchForecast`: given line distance, fish quality, `staminaUse`, `escapeStaminaUse`, escape
    timing parameters, skill factor, current stamina and whether the fish is hooked, returns the
    expected stamina cost and a verdict ✓ / ~ / ✖ (§2.4).
  - `TargetPicker`: given the crosshair fish (optional), the aim origin and direction, the cone
    angle, the cast range and candidate fish (position, species), returns the target species.
  - `ReelPolicy`: `ShouldReel(blockHeld, hooked, escaping, smartReelOn)` and
    `InHookWindow(now, nibbleTime, windowSeconds)` with the window clamped to 0.5–1.5.
- **`Core/`**: game adapters.
  - `FishCatalog`: built once per world from `ZNetScene` prefabs with a `Fish` component: species
    token, bait table, stamina and escape parameters. Read at runtime, so modded fish work. Never
    hardcode a fish.
  - `FishingState`: each frame, the local player's float (§1.2), catch, nibbler and nibble time,
    `IsEscaping`, line length, distance, stamina, skill factor, the equipped rod and bait.
  - `Compat`: detection and verdicts (§2.6).
  - `ConsoleCommands`: `anglerseye`, `anglerseye fish`.
- **`Patches/`**: each patch checks its feature toggle and `Compat` before acting.
  - `Fish.GetHoverText` postfix → hover info.
  - `FishingFloat.RPC_Nibble` postfix → bite cue (only when `correctBait` and the call set `m_nibbler`).
  - Attack-start prefix (§1.7.5) → smart bait.
  - `FishingFloat.FixedUpdate` prefix/finalizer sets a thread-static "inside float update for the
    local float" flag. A `Character.IsBlocking` prefix returns false while that flag is set, smart
    reel is on and the hooked fish `IsEscaping()`. Nothing else sees a changed `IsBlocking`.
  - `FishingFloat.TryToHook` prefix (only when the extended window is on) re-implements the method
    with the configured window and skips the original.
- **`UI/`**
  - `FishingPanel`: uGUI + TextMeshPro, 6 px above the stamina bar (or above eitr/adrenaline while
    their animators' `Visible` bool is set; the roots are never deactivated), centred on the stamina
    bar. A procedural 9-sliced rounded frame (dark fill, thin warm-gold edge), the HUD font and the
    hover text's outlined material, and the vanilla creature-level star
    (EnemyHud `level_2`) for the fish's level (`Lv N` text if that sprite is missing). Content comes
    from the pure `Model/PanelView`. Config scale and offset apply.
  - `FloatLabel`: a world-space-to-screen follower above the local float; outlined text, no box.
  - `BiteCue`: the panel's BITE! plus the 2D sound.

### 2.2 Features
1. **Fish ID.**
   - Hover: the vanilla name, then `★` × (quality − 1), then a second line
     `Bait: <best bait> ✔ (xN)` or `Bait: <best bait> ✖`, with ` 60%` when `ShowOdds` is on. If a
     fish accepts several baits, show the best carried one, or the best overall if none is carried.
   - Float label: shown while the float is in the water with no catch. Subject: a fish whose
     `m_waypointFF` is our float (available when we own the fish), else the nearest fish within the
     float's `m_range`. Shows the name, stars and bait ✔/✖. Hidden when nothing is in range. Since
     the probe found `m_range` is 50 m (§1.7), the nearest-fish fallback is capped at 10 m so the
     label stays about the fish actually near your float, not one across the pond.
2. **Smart bait.** On attack start with a fishing rod (a weapon whose `m_ammoType` matches a bait
   type), `TargetPicker` picks the species and `BaitAdvisor` picks the bait. If the chosen bait
   differs from what vanilla would use, equip it. If the species has no carried bait, show a centre
   message `Angler's Eye: <fish> needs <bait>` and don't touch the cast. With no target, do nothing.
3. **Bite and struggle cues.**
   - Nibble with correct bait: the panel shows a big yellow `BITE!` for the hook window, and the sound plays.
   - Hooked: the panel shows a big `REEL` (calm, green) or `WAIT` (struggling, amber) with a bar
     draining over the struggle, plus the forecast. No distance: vanilla shows the line length.
   - Optional extended hook window (off; 1.0 s default; 0.5–1.5 s).
4. **Smart reel** (off by default). While Block is held, reel only when the fish is calm. Stamina
   maths is untouched. While suppressed, the passive hooked drain still applies as in vanilla. It
   doesn't prevent line breaks; vanilla's pause has the same risk.
5. **Catch forecast** (§2.4). Shown on the panel while hooked, and before a hook for the targeted
   fish at the current float distance, assuming quality 1 when the fish's level is unknown.

### 2.3 Panel states (only with a rod equipped)
Title row: fish name + star icons (quality − 1). Body row:
| State | Body |
|---|---|
| Rod out, no float | `Cold bait (12)`, or `needs Cold bait` in red (` 60%` after the bait with ShowOdds) |
| Float in water | `Waiting…` + forecast word; title `Waiting…` alone if no fish is near |
| Nibble | big yellow `BITE!` (for the hook window) |
| Hooked, calm | big green `REEL` + forecast word |
| Hooked, struggling | big amber `WAIT` + draining struggle bar + forecast word |
Forecast words: `can land` (green), `tight` (amber), `unlikely` (red).
Nothing is shown when no target can be determined and no float is out.

### 2.4 Forecast model
Expected-case, assuming the player reels only while calm:
- Let `s` be the skill factor and `q` the quality. Reel speed while calm is `v = lerp(1, 2, s)`.
  Reel time is `T = d / v`.
- Expected struggle length is `E_esc = (escapeMin + escapeMax + q * escapeMaxPerLevel) / 2`, and
  the expected wait between struggles is `E_wait = (escapeWaitMin + escapeWaitMax) / 2`. The calm
  fraction of wall time is `f = E_wait / (E_wait + E_esc)`. The hook itself starts a struggle, so
  add one `E_esc` up front. Wall time is `W = E_esc + T / f`.
- Cost = reel cost × `T`, with `lerp(10 + staminaUse*q, (10 + staminaUse*q) * 0.2, s)`, plus the
  passive drain × `W`, with `lerp(1, 0.2, s)`.
- Verdict: ✓ if `stamina >= 1.25 × cost`; ~ if `stamina >= cost`; else ✖. Stamina regen while
  hooked is ignored, which is conservative.
- Unit tests pin each term against hand-computed values from §1.4–1.5.

### 2.5 Configuration
BepInEx config `com.jumpingmushroom.anglerseye.cfg` with `ConfigurationManagerAttributes`
declared locally. Every setting applies live.

| Section | Key | Default | Range / notes |
|---|---|---|---|
| General | `Enabled` | true | Master switch |
| Identify | `HoverInfo` | true | |
| Identify | `FloatLabel` | true | |
| Identify | `ShowOdds` | false | |
| Bait | `SmartBait` | true | |
| Bait | `AimConeDegrees` | 10 | 3–30 |
| Cues | `BiteCue` | true | |
| Cues | `BiteSound` | true | |
| Cues | `BiteVolume` | 0.7 | 0–1 |
| Cues | `StruggleIndicator` | true | |
| Forecast | `Forecast` | true | |
| Assists | `SmartReel` | false | |
| Assists | `ExtendedHookWindow` | false | |
| Assists | `HookWindowSeconds` | 1.0 | 0.5–1.5 |
| UI | `Scale` | 1.0 | 0.5–2 |
| UI | `OffsetX` / `OffsetY` | 0 / 0 | pixels |
| Logging | `Verbose` | false | |

### 2.6 Compatibility
- **Known fishing overhauls**, matched by GUID in `Chainloader.PluginInfos` (GUIDs read from their
  DLLs on 2026-09-29):
  - `Azumatt.Hooked` (Hooked 1.1.1; patches `Fish.FindFloat`/`TestBate` and
    `FishingFloat.FixedUpdate`/`SetCatch`/`Setup`, and replaces the fight): turn off smart bait, smart
    reel, the extended hook window, the forecast, the bite cue and the struggle indicator. Fish ID stays.
  - `sighsorry.TrollingFishing` (Trolling Fishing 1.1.3; patches `FixedUpdate`, `SetCatch`, `Setup`,
    `Catch`, `ReturnBait`, `Fish.FindFloat`): turn off smart bait, smart reel, the extended hook
    window and the forecast. Fish ID and cues stay.
- **Any other plugin** with Harmony patches (found via `Harmony.GetPatchInfo`, excluding our own
  GUID, checked once after all plugins have loaded):
  - on `FishingFloat.FixedUpdate` (e.g. ComfyFishing `games.loxley.comfyfishing`): smart reel off;
  - on `FishingFloat.TryToHook`: the extended hook window off;
  - on `Fish.GetStaminaUse` (e.g. Reely Good Rod `com.orianaventure.mod.ReelyGoodRod`): the
    forecast off, since its stamina maths would be wrong.
- Every decision is logged once at Info and listed by the `anglerseye` console command.
- Every patch body catches its own exceptions and logs once per call site (the Larder `Warned`
  pattern), so a failure never breaks vanilla fishing.

### 2.7 Testing and verification
- `dotnet test tests/AnglersEye.Tests`: `BaitAdvisor`, `CatchForecast`, `TargetPicker`,
  `ReelPolicy`.
- The runtime probe (§1.7) comes before any UI work; its findings are written into §1.
- In-game checklist on the rig: right bait / wrong bait / no bait carried; hover underwater;
  float label; bite flash and sound; struggle states; low-stamina loss vs a ✖ forecast; smart reel
  on/off; hook window on/off; compat with a dummy FishingFloat patch.

### 2.8 Release
- The version lives in three places that must agree: `PluginVersion` in
  `src/AnglersEye/Plugin.cs`, `<Version>` in the csproj, and `version_number` in
  `thunderstore/manifest.json`.
- `build/package.sh` → `dist/AnglersEye-X.Y.Z.zip` with `manifest.json`, `README.md`,
  `CHANGELOG.md`, `icon.png` (256×256, from `build/make_icon.py`), `LICENSE` and
  `plugins/AnglersEye/AnglersEye.dll`. Dependency `denikson-BepInExPack_Valheim-5.4.2350`.
- Commit, tag `vX.Y.Z`, push with tags, `gh release create`, then
  `scp dist/AnglersEye-X.Y.Z.zip equ@192.168.1.160:~/Downloads/`.
- Thunderstore categories: `client-side`, `utility`, `deep-north-update`, `ai-generated`.
- No AI attribution in commits, PRs, the README or release notes.
