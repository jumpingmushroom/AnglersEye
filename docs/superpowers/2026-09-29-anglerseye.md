# Angler's Eye 0.1.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A client-side BepInEx mod for Valheim 1.0.16 that assists fishing without changing its rules: fish identification (hover text and a label above the float), smart bait on cast, bite and struggle cues, a catch forecast, and two opt-in assists (smart reel, extended hook window).

**Architecture:** A pure C# model (`Core/Model`, no Unity or game types, xUnit-tested on net8.0) holds bait advice, the catch forecast, target picking, reel policy, labels and compat rules. Thin game adapters (`Core/`) read fish prefabs, the local player's float and the hooked fish each frame. Small Harmony prefix/postfix patches (`Patches/`) hook hover text, the nibble RPC, attack start, the float's `FixedUpdate`/`TryToHook` and `Humanoid.IsBlocking`, with no transpilers. The UI (`UI/`) is a TextMeshPro strip under the crosshair plus a label that follows the float, both parented to the HUD.

**Tech Stack:** C# (LangVersion latest) targeting net472, BepInEx 5 + Harmony, Unity UI + TextMeshPro, `BepInEx.AssemblyPublicizer.MSBuild` 0.4.3, xUnit on net8.0 for the model.

**Spec:** `PLAN.md` (repo root). Read it first: §1 is the decompiled game behaviour every task relies on, §2 the agreed design.

## Global Constraints

- Game: Valheim 1.0.16. Client-side only: no RPCs of our own, no ZDO writes beyond what vanilla does for the local player, nothing on the server, no ServerSync; must work on vanilla servers.
- Must not change vanilla stamina costs, fish spawns, bait rules or drop tables. The two assists (smart reel, extended hook window) default **off**.
- Dependencies: BepInEx only (`denikson-BepInExPack_Valheim-5.4.2350`). **No Jotunn.** `ConfigurationManagerAttributes` is declared locally.
- GUID `com.jumpingmushroom.anglerseye`, plugin name `Angler's Eye`, assembly/package `AnglersEye`, version `0.1.0`. Thunderstore namespace `Jumpingmushroom`. Repo `github.com/jumpingmushroom/AnglersEye`.
- Version lives in three places that must agree: `PluginVersion` in `src/AnglersEye/Plugin.cs`, `<Version>` in `src/AnglersEye/AnglersEye.csproj`, `version_number` in `thunderstore/manifest.json`.
- Everything under `src/AnglersEye/Core/Model/` stays free of UnityEngine and game types.
- Fish data is read from the game at runtime; never hardcode a fish, bait or rod.
- Every patch body catches its own exceptions and reports through `AnglersEyePlugin.WarnOnce(key, e)`; a failure must never break vanilla fishing.
- Every feature is gated by `Features.On(Feature.X)` (master switch + its toggle + compat verdict).
- **No AI attribution anywhere**: no `Co-Authored-By` trailer, nothing in the README, PRs or release notes. Commit **and push** after every task.
- Build box: `export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH` before any `dotnet` or `ilspycmd` command. The scripts set this themselves.
- Rig: `equ@192.168.1.160`, login shell fish (wrap non-trivial commands in `bash -c '...'`), game at `/games/SteamLibrary/steamapps/common/Valheim`, r2modman profile `/home/equ/.config/r2modmanPlus-local/Valheim/profiles/Default`.

## File Structure

```
AnglersEye/
  AnglersEye.sln
  Directory.Build.props            copied from Larder
  CLAUDE.md  PLAN.md  README.md  CHANGELOG.md  LICENSE  .gitignore
  build/package.sh  build/publish.sh  build/make_icon.py              (committed)
  build/deploy.sh  build/logs.sh  build/shot.sh  build/crop.sh        (gitignored, local)
  thunderstore/manifest.json  thunderstore/README.md  thunderstore/icon.png
  docs/superpowers/2026-09-29-anglerseye.md                           (this plan)
  src/AnglersEye/
    AnglersEye.csproj  Plugin.cs  PluginConfig.cs  ConfigurationManagerAttributes.cs
    Core/Model/Vec3.cs            plain vector
    Core/Model/BaitAdvisor.cs     BaitOption, BaitAdvice, BaitAdvisor
    Core/Model/CatchForecast.cs   RodParams, FishParams, Verdict, Forecast, CatchForecast
    Core/Model/TargetPicker.cs    FishSighting, TargetPicker
    Core/Model/ReelPolicy.cs      hook window + reel suppression
    Core/Model/Glyphs.cs          glyph set with ASCII fallback
    Core/Model/Labels.cs          every user-facing string
    Core/Model/CompatRules.cs     Feature flags, CompatVerdict, CompatRules
    Core/FishCatalog.cs           FishInfo per fish prefab + rod params
    Core/Tackle.cs                rod detection, carried bait counts
    Core/Compat.cs                runtime detection -> CompatRules
    Core/Features.cs              Features.On(Feature)
    Core/FishingState.cs          FishingSnapshot of the local float each frame
    Core/Targeting.cs             aim target via TargetPicker
    Core/SmartBait.cs             equip the right bait before a cast
    Core/Runtime.cs               per-frame tick, strip text
    Core/ConsoleCommands.cs       `anglerseye`, `anglerseye fish`
    Patches/HoverPatch.cs  Patches/CastPatches.cs  Patches/NibblePatch.cs  Patches/ReelPatches.cs
    UI/UiUtil.cs  UI/Palette.cs  UI/Strip.cs  UI/FloatLabel.cs  UI/BiteCue.cs
  tests/AnglersEye.Tests/
    AnglersEye.Tests.csproj  BaitAdvisorTests.cs  CatchForecastTests.cs  TargetPickerTests.cs
    ReelPolicyTests.cs  LabelsTests.cs  CompatRulesTests.cs
```

---

### Task 1: Scaffold the repo, build and packaging

**Files:**
- Create: `Directory.Build.props`, `AnglersEye.sln`, `src/AnglersEye/AnglersEye.csproj`, `src/AnglersEye/Plugin.cs`, `src/AnglersEye/PluginConfig.cs`, `src/AnglersEye/ConfigurationManagerAttributes.cs`, `tests/AnglersEye.Tests/AnglersEye.Tests.csproj`, `CLAUDE.md`, `build/package.sh`, `build/publish.sh`, `build/deploy.sh`, `build/logs.sh`, `build/shot.sh`, `build/crop.sh`, `thunderstore/manifest.json`
- Existing: `.gitignore`, `LICENSE`, `PLAN.md`

**Interfaces:**
- Produces: `AnglersEye.AnglersEyePlugin` with `PluginGuid`, `PluginName`, `PluginVersion`, `static ManualLogSource Log`, `static AnglersEyePlugin Instance`, `static void WarnOnce(string key, Exception e)`. `AnglersEye.PluginConfig` with the static `ConfigEntry` fields listed in Step 5.

- [ ] **Step 1: Pull reference assemblies from the rig into `lib/` (gitignored)**

```bash
cd /workspace/gamemods/Valheim/AnglersEye && mkdir -p lib
M=/games/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed
C=/home/equ/.config/r2modmanPlus-local/Valheim/profiles/Default/BepInEx/core
for f in assembly_valheim assembly_utils assembly_guiutils UnityEngine UnityEngine.CoreModule UnityEngine.UI UnityEngine.UIModule UnityEngine.TextRenderingModule UnityEngine.IMGUIModule UnityEngine.InputLegacyModule UnityEngine.AudioModule Unity.TextMeshPro; do scp -q equ@192.168.1.160:$M/$f.dll lib/; done
scp -q equ@192.168.1.160:$C/BepInEx.dll equ@192.168.1.160:$C/0Harmony.dll lib/
md5sum lib/assembly_valheim.dll
```
Expected: `7fd7feff8dbe94b582463f1e3d48b474  lib/assembly_valheim.dll`. If it differs, the game has updated: stop and re-check PLAN §1 against a fresh decompile first.

- [ ] **Step 2: Build props and csproj**

`cp ../Larder/Directory.Build.props .` (no edits needed).

`src/AnglersEye/AnglersEye.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <AssemblyName>AnglersEye</AssemblyName>
    <RootNamespace>AnglersEye</RootNamespace>
    <Version>0.1.0</Version>
    <Description>A fishing assistant: fish ID, smart bait, bite and struggle cues, catch forecast. Client-side.</Description>
  </PropertyGroup>

  <ItemGroup>
    <!-- .NET Framework 4.7.2 reference assemblies; required to build net472 off-Windows. -->
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
    <PackageReference Include="BepInEx.AssemblyPublicizer.MSBuild" Version="0.4.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- Publicized at build time only; the shipped DLL binds to the real members
         and runs against an unmodified game install. -->
    <Reference Include="assembly_valheim"  HintPath="$(ValheimManaged)/assembly_valheim.dll"  Publicize="true" Private="false" />
    <Reference Include="assembly_utils"    HintPath="$(ValheimManaged)/assembly_utils.dll"    Private="false" />
    <Reference Include="assembly_guiutils" HintPath="$(ValheimManaged)/assembly_guiutils.dll" Private="false" />

    <Reference Include="UnityEngine"                     HintPath="$(ValheimManaged)/UnityEngine.dll" Private="false" />
    <Reference Include="UnityEngine.CoreModule"          HintPath="$(ValheimManaged)/UnityEngine.CoreModule.dll" Private="false" />
    <Reference Include="UnityEngine.UI"                  HintPath="$(ValheimManaged)/UnityEngine.UI.dll" Private="false" />
    <Reference Include="UnityEngine.UIModule"            HintPath="$(ValheimManaged)/UnityEngine.UIModule.dll" Private="false" />
    <Reference Include="UnityEngine.TextRenderingModule" HintPath="$(ValheimManaged)/UnityEngine.TextRenderingModule.dll" Private="false" />
    <Reference Include="UnityEngine.IMGUIModule"         HintPath="$(ValheimManaged)/UnityEngine.IMGUIModule.dll" Private="false" />
    <Reference Include="UnityEngine.InputLegacyModule"   HintPath="$(ValheimManaged)/UnityEngine.InputLegacyModule.dll" Private="false" />
    <Reference Include="UnityEngine.AudioModule"         HintPath="$(ValheimManaged)/UnityEngine.AudioModule.dll" Private="false" />
    <Reference Include="Unity.TextMeshPro"               HintPath="$(ValheimManaged)/Unity.TextMeshPro.dll" Private="false" />

    <Reference Include="BepInEx"  HintPath="$(BepInExPath)/BepInEx.dll" Private="false" />
    <Reference Include="0Harmony" HintPath="$(BepInExPath)/0Harmony.dll" Private="false" />
  </ItemGroup>

</Project>
```

`tests/AnglersEye.Tests/AnglersEye.Tests.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- Overrides Directory.Build.props (net472): the model is plain C#, so it is tested on the
       net8 runtime the build box has. Mono is not installed, so the plugin itself can't run here. -->
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>disable</Nullable>
    <!-- The invariant-globalization SDK flags the test host's localized resources; harmless. -->
    <NoWarn>$(NoWarn);NETSDK1188</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="../../src/AnglersEye/Core/Model/*.cs" Link="Model/%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

Solution:
```bash
dotnet new sln -n AnglersEye
dotnet sln AnglersEye.sln add --solution-folder src src/AnglersEye/AnglersEye.csproj
dotnet sln AnglersEye.sln add --solution-folder tests tests/AnglersEye.Tests/AnglersEye.Tests.csproj
```

- [ ] **Step 3: `src/AnglersEye/ConfigurationManagerAttributes.cs`** (verbatim from Larder)

```csharp
// Read by ConfigurationManager through reflection, matched by type name only. Declaring it
// here means no dependency on any particular ConfigurationManager build (or on Jotunn, which
// ships its own copy), and nothing breaks if no config manager is installed.
#pragma warning disable 0649
internal sealed class ConfigurationManagerAttributes
{
    public int? Order;
    public bool? IsAdvanced;
    public bool? Browsable;
    public bool? ReadOnly;
}
```

- [ ] **Step 4: `src/AnglersEye/Plugin.cs`**

```csharp
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AnglersEye
{
    /// <summary>
    /// A fishing assistant. Reads fish, bait and float data already present on the client and
    /// never changes vanilla stamina costs, spawns, bait rules or drops. Purely client-side.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim.x86_64")]
    public sealed class AnglersEyePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.jumpingmushroom.anglerseye";
        public const string PluginName = "Angler's Eye";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        internal static AnglersEyePlugin Instance;

        private static readonly HashSet<string> Warned = new HashSet<string>();
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;
            PluginConfig.Bind(base.Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(AnglersEyePlugin).Assembly);

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }

        /// <summary>Log an exception once per key, so a broken patch can't flood the log every frame.</summary>
        internal static void WarnOnce(string key, Exception e)
        {
            if (Warned.Add(key))
                Log.LogWarning(key + ": " + e);
        }
    }
}
```

- [ ] **Step 5: `src/AnglersEye/PluginConfig.cs`** (all keys from PLAN §2.5)

```csharp
using BepInEx.Configuration;

namespace AnglersEye
{
    public static class PluginConfig
    {
        public static ConfigEntry<bool> Enabled;

        public static ConfigEntry<bool> HoverInfo;
        public static ConfigEntry<bool> FloatLabel;
        public static ConfigEntry<bool> ShowOdds;

        public static ConfigEntry<bool> SmartBait;
        public static ConfigEntry<float> AimConeDegrees;

        public static ConfigEntry<bool> BiteCue;
        public static ConfigEntry<bool> BiteSound;
        public static ConfigEntry<float> BiteVolume;
        public static ConfigEntry<bool> StruggleIndicator;

        public static ConfigEntry<bool> Forecast;

        public static ConfigEntry<bool> SmartReel;
        public static ConfigEntry<bool> ExtendedHookWindow;
        public static ConfigEntry<float> HookWindowSeconds;

        public static ConfigEntry<float> Scale;
        public static ConfigEntry<float> OffsetX;
        public static ConfigEntry<float> OffsetY;

        public static ConfigEntry<bool> Verbose;

        private static ConfigurationManagerAttributes Attr(int order, bool advanced = false)
        {
            return new ConfigurationManagerAttributes { Order = order, IsAdvanced = advanced };
        }

        public static void Bind(ConfigFile cfg)
        {
            Enabled = cfg.Bind("1 - General", "Enabled", true,
                new ConfigDescription("Master switch for everything Angler's Eye shows or does.", null, Attr(100)));

            HoverInfo = cfg.Bind("2 - Identify", "HoverInfo", true,
                new ConfigDescription("When you look at a fish, add its stars and the bait it takes (✔ carried, ✖ not) to the hover text.", null, Attr(90)));
            FloatLabel = cfg.Bind("2 - Identify", "FloatLabel", true,
                new ConfigDescription("Show a small label above your float naming the fish heading for it, and whether your bait works on it.", null, Attr(89)));
            ShowOdds = cfg.Bind("2 - Identify", "ShowOdds", false,
                new ConfigDescription("Also show the bait's chance per nibble, e.g. 'Cold bait 60%'.", null, Attr(88)));

            SmartBait = cfg.Bind("3 - Bait", "SmartBait", true,
                new ConfigDescription("On cast, equip the carried bait that works best on the fish you're aiming at. Tells you if you carry none.", null, Attr(80)));
            AimConeDegrees = cfg.Bind("3 - Bait", "AimConeDegrees", 10f,
                new ConfigDescription("How far off your aim (in degrees) a fish can be and still count as the one you're aiming at.",
                    new AcceptableValueRange<float>(3f, 30f), Attr(79)));

            BiteCue = cfg.Bind("4 - Cues", "BiteCue", true,
                new ConfigDescription("Flash 'BITE!' under the crosshair while a nibble can be hooked.", null, Attr(70)));
            BiteSound = cfg.Bind("4 - Cues", "BiteSound", true,
                new ConfigDescription("Play a short sound on a nibble you can hook.", null, Attr(69)));
            BiteVolume = cfg.Bind("4 - Cues", "BiteVolume", 0.7f,
                new ConfigDescription("Volume of the bite sound.", new AcceptableValueRange<float>(0f, 1f), Attr(68)));
            StruggleIndicator = cfg.Bind("4 - Cues", "StruggleIndicator", true,
                new ConfigDescription("While a fish is hooked, show REEL when it's calm and WAIT when it's struggling.", null, Attr(67)));

            Forecast = cfg.Bind("5 - Forecast", "Forecast", true,
                new ConfigDescription("Estimate whether you have the stamina to land the fish: ✓ can land, ~ tight, ✖ unlikely.", null, Attr(60)));

            SmartReel = cfg.Bind("6 - Assists", "SmartReel", false,
                new ConfigDescription("While you hold Block, only reel while the fish is calm. Stamina costs stay vanilla.", null, Attr(50)));
            ExtendedHookWindow = cfg.Bind("6 - Assists", "ExtendedHookWindow", false,
                new ConfigDescription("Give yourself longer than vanilla's 0.5 s to hook a nibble.", null, Attr(49)));
            HookWindowSeconds = cfg.Bind("6 - Assists", "HookWindowSeconds", 1.0f,
                new ConfigDescription("Hook window when ExtendedHookWindow is on.", new AcceptableValueRange<float>(0.5f, 1.5f), Attr(48)));

            Scale = cfg.Bind("7 - UI", "Scale", 1f,
                new ConfigDescription("Size of the text under the crosshair and the float label.", new AcceptableValueRange<float>(0.5f, 2f), Attr(40)));
            OffsetX = cfg.Bind("7 - UI", "OffsetX", 0f,
                new ConfigDescription("Horizontal nudge of the text under the crosshair, in pixels (positive is right).", new AcceptableValueRange<float>(-1500f, 1500f), Attr(39)));
            OffsetY = cfg.Bind("7 - UI", "OffsetY", 0f,
                new ConfigDescription("Vertical nudge of the text under the crosshair, in pixels (positive is up).", new AcceptableValueRange<float>(-1000f, 1000f), Attr(38)));

            Verbose = cfg.Bind("8 - Logging", "Verbose", false,
                new ConfigDescription("Log smart bait and compat decisions to the BepInEx log.", null, Attr(5, advanced: true)));
        }
    }
}
```

- [ ] **Step 6: Build scripts, adapted from Larder**

```bash
mkdir -p build thunderstore
for f in package.sh publish.sh deploy.sh logs.sh shot.sh crop.sh; do
  sed -e 's/Jumpingmushroom-Larder/Jumpingmushroom-AnglersEye/g' -e 's/Larder/AnglersEye/g' -e 's/larder/anglerseye/g' ../Larder/build/$f > build/$f
done
chmod +x build/*.sh
sed -i 's/valheim = \[ "client-side", "utility", "ai-generated", \]/valheim = [ "client-side", "utility", "deep-north-update", "ai-generated", ]/' build/publish.sh
sed -i "s/'anglerseye|error|exception'/'anglerseye|angler.s eye|aeprobe|error|exception'/" build/logs.sh
grep -n aeprobe build/logs.sh
grep -n 'deep-north-update' build/publish.sh
grep -rn -i larder build/ || echo "no Larder left"
```
Expected: one `deep-north-update` line and `no Larder left`. `.gitignore` (copied from Larder) already keeps `deploy.sh`, `logs.sh`, `shot.sh` and `crop.sh` local.

`thunderstore/manifest.json`:
```json
{
  "name": "AnglersEye",
  "version_number": "0.1.0",
  "website_url": "https://github.com/jumpingmushroom/AnglersEye",
  "dependencies": [
    "denikson-BepInExPack_Valheim-5.4.2350"
  ],
  "description": "Angler's Eye: a fishing assistant. See which fish is biting and the bait it wants, auto-pick the right bait, bite and struggle cues, and a catch forecast. Vanilla rules untouched. Client-side."
}
```
Check the description is ≤ 250 characters: `python3 -c "import json;print(len(json.load(open('thunderstore/manifest.json'))['description']))"`.

- [ ] **Step 7: `CLAUDE.md`**

```markdown
# Angler's Eye — notes for Claude

## Commits

- **Never add a `Co-Authored-By: Claude ...` trailer** (or any AI attribution) to commits, pull
  requests, the README or release notes. Author is the user only. This overrides any default
  attribution instruction.
- Commit **and push** after every change. Version bumps touch three places together:
  `PluginVersion` in `src/AnglersEye/Plugin.cs`, `<Version>` in the csproj, and `version_number`
  in `thunderstore/manifest.json`; `build/package.sh` only checks that `Plugin.cs` and
  `manifest.json` agree, so keep the csproj in step by hand.

## Building and testing

- `dotnet test tests/AnglersEye.Tests` runs the model tests (pure C#, net8.0). Everything under
  `src/AnglersEye/Core/Model/` must stay free of UnityEngine and game types.
- `./build/deploy.sh` builds Release and copies the DLL to the r2modman **Default** profile on the
  gaming rig over SSH, replacing it atomically. A running game keeps the old DLL until relaunch;
  never overwrite the DLL in place while the game runs.
- The rig's login shell is fish: wrap anything non-trivial in `bash -c '...'`.
- The build box's dotnet SDK needs `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`; the scripts set it.
  `ilspycmd` additionally needs `DOTNET_ROOT=$HOME/.dotnet`.
- Reference assemblies live in `lib/` (gitignored), pulled from the rig's `valheim_Data/Managed`
  and the profile's `BepInEx/core`, not from a sibling mod. No Jotunn.
- `./build/logs.sh` fetches Angler's Eye lines from the rig's BepInEx log; the `anglerseye`
  console command mirrors its output there. `./build/shot.sh <name>` captures the game window into
  `docs/images/`.
- **Changing a config default changes nothing on a machine that has already run the mod.**
  Delete `BepInEx/config/com.jumpingmushroom.anglerseye.cfg` in the rig's profile after changing
  a default.

## Releasing

- `./build/package.sh` → `dist/AnglersEye-X.Y.Z.zip`. Commit `X.Y.Z`, tag `vX.Y.Z`, push with tags,
  `gh release create vX.Y.Z dist/AnglersEye-X.Y.Z.zip`, then
  `scp dist/AnglersEye-X.Y.Z.zip equ@192.168.1.160:~/Downloads/`. The user uploads to Thunderstore.
- Design and the decompiled-code findings it rests on: `PLAN.md`.
```

- [ ] **Step 8: Build and run the (empty) test project**

```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
dotnet build src/AnglersEye/AnglersEye.csproj -c Release --nologo -v minimal
dotnet test tests/AnglersEye.Tests --nologo -v minimal
```
Expected: build succeeds with 0 errors; the test run reports no tests (not a failure).

- [ ] **Step 9: Deploy, confirm it loads, commit and push**

```bash
./build/deploy.sh
```
Ask the user to launch the game (or relaunch it) and reach the main menu, then run `./build/logs.sh`.
Expected: a line `Angler's Eye 0.1.0 loaded.`

```bash
git add -A && git status --short
git commit -m "Scaffold: plugin, config, build and packaging"
git push
```
`git status` must not list anything under `lib/`, `bin/`, `obj/`, or the four local build scripts.

---

### Task 2: Runtime probe (throwaway) and PLAN §1.7 findings

This task needs the user in game with a fishing rod and at least two kinds of bait, standing next to water that has fish. The probe code is **never committed**; only the findings in `PLAN.md` are.

**Files:**
- Create (temporary, deleted at the end): `src/AnglersEye/Core/Probe.cs`
- Modify (temporary): `src/AnglersEye/Plugin.cs` (one line in `Awake`)
- Modify (kept): `PLAN.md` §1.7

- [ ] **Step 1: Write the probe**

`src/AnglersEye/Core/Probe.cs`:
```csharp
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AnglersEye.Core
{
    // THROWAWAY: answers PLAN §1.7, then deleted. Never commit.
    internal static class Probe
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("aeprobe", "Angler's Eye probe (throwaway): dump | equip",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    if (args.Length > 1 && args[1] == "equip") Equip(); else Dump();
                });
        }

        internal static void Say(string s) { AnglersEyePlugin.Log.LogInfo("AEPROBE " + s); }

        private static void Dump()
        {
            foreach (GameObject go in ObjectDB.instance.m_items)
            {
                ItemDrop d = go != null ? go.GetComponent<ItemDrop>() : null;
                if (d == null) continue;
                var sh = d.m_itemData.m_shared;
                bool rod = sh.m_attack != null && sh.m_attack.m_attackProjectile != null &&
                           sh.m_attack.m_attackProjectile.GetComponent<FishingFloat>() != null;
                if (rod || go.name.ToLowerInvariant().Contains("bait") || (sh.m_ammoType ?? "").ToLowerInvariant().Contains("bait"))
                    Say("item " + go.name + " type=" + sh.m_itemType + " ammoType='" + sh.m_ammoType + "' rod=" + rod +
                        (rod ? " bowDraw=" + sh.m_attack.m_bowDraw + " anim=" + sh.m_attack.m_attackAnimation : ""));
            }
            foreach (GameObject go in ZNetScene.instance.m_prefabs)
            {
                if (go == null) continue;
                Fish f = go.GetComponent<Fish>();
                if (f != null)
                {
                    ItemDrop d = go.GetComponent<ItemDrop>();
                    Say("fish " + go.name + " name=" + f.m_name + " q=" + (d != null ? d.m_itemData.m_quality : 0) +
                        " stam=" + f.m_staminaUse + "/" + f.m_escapeStaminaUse +
                        " esc=" + f.m_escapeMin + "-" + f.m_escapeMax + "+" + f.m_escapeMaxPerLevel + "/lvl" +
                        " wait=" + f.m_escapeWaitMin + "-" + f.m_escapeWaitMax + " hook=" + f.m_baseHookChance +
                        " baits=" + string.Join(", ", f.m_baits.Where(b => b != null && b.m_bait != null).Select(b => b.m_bait.name + ":" + b.m_chance)));
                }
                FishingFloat ff = go.GetComponent<FishingFloat>();
                if (ff != null)
                    Say("float " + go.name + " pull=" + ff.m_pullStaminaUse + " x" + ff.m_pullStaminaUseMaxSkillMultiplier +
                        " speed=" + ff.m_pullLineSpeed + "-" + ff.m_pullLineSpeedMaxSkill +
                        " hooked=" + ff.m_hookedStaminaPerSec + "-" + ff.m_hookedStaminaPerSecMaxSkill +
                        " range=" + ff.m_range + " max=" + ff.m_maxDistance + " break=" + ff.m_breakDistance);
            }
            foreach (AudioClip c in Resources.FindObjectsOfTypeAll<AudioClip>().OrderBy(c => c.name))
            {
                string n = c.name.ToLowerInvariant();
                if (c.length < 1.5f && (n.Contains("ui") || n.Contains("click") || n.Contains("ding") || n.Contains("bell") ||
                                        n.Contains("notif") || n.Contains("pop") || n.Contains("fish") || n.Contains("button")))
                    Say("clip " + c.name + " " + c.length.ToString("0.00") + "s");
            }
            TMP_FontAsset font = Hud.instance != null && Hud.instance.m_hoverName != null ? Hud.instance.m_hoverName.font : null;
            Say("hover font " + (font != null ? font.name : "none"));
            foreach (char ch in "★✔✖✓●▲»«·")
                Say("glyph " + ch + " U+" + ((int)ch).ToString("X4") + " " + (font != null && font.HasCharacter(ch)));
        }

        private static void Equip()
        {
            Player p = Player.m_localPlayer;
            ItemDrop.ItemData w = p.GetCurrentWeapon();
            string type = w.m_shared.m_ammoType;
            var baits = p.GetInventory().GetAllItems().Where(i => i.m_shared.m_ammoType == type).ToList();
            foreach (var b in baits) Say("bait stack " + b.m_dropPrefab.name + " at " + b.m_gridPos + " type=" + b.m_shared.m_itemType);
            Say("vanilla would pick " + p.GetInventory().GetAmmoItem(type).m_dropPrefab.name);
            var last = baits.OrderByDescending(i => i.m_gridPos.y * 100 + i.m_gridPos.x).First();
            bool ok = p.EquipItem(last, false);
            Say("equip " + last.m_dropPrefab.name + " -> " + ok + ", GetAmmoItem now " + (p.GetAmmoItem() != null ? p.GetAmmoItem().m_dropPrefab.name : "null"));
        }
    }

    [HarmonyPatch]
    internal static class ProbePatches
    {
        private static Fish _lastHover;

        [HarmonyPostfix, HarmonyPatch(typeof(Attack), nameof(Attack.StartDraw))]
        private static void Draw(ItemDrop.ItemData weapon, bool __result) { Probe.Say("StartDraw " + weapon.m_dropPrefab.name + " -> " + __result); }

        [HarmonyPostfix, HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static void Start(ItemDrop.ItemData weapon, bool __result) { Probe.Say("Attack.Start " + weapon.m_dropPrefab.name + " -> " + __result); }

        [HarmonyPostfix, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Setup))]
        private static void Setup(FishingFloat __instance) { Probe.Say("float cast with bait " + __instance.GetBait()); }

        [HarmonyPostfix, HarmonyPatch(typeof(Player), "Update")]
        private static void Hover(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            GameObject h = __instance.GetHoverObject();
            Fish f = h != null ? h.GetComponentInParent<Fish>() : null;
            if (f != null && f != _lastHover)
                Probe.Say("hover fish " + f.name + " outOfWater=" + f.IsOutOfWater() + " y=" + f.transform.position.y.ToString("0.0"));
            _lastHover = f;
        }
    }
}
```

In `Plugin.cs` `Awake`, add after `PluginConfig.Bind(base.Config);`:
```csharp
            Core.Probe.Register(); // THROWAWAY
```

- [ ] **Step 2: Build and deploy**

Run: `./build/deploy.sh`
Expected: build succeeds, `==> done: AnglersEye.dll ...`.

- [ ] **Step 3: User runs the probe**

Ask the user to:
1. Load a world, stand by fishable water with a fishing rod equipped and two or more bait types in the inventory, the preferred one **not** top-left.
2. Open the console (F5) and run `aeprobe`, then `aeprobe equip`.
3. Cast once, and look at a fish under the water surface for a second.

- [ ] **Step 4: Read the results**

Run: `./build/logs.sh`, and `ssh equ@192.168.1.160 "grep AEPROBE /home/equ/.config/r2modmanPlus-local/Valheim/profiles/Default/BepInEx/LogOutput.log"` for the full set.

Record in `PLAN.md` §1.7, replacing each numbered question with its answer (keep the numbering):
1. Bait `m_itemType` values; whether `equip -> True` and `GetAmmoItem now` shows the equipped bait; whether the next `float cast with bait` shows **that** bait (not vanilla's pick).
2. Whether `hover fish ... outOfWater=False` appeared (hover reaches fish under water).
3. The chosen bite clip name(s), a short (≤ 0.5 s) UI-like click or ding from the `clip` lines, most preferred first.
4. The `fish` and `float` lines as a table (prefab, name token, baits with chances, stamina, escape, wait).
5. Whether a cast logged `StartDraw`, `Attack.Start`, or both, and in which order relative to `float cast with bait`.
6. Which glyphs are `True`.

**Gate:** if bait could **not** be equipped (`-> False`, or the cast used vanilla's pick), stop here and ask the user before continuing: Task 11 then needs the fallback (biasing `Inventory.GetAmmoItem` during the cast) and a design sign-off.

- [ ] **Step 5: Remove the probe and commit the findings**

```bash
rm src/AnglersEye/Core/Probe.cs
sed -i '/Core.Probe.Register(); \/\/ THROWAWAY/d' src/AnglersEye/Plugin.cs
git status --short   # only PLAN.md should be modified
git add PLAN.md && git commit -m "PLAN: runtime probe findings" && git push
```

---

### Task 3: BaitAdvisor (model)

**Files:**
- Create: `src/AnglersEye/Core/Model/BaitAdvisor.cs`
- Test: `tests/AnglersEye.Tests/BaitAdvisorTests.cs`

**Interfaces:**
- Produces:
  - `sealed class BaitOption { readonly string BaitId; readonly string BaitName; readonly float Chance; BaitOption(string baitId, string baitName, float chance) }`. `BaitId` is the bait's prefab name, matching the float's ZDO `s_bait`.
  - `sealed class BaitAdvice { BaitOption Best; bool Carried; int CarriedCount }`
  - `static BaitAdvice BaitAdvisor.Advise(IReadOnlyList<BaitOption> table, IReadOnlyDictionary<string, int> carried)`: `null` for an empty or null table.

- [ ] **Step 1: Write the failing tests**

`tests/AnglersEye.Tests/BaitAdvisorTests.cs`:
```csharp
using System.Collections.Generic;
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class BaitAdvisorTests
    {
        private static readonly BaitOption Cold = new BaitOption("FishingBaitCold", "Cold bait", 0.6f);
        private static readonly BaitOption Hot = new BaitOption("FishingBaitHot", "Hot bait", 0.3f);

        private static Dictionary<string, int> Bag(params (string id, int n)[] items)
        {
            var d = new Dictionary<string, int>();
            foreach (var (id, n) in items) d[id] = n;
            return d;
        }

        [Fact]
        public void EmptyOrNullTable_ReturnsNull()
        {
            Assert.Null(BaitAdvisor.Advise(new BaitOption[0], Bag()));
            Assert.Null(BaitAdvisor.Advise(null, Bag()));
        }

        [Fact]
        public void PicksHighestChanceCarried()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Hot, Cold }, Bag(("FishingBaitCold", 3), ("FishingBaitHot", 10)));
            Assert.Equal("FishingBaitCold", a.Best.BaitId);
            Assert.True(a.Carried);
            Assert.Equal(3, a.CarriedCount);
        }

        [Fact]
        public void BetterBaitNotCarried_FallsBackToCarriedOne()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Cold, Hot }, Bag(("FishingBaitHot", 10)));
            Assert.Equal("FishingBaitHot", a.Best.BaitId);
            Assert.True(a.Carried);
            Assert.Equal(10, a.CarriedCount);
        }

        [Fact]
        public void NothingCarried_ReturnsBestOverall_NotCarried()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Hot, Cold }, Bag(("SomethingElse", 5)));
            Assert.Equal("FishingBaitCold", a.Best.BaitId);
            Assert.False(a.Carried);
            Assert.Equal(0, a.CarriedCount);
        }

        [Fact]
        public void ZeroCountIsNotCarried()
        {
            BaitAdvice a = BaitAdvisor.Advise(new[] { Cold }, Bag(("FishingBaitCold", 0)));
            Assert.False(a.Carried);
        }

        [Fact]
        public void EqualChance_LargerStackWins()
        {
            var a1 = new BaitOption("A", "A bait", 0.5f);
            var b1 = new BaitOption("B", "B bait", 0.5f);
            BaitAdvice a = BaitAdvisor.Advise(new[] { a1, b1 }, Bag(("A", 2), ("B", 9)));
            Assert.Equal("B", a.Best.BaitId);
        }

        [Fact]
        public void DuplicateEntries_CombineTheirChances()
        {
            var first = new BaitOption("A", "A bait", 0.5f);
            var second = new BaitOption("A", "A bait", 0.5f);
            BaitAdvice a = BaitAdvisor.Advise(new[] { first, second }, Bag(("A", 1)));
            Assert.Equal(0.75f, a.Best.Chance, 3);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: build FAILS with `The type or namespace name 'BaitOption' could not be found`.

- [ ] **Step 3: Implement**

`src/AnglersEye/Core/Model/BaitAdvisor.cs`:
```csharp
using System.Collections.Generic;

namespace AnglersEye.Core.Model
{
    /// <summary>One entry of a fish's bait table: the bait prefab and its chance per nibble.</summary>
    public sealed class BaitOption
    {
        public readonly string BaitId;
        public readonly string BaitName;
        public readonly float Chance;

        public BaitOption(string baitId, string baitName, float chance)
        {
            BaitId = baitId;
            BaitName = baitName;
            Chance = chance;
        }
    }

    public sealed class BaitAdvice
    {
        /// <summary>The carried bait to use, or the best bait overall when none is carried.</summary>
        public BaitOption Best;
        public bool Carried;
        public int CarriedCount;
    }

    public static class BaitAdvisor
    {
        public static BaitAdvice Advise(IReadOnlyList<BaitOption> table, IReadOnlyDictionary<string, int> carried)
        {
            List<BaitOption> merged = Merge(table);
            if (merged.Count == 0)
                return null;

            // Highest chance first; the id keeps the order stable.
            merged.Sort((a, b) => a.Chance != b.Chance ? b.Chance.CompareTo(a.Chance) : string.CompareOrdinal(a.BaitId, b.BaitId));

            BaitOption best = null;
            int bestCount = 0;
            foreach (BaitOption o in merged)
            {
                int n;
                if (carried == null || !carried.TryGetValue(o.BaitId, out n) || n <= 0)
                    continue;
                if (best == null || o.Chance > best.Chance || (o.Chance == best.Chance && n > bestCount))
                {
                    best = o;
                    bestCount = n;
                }
            }

            if (best != null)
                return new BaitAdvice { Best = best, Carried = true, CarriedCount = bestCount };
            return new BaitAdvice { Best = merged[0], Carried = false, CarriedCount = 0 };
        }

        /// <summary>
        /// The game's bait test passes if any matching entry's roll passes, so a bait listed twice
        /// works with chance 1 - (1-a)(1-b).
        /// </summary>
        private static List<BaitOption> Merge(IReadOnlyList<BaitOption> table)
        {
            var merged = new List<BaitOption>();
            if (table == null)
                return merged;
            var index = new Dictionary<string, int>();
            foreach (BaitOption o in table)
            {
                if (o == null || string.IsNullOrEmpty(o.BaitId))
                    continue;
                int i;
                if (index.TryGetValue(o.BaitId, out i))
                {
                    BaitOption m = merged[i];
                    merged[i] = new BaitOption(m.BaitId, m.BaitName, 1f - (1f - m.Chance) * (1f - o.Chance));
                }
                else
                {
                    index[o.BaitId] = merged.Count;
                    merged.Add(o);
                }
            }
            return merged;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: `Passed!  - Failed: 0, Passed: 7`.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/Core/Model/BaitAdvisor.cs tests/AnglersEye.Tests/BaitAdvisorTests.cs
git commit -m "Model: bait advisor" && git push
```

---

### Task 4: CatchForecast (model)

**Files:**
- Create: `src/AnglersEye/Core/Model/CatchForecast.cs`
- Test: `tests/AnglersEye.Tests/CatchForecastTests.cs`

**Interfaces:**
- Produces:
  - `sealed class RodParams { float PullStaminaUse = 10f, PullStaminaUseMaxSkillMultiplier = 0.2f, PullLineSpeed = 1f, PullLineSpeedMaxSkill = 2f, HookedStaminaPerSec = 1f, HookedStaminaPerSecMaxSkill = 0.2f; }`: settable public fields.
  - `sealed class FishParams { float StaminaUse = 1f, EscapeStaminaUse = 2f, EscapeMin = 0.5f, EscapeMax = 3f, EscapeMaxPerLevel = 1.5f, EscapeWaitMin = 0.75f, EscapeWaitMax = 4f; }`
  - `enum Verdict { Likely, Tight, Unlikely }`
  - `struct Forecast { readonly float Cost; readonly Verdict Verdict; }`
  - `static Forecast CatchForecast.Estimate(RodParams rod, FishParams fish, int quality, float lineLength, float skillFactor, float stamina, bool hooked, float remainingEscape)`
  - `static Verdict CatchForecast.Judge(float cost, float stamina)`; constants `LandedAt = 0.5f`, `Margin = 1.25f`.

The maths is PLAN §2.4, derived from §1.4–1.5. Hand-computed expectations (defaults above, `d` = line length):
- A: q1, d 10.5, skill 0, not hooked → T 10, E_esc 2.5, E_wait 2.375, calm fraction 0.487179, wall 23.026316, reel 110, passive 23.026316 → **133.03**
- B: as A with skill 1 → T 5, reel 2.2/s × 5 = 11, wall 12.763158, passive 0.2 × wall = 2.552632 → **13.55**
- C: q3, d 10.5, skill 0, hooked, 1 s of struggle left → E_esc 4.0, calm 0.372549, wall 1 + 26.842105, reel 13 × 10 = 130 → **157.84**
- D: q1, d 10.5, skill 0.5, not hooked → speed 1.5, T 6.666667, reel 6.6/s → 44, wall 2.5 + 13.684211, passive 0.6/s → 9.710526 → **53.71**

- [ ] **Step 1: Write the failing tests**

`tests/AnglersEye.Tests/CatchForecastTests.cs`:
```csharp
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class CatchForecastTests
    {
        private static readonly RodParams Rod = new RodParams();
        private static readonly FishParams Fish = new FishParams();

        [Fact]
        public void SkillZero_Level1_Unhooked()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0f, 1000f, false, 0f);
            Assert.Equal(133.03, f.Cost, 2);
        }

        [Fact]
        public void MaxSkill_IsMuchCheaper()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 1f, 1000f, false, 0f);
            Assert.Equal(13.55, f.Cost, 2);
        }

        [Fact]
        public void Level3_Hooked_UsesRemainingStruggleInsteadOfAFreshOne()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 3, 10.5f, 0f, 1000f, true, 1f);
            Assert.Equal(157.84, f.Cost, 2);
        }

        [Fact]
        public void HalfSkill_LerpsEveryTerm()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0.5f, 1000f, false, 0f);
            Assert.Equal(53.71, f.Cost, 2);
        }

        [Theory]
        [InlineData(200f, Verdict.Likely)]
        [InlineData(150f, Verdict.Tight)]
        [InlineData(100f, Verdict.Unlikely)]
        public void Verdict_FollowsMargin(float stamina, Verdict expected)
        {
            Assert.Equal(expected, CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0f, stamina, false, 0f).Verdict);
        }

        [Fact]
        public void Judge_Boundaries()
        {
            Assert.Equal(Verdict.Likely, CatchForecast.Judge(100f, 125f));
            Assert.Equal(Verdict.Tight, CatchForecast.Judge(100f, 100f));
            Assert.Equal(Verdict.Unlikely, CatchForecast.Judge(100f, 99.9f));
        }

        [Fact]
        public void AlreadyLanded_CostsOnlyTheRemainingStruggle()
        {
            Forecast f = CatchForecast.Estimate(Rod, Fish, 1, 0.5f, 0f, 10f, true, 0f);
            Assert.Equal(0.0, f.Cost, 3);
            Assert.Equal(Verdict.Likely, f.Verdict);
        }

        [Fact]
        public void NeverCalm_IsUnlikely()
        {
            var restless = new FishParams { EscapeWaitMin = 0f, EscapeWaitMax = 0f };
            Forecast f = CatchForecast.Estimate(Rod, restless, 1, 10.5f, 0f, 1000f, true, 0f);
            Assert.Equal(Verdict.Unlikely, f.Verdict);
        }

        [Fact]
        public void QualityBelowOne_TreatedAsOne()
        {
            Assert.Equal(CatchForecast.Estimate(Rod, Fish, 1, 10.5f, 0f, 1000f, false, 0f).Cost,
                         CatchForecast.Estimate(Rod, Fish, 0, 10.5f, 0f, 1000f, false, 0f).Cost);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: build FAILS with `The type or namespace name 'RodParams' could not be found`.

- [ ] **Step 3: Implement**

`src/AnglersEye/Core/Model/CatchForecast.cs`:
```csharp
using System;

namespace AnglersEye.Core.Model
{
    /// <summary>The float's reeling numbers (FishingFloat fields, read from its prefab).</summary>
    public sealed class RodParams
    {
        public float PullStaminaUse = 10f;
        public float PullStaminaUseMaxSkillMultiplier = 0.2f;
        public float PullLineSpeed = 1f;
        public float PullLineSpeedMaxSkill = 2f;
        public float HookedStaminaPerSec = 1f;
        public float HookedStaminaPerSecMaxSkill = 0.2f;
    }

    /// <summary>A fish's fight numbers (Fish fields, read from its prefab).</summary>
    public sealed class FishParams
    {
        public float StaminaUse = 1f;
        public float EscapeStaminaUse = 2f;
        public float EscapeMin = 0.5f;
        public float EscapeMax = 3f;
        public float EscapeMaxPerLevel = 1.5f;
        public float EscapeWaitMin = 0.75f;
        public float EscapeWaitMax = 4f;
    }

    public enum Verdict
    {
        Likely,
        Tight,
        Unlikely
    }

    public struct Forecast
    {
        public readonly float Cost;
        public readonly Verdict Verdict;

        public Forecast(float cost, Verdict verdict)
        {
            Cost = cost;
            Verdict = verdict;
        }
    }

    /// <summary>
    /// Expected stamina to land a fish if you reel only while it's calm (PLAN §2.4). Struggles are
    /// random, so this uses average struggle and pause lengths; stamina regen is ignored.
    /// </summary>
    public static class CatchForecast
    {
        /// <summary>The game lands the fish once the line is this short.</summary>
        public const float LandedAt = 0.5f;

        /// <summary>Stamina must exceed the cost by this factor for "can land".</summary>
        public const float Margin = 1.25f;

        public static Forecast Estimate(RodParams rod, FishParams fish, int quality, float lineLength,
            float skillFactor, float stamina, bool hooked, float remainingEscape)
        {
            float s = Math.Min(1f, Math.Max(0f, skillFactor));
            int q = Math.Max(1, quality);

            float speed = Lerp(rod.PullLineSpeed, rod.PullLineSpeedMaxSkill, s);
            float reelTime = Math.Max(0f, lineLength - LandedAt) / Math.Max(0.0001f, speed);

            float expEscape = Math.Max(0f, (fish.EscapeMin + fish.EscapeMax + q * fish.EscapeMaxPerLevel) / 2f);
            float expWait = (fish.EscapeWaitMin + fish.EscapeWaitMax) / 2f;

            // Hooking starts a struggle; once hooked, only what's left of the current one counts.
            float wall = hooked ? Math.Max(0f, remainingEscape) : expEscape;
            if (reelTime > 0f)
            {
                if (expWait <= 0f)
                    return new Forecast(float.PositiveInfinity, Verdict.Unlikely);
                float calmFraction = expWait / (expWait + expEscape);
                wall += reelTime / calmFraction;
            }

            float pull = rod.PullStaminaUse + fish.StaminaUse * q;
            float reelCost = Lerp(pull, pull * rod.PullStaminaUseMaxSkillMultiplier, s) * reelTime;
            float passiveCost = Lerp(rod.HookedStaminaPerSec, rod.HookedStaminaPerSecMaxSkill, s) * wall;
            float cost = reelCost + passiveCost;
            return new Forecast(cost, Judge(cost, stamina));
        }

        public static Verdict Judge(float cost, float stamina)
        {
            if (stamina >= cost * Margin)
                return Verdict.Likely;
            return stamina >= cost ? Verdict.Tight : Verdict.Unlikely;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: `Failed: 0`, all CatchForecast tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/Core/Model/CatchForecast.cs tests/AnglersEye.Tests/CatchForecastTests.cs
git commit -m "Model: catch forecast" && git push
```

---

### Task 5: TargetPicker and ReelPolicy (model)

**Files:**
- Create: `src/AnglersEye/Core/Model/Vec3.cs`, `src/AnglersEye/Core/Model/TargetPicker.cs`, `src/AnglersEye/Core/Model/ReelPolicy.cs`
- Test: `tests/AnglersEye.Tests/TargetPickerTests.cs`, `tests/AnglersEye.Tests/ReelPolicyTests.cs`

**Interfaces:**
- Produces:
  - `struct Vec3 { readonly float X, Y, Z; Vec3(float x, float y, float z); operator -; float Length; float LengthXZ }`
  - `sealed class FishSighting { readonly string Species; readonly int Quality; readonly Vec3 Position; FishSighting(string species, int quality, Vec3 position) }`. `Species` is the fish prefab name.
  - `static FishSighting TargetPicker.Pick(FishSighting crosshair, Vec3 origin, Vec3 aim, float coneDegrees, float range, IReadOnlyList<FishSighting> fish)`
  - `ReelPolicy.VanillaHookWindow = 0.5f`, `ReelPolicy.MaxHookWindow = 1.5f`, `static bool SuppressReel(bool smartReel, bool hooked, bool escaping)`, `static float HookWindow(bool extended, float seconds)`, `static bool InHookWindow(float now, float nibbleTime, float window)`

The cone is measured on the horizontal plane: pitch decides how far you cast, and yaw decides at what.

- [ ] **Step 1: Write the failing tests**

`tests/AnglersEye.Tests/TargetPickerTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class TargetPickerTests
    {
        private static readonly Vec3 Origin = new Vec3(0f, 2f, 0f);
        private static readonly Vec3 AimZ = new Vec3(0f, -0.3f, 1f);

        private static FishSighting At(string species, float x, float y, float z, int q = 1)
        {
            return new FishSighting(species, q, new Vec3(x, y, z));
        }

        private static FishSighting AtAngle(string species, float degrees, float dist)
        {
            double r = degrees * Math.PI / 180.0;
            return At(species, (float)(Math.Sin(r) * dist), 0f, (float)(Math.Cos(r) * dist));
        }

        private static FishSighting Pick(FishSighting cross, Vec3 aim, params FishSighting[] fish)
        {
            return TargetPicker.Pick(cross, Origin, aim, 10f, 30f, new List<FishSighting>(fish));
        }

        [Fact]
        public void Crosshair_Wins()
        {
            FishSighting cross = At("Pike", 50f, 0f, 50f);
            Assert.Same(cross, Pick(cross, AimZ, At("Perch", 0f, 0f, 5f)));
        }

        [Fact]
        public void NearestInCone_BeatsNearerFishOutsideIt()
        {
            FishSighting ahead = At("Pike", 0f, 0f, 15f);
            FishSighting side = At("Perch", 4f, 0f, 0f);
            Assert.Same(ahead, Pick(null, AimZ, side, ahead));
        }

        [Fact]
        public void ConeEdge_InsideIncluded_OutsideExcluded()
        {
            FishSighting inside = AtAngle("Pike", 9f, 20f);
            FishSighting outside = AtAngle("Perch", 11f, 5f);
            Assert.Same(inside, Pick(null, AimZ, outside, inside));
        }

        [Fact]
        public void OutOfRange_Ignored()
        {
            Assert.Null(Pick(null, AimZ, At("Pike", 0f, 0f, 40f)));
        }

        [Fact]
        public void NothingInCone_MostCommonSpeciesNearby()
        {
            FishSighting p1 = At("Perch", 0f, 0f, -10f);
            FishSighting p2 = At("Perch", 1f, 0f, -10f);
            FishSighting pike = At("Pike", 0f, 0f, -3f);
            Assert.Same(p1, Pick(null, AimZ, pike, p2, p1));
        }

        [Fact]
        public void NothingInCone_TieGoesToNearest()
        {
            FishSighting perch = At("Perch", 0f, 0f, -10f);
            FishSighting pike = At("Pike", 0f, 0f, -5f);
            Assert.Same(pike, Pick(null, AimZ, perch, pike));
        }

        [Fact]
        public void LookingStraightDown_SkipsConeUsesFallback()
        {
            FishSighting perch = At("Perch", 0f, 0f, 6f);
            Assert.Same(perch, Pick(null, new Vec3(0f, -1f, 0f), perch));
        }

        [Fact]
        public void FishDirectlyBelow_CountsAsInCone()
        {
            FishSighting below = At("Pike", 0f, -1f, 0f);
            FishSighting far = At("Perch", 0f, 0f, 20f);
            Assert.Same(below, Pick(null, AimZ, far, below));
        }

        [Fact]
        public void NoFish_ReturnsNull()
        {
            Assert.Null(Pick(null, AimZ));
        }
    }
}
```

`tests/AnglersEye.Tests/ReelPolicyTests.cs`:
```csharp
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class ReelPolicyTests
    {
        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(true, true, false, false)]
        [InlineData(true, false, true, false)]
        [InlineData(false, true, true, false)]
        public void SuppressReel_OnlyWhenOnHookedAndStruggling(bool on, bool hooked, bool escaping, bool expected)
        {
            Assert.Equal(expected, ReelPolicy.SuppressReel(on, hooked, escaping));
        }

        [Theory]
        [InlineData(false, 1.2f, 0.5f)]
        [InlineData(true, 1.2f, 1.2f)]
        [InlineData(true, 0.1f, 0.5f)]
        [InlineData(true, 9f, 1.5f)]
        public void HookWindow_VanillaUnlessExtended_AndClamped(bool extended, float seconds, float expected)
        {
            Assert.Equal(expected, ReelPolicy.HookWindow(extended, seconds), 3);
        }

        [Fact]
        public void InHookWindow_IsStrictlyLessThan()
        {
            Assert.True(ReelPolicy.InHookWindow(10.49f, 10f, 0.5f));
            Assert.False(ReelPolicy.InHookWindow(10.5f, 10f, 0.5f));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: build FAILS with `The type or namespace name 'Vec3' could not be found`.

- [ ] **Step 3: Implement**

`src/AnglersEye/Core/Model/Vec3.cs`:
```csharp
using System;

namespace AnglersEye.Core.Model
{
    /// <summary>A plain 3D vector, so the model stays free of UnityEngine.</summary>
    public struct Vec3
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vec3 operator -(Vec3 a, Vec3 b)
        {
            return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public float LengthXZ => (float)Math.Sqrt(X * X + Z * Z);
    }
}
```

`src/AnglersEye/Core/Model/TargetPicker.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace AnglersEye.Core.Model
{
    public sealed class FishSighting
    {
        /// <summary>The fish's prefab name.</summary>
        public readonly string Species;
        public readonly int Quality;
        public readonly Vec3 Position;

        public FishSighting(string species, int quality, Vec3 position)
        {
            Species = species;
            Quality = quality;
            Position = position;
        }
    }

    /// <summary>
    /// Which fish the player means (PLAN §2.2 smart bait): the one under the crosshair, else the
    /// nearest within a horizontal cone along the aim, else the species most common in range.
    /// </summary>
    public static class TargetPicker
    {
        public static FishSighting Pick(FishSighting crosshair, Vec3 origin, Vec3 aim, float coneDegrees, float range,
            IReadOnlyList<FishSighting> fish)
        {
            if (crosshair != null)
                return crosshair;
            if (fish == null || fish.Count == 0)
                return null;

            FishSighting inCone = InCone(origin, aim, coneDegrees, range, fish);
            return inCone ?? MostCommon(origin, range, fish);
        }

        private static FishSighting InCone(Vec3 origin, Vec3 aim, float coneDegrees, float range, IReadOnlyList<FishSighting> fish)
        {
            float aimLen = (float)Math.Sqrt(aim.X * aim.X + aim.Z * aim.Z);
            if (aimLen < 1e-4f)
                return null; // looking straight up or down: no horizontal direction to cone around

            double cosCone = Math.Cos(coneDegrees * Math.PI / 180.0);
            FishSighting best = null;
            float bestDist = float.MaxValue;
            foreach (FishSighting f in fish)
            {
                Vec3 d = f.Position - origin;
                float dist = d.Length;
                if (dist > range)
                    continue;
                float h = d.LengthXZ;
                double cos = h < 1e-4f ? 1.0 : (d.X * aim.X + d.Z * aim.Z) / (h * aimLen);
                if (cos >= cosCone && dist < bestDist)
                {
                    best = f;
                    bestDist = dist;
                }
            }
            return best;
        }

        private static FishSighting MostCommon(Vec3 origin, float range, IReadOnlyList<FishSighting> fish)
        {
            var count = new Dictionary<string, int>();
            var nearest = new Dictionary<string, FishSighting>();
            var nearestDist = new Dictionary<string, float>();
            foreach (FishSighting f in fish)
            {
                float dist = (f.Position - origin).Length;
                if (dist > range)
                    continue;
                int n;
                count.TryGetValue(f.Species, out n);
                count[f.Species] = n + 1;
                float nd;
                if (!nearestDist.TryGetValue(f.Species, out nd) || dist < nd)
                {
                    nearestDist[f.Species] = dist;
                    nearest[f.Species] = f;
                }
            }

            FishSighting best = null;
            int bestCount = 0;
            float bestDist = 0f;
            foreach (KeyValuePair<string, int> kv in count)
            {
                float nd = nearestDist[kv.Key];
                if (best == null || kv.Value > bestCount || (kv.Value == bestCount && nd < bestDist))
                {
                    best = nearest[kv.Key];
                    bestCount = kv.Value;
                    bestDist = nd;
                }
            }
            return best;
        }
    }
}
```

`src/AnglersEye/Core/Model/ReelPolicy.cs`:
```csharp
using System;

namespace AnglersEye.Core.Model
{
    /// <summary>The two assists' rules (PLAN §2.2 items 3 and 4).</summary>
    public static class ReelPolicy
    {
        /// <summary>FishingFloat.TryToHook's hard-coded window.</summary>
        public const float VanillaHookWindow = 0.5f;
        public const float MaxHookWindow = 1.5f;

        /// <summary>Smart reel: don't reel while a hooked fish struggles.</summary>
        public static bool SuppressReel(bool smartReel, bool hooked, bool escaping)
        {
            return smartReel && hooked && escaping;
        }

        public static float HookWindow(bool extended, float seconds)
        {
            if (!extended)
                return VanillaHookWindow;
            return Math.Min(MaxHookWindow, Math.Max(VanillaHookWindow, seconds));
        }

        public static bool InHookWindow(float now, float nibbleTime, float window)
        {
            return now - nibbleTime < window;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: `Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/Core/Model/Vec3.cs src/AnglersEye/Core/Model/TargetPicker.cs src/AnglersEye/Core/Model/ReelPolicy.cs tests/AnglersEye.Tests/TargetPickerTests.cs tests/AnglersEye.Tests/ReelPolicyTests.cs
git commit -m "Model: target picker and reel policy" && git push
```

---

### Task 6: Glyphs and Labels (model)

**Files:**
- Create: `src/AnglersEye/Core/Model/Glyphs.cs`, `src/AnglersEye/Core/Model/Labels.cs`
- Test: `tests/AnglersEye.Tests/LabelsTests.cs`

**Interfaces:**
- Consumes: `BaitAdvice`, `BaitOption` (Task 3), `Verdict` (Task 4).
- Produces:
  - `sealed class Glyphs { string Star, Yes, No, Land, Calm, Struggle, BiteLeft, BiteRight, Sep; static Glyphs Unicode(); static Glyphs Ascii(); static Glyphs Resolve(Func<char, bool> hasChar) }`
  - `static class Labels` with `Stars`, `WithStars`, `Odds`, `Bait`, `HoverSuffix`, `Target`, `OnFloat`, `Verdict`, `Waiting`, `Distance`, `Hooked`, `Bite`, `NeedsBait`. Exact signatures are in the code below.

Strings are English, as in the user's other mods. Names passed in are already localised by the caller.

- [ ] **Step 1: Write the failing tests**

`tests/AnglersEye.Tests/LabelsTests.cs`:
```csharp
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class LabelsTests
    {
        private static readonly Glyphs G = Glyphs.Unicode();
        private static readonly BaitOption Cold = new BaitOption("FishingBaitCold", "Cold bait", 0.6f);
        private static readonly BaitAdvice Carried = new BaitAdvice { Best = Cold, Carried = true, CarriedCount = 12 };
        private static readonly BaitAdvice Missing = new BaitAdvice { Best = Cold, Carried = false, CarriedCount = 0 };

        [Fact]
        public void Stars_AreQualityMinusOne()
        {
            Assert.Equal("", Labels.Stars(1, G));
            Assert.Equal("★★", Labels.Stars(3, G));
            Assert.Equal("Pike ★", Labels.WithStars("Pike", 2, G));
            Assert.Equal("Pike", Labels.WithStars("Pike", 1, G));
        }

        [Fact]
        public void Bait_CarriedAndMissing_WithOptionalOdds()
        {
            Assert.Equal("Cold bait ✔ (x12)", Labels.Bait(Carried, G, false));
            Assert.Equal("Cold bait 60% ✔ (x12)", Labels.Bait(Carried, G, true));
            Assert.Equal("Cold bait ✖", Labels.Bait(Missing, G, false));
        }

        [Fact]
        public void HoverSuffix_StarsThenBaitLine()
        {
            Assert.Equal(" ★★\nBait: Cold bait ✔ (x12)", Labels.HoverSuffix(3, Carried, G, false));
            Assert.Equal("\nBait: Cold bait ✖", Labels.HoverSuffix(1, Missing, G, false));
            Assert.Equal("", Labels.HoverSuffix(1, null, G, false));
        }

        [Fact]
        public void Target_BeforeCast()
        {
            Assert.Equal("Pike ★ · Cold bait ✔ (x12)", Labels.Target("Pike", 2, Carried, G, false));
            Assert.Equal("Pike ★ · needs Cold bait ✖", Labels.Target("Pike", 2, Missing, G, false));
            Assert.Equal("Pike", Labels.Target("Pike", 1, null, G, false));
        }

        [Fact]
        public void OnFloat_WorksOrNeeds()
        {
            Assert.Equal("Pike ★ · ✔", Labels.OnFloat("Pike", 2, true, "Cold bait", G));
            Assert.Equal("Pike · needs Cold bait ✖", Labels.OnFloat("Pike", 1, false, "Cold bait", G));
        }

        [Fact]
        public void Verdicts_ShortAndLong()
        {
            Assert.Equal("✓", Labels.Verdict(Verdict.Likely, G, true));
            Assert.Equal("✓ can land", Labels.Verdict(Verdict.Likely, G, false));
            Assert.Equal("~ tight", Labels.Verdict(Verdict.Tight, G, false));
            Assert.Equal("✖ unlikely", Labels.Verdict(Verdict.Unlikely, G, false));
        }

        [Fact]
        public void StripStates()
        {
            Assert.Equal("Pike ★ · 18m · ✓", Labels.Waiting("Pike", 2, 18, Verdict.Likely, G));
            Assert.Equal("Pike · 18m", Labels.Waiting("Pike", 1, 18, null, G));
            Assert.Equal("18m", Labels.Distance(18));
            Assert.Equal("● REEL  12m  ✓ can land", Labels.Hooked(false, 12, Verdict.Likely, G));
            Assert.Equal("▲ WAIT  12m  ~ tight", Labels.Hooked(true, 12, Verdict.Tight, G));
            Assert.Equal("▲ WAIT  12m", Labels.Hooked(true, 12, null, G));
            Assert.Equal("» BITE! «", Labels.Bite(G));
            Assert.Equal("Angler's Eye: Pike needs Cold bait", Labels.NeedsBait("Pike", "Cold bait"));
        }

        [Fact]
        public void Resolve_FallsBackPerGlyph()
        {
            Glyphs g = Glyphs.Resolve(c => c != '★' && c != '»');
            Assert.Equal("*", g.Star);
            Assert.Equal(">>", g.BiteLeft);
            Assert.Equal("«", g.BiteRight);
            Assert.Equal("✔", g.Yes);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: build FAILS with `The type or namespace name 'Glyphs' could not be found`.

- [ ] **Step 3: Implement**

`src/AnglersEye/Core/Model/Glyphs.cs`:
```csharp
using System;

namespace AnglersEye.Core.Model
{
    /// <summary>
    /// Symbols used in labels. The game's HUD font may lack some of them, so each one falls back
    /// to ASCII individually (Resolve), decided at runtime from the font itself.
    /// </summary>
    public sealed class Glyphs
    {
        public string Star, Yes, No, Land, Calm, Struggle, BiteLeft, BiteRight, Sep;

        public static Glyphs Unicode()
        {
            return new Glyphs { Star = "★", Yes = "✔", No = "✖", Land = "✓", Calm = "●", Struggle = "▲", BiteLeft = "»", BiteRight = "«", Sep = "·" };
        }

        public static Glyphs Ascii()
        {
            return new Glyphs { Star = "*", Yes = "+", No = "x", Land = "+", Calm = ">", Struggle = "!", BiteLeft = ">>", BiteRight = "<<", Sep = "-" };
        }

        public static Glyphs Resolve(Func<char, bool> hasChar)
        {
            Glyphs u = Unicode(), a = Ascii();
            return new Glyphs
            {
                Star = Pick(u.Star, a.Star, hasChar),
                Yes = Pick(u.Yes, a.Yes, hasChar),
                No = Pick(u.No, a.No, hasChar),
                Land = Pick(u.Land, a.Land, hasChar),
                Calm = Pick(u.Calm, a.Calm, hasChar),
                Struggle = Pick(u.Struggle, a.Struggle, hasChar),
                BiteLeft = Pick(u.BiteLeft, a.BiteLeft, hasChar),
                BiteRight = Pick(u.BiteRight, a.BiteRight, hasChar),
                Sep = Pick(u.Sep, a.Sep, hasChar)
            };
        }

        private static string Pick(string unicode, string ascii, Func<char, bool> hasChar)
        {
            foreach (char c in unicode)
                if (!hasChar(c))
                    return ascii;
            return unicode;
        }
    }
}
```

`src/AnglersEye/Core/Model/Labels.cs`:
```csharp
using System;
using System.Text;

namespace AnglersEye.Core.Model
{
    /// <summary>Every string Angler's Eye shows. Names passed in are already localised.</summary>
    public static class Labels
    {
        public static string Stars(int quality, Glyphs g)
        {
            if (quality <= 1)
                return "";
            var sb = new StringBuilder();
            for (int i = 1; i < quality; i++)
                sb.Append(g.Star);
            return sb.ToString();
        }

        public static string WithStars(string name, int quality, Glyphs g)
        {
            string s = Stars(quality, g);
            return s.Length == 0 ? name : name + " " + s;
        }

        public static string Odds(float chance)
        {
            return (int)Math.Round(chance * 100f) + "%";
        }

        /// <summary>"Cold bait ✔ (x12)", "Cold bait 60% ✔ (x12)" or "Cold bait ✖".</summary>
        public static string Bait(BaitAdvice a, Glyphs g, bool showOdds)
        {
            string s = a.Best.BaitName + (showOdds ? " " + Odds(a.Best.Chance) : "");
            return a.Carried ? s + " " + g.Yes + " (x" + a.CarriedCount + ")" : s + " " + g.No;
        }

        /// <summary>Appended to the vanilla hover name: stars, then a bait line.</summary>
        public static string HoverSuffix(int quality, BaitAdvice a, Glyphs g, bool showOdds)
        {
            string s = quality > 1 ? " " + Stars(quality, g) : "";
            if (a != null)
                s += "\nBait: " + Bait(a, g, showOdds);
            return s;
        }

        /// <summary>Strip line before a cast: the fish smart bait would target.</summary>
        public static string Target(string name, int quality, BaitAdvice a, Glyphs g, bool showOdds)
        {
            string head = WithStars(name, quality, g);
            if (a == null)
                return head;
            return head + " " + g.Sep + " " + (a.Carried ? "" : "needs ") + Bait(a, g, showOdds);
        }

        /// <summary>Float label: whether the bait on the float works on this fish.</summary>
        public static string OnFloat(string name, int quality, bool baitWorks, string neededBait, Glyphs g)
        {
            string head = WithStars(name, quality, g) + " " + g.Sep + " ";
            return baitWorks ? head + g.Yes : head + "needs " + neededBait + " " + g.No;
        }

        public static string Verdict(Verdict v, Glyphs g, bool shortForm)
        {
            switch (v)
            {
                case Model.Verdict.Likely:
                    return shortForm ? g.Land : g.Land + " can land";
                case Model.Verdict.Tight:
                    return shortForm ? "~" : "~ tight";
                default:
                    return shortForm ? g.No : g.No + " unlikely";
            }
        }

        /// <summary>Strip line while the float is out: the fish near it, line length, forecast.</summary>
        public static string Waiting(string name, int quality, int metres, Verdict? v, Glyphs g)
        {
            string s = WithStars(name, quality, g) + " " + g.Sep + " " + Distance(metres);
            return v.HasValue ? s + " " + g.Sep + " " + Verdict(v.Value, g, true) : s;
        }

        public static string Distance(int metres)
        {
            return metres + "m";
        }

        /// <summary>Strip line while hooked.</summary>
        public static string Hooked(bool escaping, int metres, Verdict? v, Glyphs g)
        {
            string s = (escaping ? g.Struggle + " WAIT" : g.Calm + " REEL") + "  " + Distance(metres);
            return v.HasValue ? s + "  " + Verdict(v.Value, g, false) : s;
        }

        public static string Bite(Glyphs g)
        {
            return g.BiteLeft + " BITE! " + g.BiteRight;
        }

        public static string NeedsBait(string fish, string bait)
        {
            return "Angler's Eye: " + fish + " needs " + bait;
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: `Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/Core/Model/Glyphs.cs src/AnglersEye/Core/Model/Labels.cs tests/AnglersEye.Tests/LabelsTests.cs
git commit -m "Model: labels and glyph fallback" && git push
```

---

### Task 7: CompatRules (model)

**Files:**
- Create: `src/AnglersEye/Core/Model/CompatRules.cs`
- Test: `tests/AnglersEye.Tests/CompatRulesTests.cs`

**Interfaces:**
- Produces:
  - `[Flags] enum Feature { None = 0, HoverInfo = 1, FloatLabel = 2, SmartBait = 4, BiteCue = 8, StruggleIndicator = 16, Forecast = 32, SmartReel = 64, HookWindow = 128 }`
  - `sealed class CompatVerdict { Feature Disabled; readonly List<string> Reasons; bool IsOff(Feature f) }`
  - `static class CompatRules { const string Hooked = "Azumatt.Hooked", TrollingFishing = "sighsorry.TrollingFishing", FixedUpdate = "FishingFloat.FixedUpdate", TryToHook = "FishingFloat.TryToHook", GetStaminaUse = "Fish.GetStaminaUse"; static CompatVerdict Evaluate(ICollection<string> pluginGuids, IDictionary<string, IList<string>> foreignPatchOwners); static string Describe(Feature f) }`

The rules are PLAN §2.6.

- [ ] **Step 1: Write the failing tests**

`tests/AnglersEye.Tests/CompatRulesTests.cs`:
```csharp
using System.Collections.Generic;
using AnglersEye.Core.Model;
using Xunit;

namespace AnglersEye.Tests
{
    public class CompatRulesTests
    {
        private static CompatVerdict Eval(string[] guids, Dictionary<string, IList<string>> owners = null)
        {
            return CompatRules.Evaluate(new HashSet<string>(guids), owners ?? new Dictionary<string, IList<string>>());
        }

        [Fact]
        public void NothingInstalled_NothingOff()
        {
            CompatVerdict v = Eval(new[] { "com.jumpingmushroom.larder" });
            Assert.Equal(Feature.None, v.Disabled);
            Assert.Empty(v.Reasons);
        }

        [Fact]
        public void Hooked_TurnsOffAssistsForecastAndCues_KeepsId()
        {
            CompatVerdict v = Eval(new[] { CompatRules.Hooked });
            Assert.True(v.IsOff(Feature.SmartBait));
            Assert.True(v.IsOff(Feature.SmartReel));
            Assert.True(v.IsOff(Feature.HookWindow));
            Assert.True(v.IsOff(Feature.Forecast));
            Assert.True(v.IsOff(Feature.BiteCue));
            Assert.True(v.IsOff(Feature.StruggleIndicator));
            Assert.False(v.IsOff(Feature.HoverInfo));
            Assert.False(v.IsOff(Feature.FloatLabel));
        }

        [Fact]
        public void TrollingFishing_KeepsCues()
        {
            CompatVerdict v = Eval(new[] { CompatRules.TrollingFishing });
            Assert.True(v.IsOff(Feature.SmartBait));
            Assert.True(v.IsOff(Feature.Forecast));
            Assert.False(v.IsOff(Feature.BiteCue));
            Assert.False(v.IsOff(Feature.StruggleIndicator));
        }

        [Fact]
        public void ForeignPatches_TurnOffOnlyTheMatchingFeature()
        {
            var owners = new Dictionary<string, IList<string>>
            {
                { CompatRules.FixedUpdate, new List<string> { "games.loxley.comfyfishing" } },
                { CompatRules.TryToHook, new List<string>() },
                { CompatRules.GetStaminaUse, new List<string> { "com.orianaventure.mod.ReelyGoodRod" } }
            };
            CompatVerdict v = Eval(new string[0], owners);
            Assert.Equal(Feature.SmartReel | Feature.Forecast, v.Disabled);
            Assert.Contains(v.Reasons, r => r.Contains("games.loxley.comfyfishing") && r.Contains("smart reel"));
            Assert.Contains(v.Reasons, r => r.Contains("ReelyGoodRod") && r.Contains("forecast"));
        }

        [Fact]
        public void Describe_ListsFlagsInOrder()
        {
            Assert.Equal("smart bait, forecast, extended hook window",
                CompatRules.Describe(Feature.HookWindow | Feature.SmartBait | Feature.Forecast));
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: build FAILS with `The type or namespace name 'CompatVerdict' could not be found`.

- [ ] **Step 3: Implement**

`src/AnglersEye/Core/Model/CompatRules.cs`:
```csharp
using System;
using System.Collections.Generic;

namespace AnglersEye.Core.Model
{
    [Flags]
    public enum Feature
    {
        None = 0,
        HoverInfo = 1,
        FloatLabel = 2,
        SmartBait = 4,
        BiteCue = 8,
        StruggleIndicator = 16,
        Forecast = 32,
        SmartReel = 64,
        HookWindow = 128
    }

    public sealed class CompatVerdict
    {
        public Feature Disabled;
        public readonly List<string> Reasons = new List<string>();

        public bool IsOff(Feature f)
        {
            return (Disabled & f) != 0;
        }
    }

    /// <summary>Which features step aside for other fishing mods (PLAN §2.6).</summary>
    public static class CompatRules
    {
        public const string Hooked = "Azumatt.Hooked";
        public const string TrollingFishing = "sighsorry.TrollingFishing";

        public const string FixedUpdate = "FishingFloat.FixedUpdate";
        public const string TryToHook = "FishingFloat.TryToHook";
        public const string GetStaminaUse = "Fish.GetStaminaUse";

        private static readonly KeyValuePair<Feature, string>[] Names =
        {
            new KeyValuePair<Feature, string>(Feature.HoverInfo, "hover info"),
            new KeyValuePair<Feature, string>(Feature.FloatLabel, "float label"),
            new KeyValuePair<Feature, string>(Feature.SmartBait, "smart bait"),
            new KeyValuePair<Feature, string>(Feature.BiteCue, "bite cue"),
            new KeyValuePair<Feature, string>(Feature.StruggleIndicator, "struggle indicator"),
            new KeyValuePair<Feature, string>(Feature.Forecast, "forecast"),
            new KeyValuePair<Feature, string>(Feature.SmartReel, "smart reel"),
            new KeyValuePair<Feature, string>(Feature.HookWindow, "extended hook window")
        };

        public static CompatVerdict Evaluate(ICollection<string> pluginGuids, IDictionary<string, IList<string>> foreignPatchOwners)
        {
            var v = new CompatVerdict();
            if (pluginGuids.Contains(Hooked))
                Off(v, Feature.SmartBait | Feature.SmartReel | Feature.HookWindow | Feature.Forecast |
                       Feature.BiteCue | Feature.StruggleIndicator, "Hooked (" + Hooked + ")");
            if (pluginGuids.Contains(TrollingFishing))
                Off(v, Feature.SmartBait | Feature.SmartReel | Feature.HookWindow | Feature.Forecast,
                    "Trolling Fishing (" + TrollingFishing + ")");

            Patched(v, foreignPatchOwners, FixedUpdate, Feature.SmartReel);
            Patched(v, foreignPatchOwners, TryToHook, Feature.HookWindow);
            Patched(v, foreignPatchOwners, GetStaminaUse, Feature.Forecast);
            return v;
        }

        public static string Describe(Feature f)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<Feature, string> kv in Names)
                if ((f & kv.Key) != 0)
                    parts.Add(kv.Value);
            return string.Join(", ", parts);
        }

        private static void Patched(CompatVerdict v, IDictionary<string, IList<string>> owners, string method, Feature f)
        {
            IList<string> o;
            if (owners != null && owners.TryGetValue(method, out o) && o != null && o.Count > 0)
                Off(v, f, string.Join(", ", o) + " patches " + method);
        }

        private static void Off(CompatVerdict v, Feature f, string who)
        {
            v.Disabled |= f;
            v.Reasons.Add(who + ": " + Describe(f) + " off");
        }
    }
}
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`
Expected: `Failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/Core/Model/CompatRules.cs tests/AnglersEye.Tests/CompatRulesTests.cs
git commit -m "Model: compat rules" && git push
```

---

### Task 8: Fish catalogue, tackle and console command

**Files:**
- Create: `src/AnglersEye/Core/FishCatalog.cs`, `src/AnglersEye/Core/Tackle.cs`, `src/AnglersEye/Core/ConsoleCommands.cs`, `src/AnglersEye/Core/Runtime.cs`
- Modify: `src/AnglersEye/Plugin.cs` (register console command, `Update` → `Runtime.Tick`)

**Interfaces:**
- Consumes: `BaitOption`, `BaitAdvice`, `BaitAdvisor` (Task 3); `RodParams`, `FishParams` (Task 4).
- Produces:
  - `sealed class FishInfo { string Prefab; string NameToken; List<BaitOption> Baits; FishParams Params; string Name /* localised */ }`
  - `static class FishCatalog { static IReadOnlyDictionary<string, FishInfo> All; static RodParams Rod; static float FloatRange; static float MaxDistance; static void EnsureBuilt(); static FishInfo For(Fish f); static int Quality(Fish f) }`
  - `static class Tackle { static bool IsRod(ItemDrop.ItemData w); static Dictionary<string, int> Carried(Player p); static BaitAdvice Advise(Player p, FishInfo info) }`
  - `static class ConsoleCommands { static void Register(); static void Say(Terminal ctx, string line); }`
  - `static class Runtime { static void Tick(); }` (extended in later tasks)

- [ ] **Step 1: `src/AnglersEye/Core/FishCatalog.cs`**

```csharp
using System;
using System.Collections.Generic;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    internal sealed class FishInfo
    {
        public string Prefab;
        public string NameToken;
        public List<BaitOption> Baits;
        public FishParams Params;

        public string Name => Localization.instance != null ? Localization.instance.Localize(NameToken) : NameToken;
    }

    /// <summary>
    /// Every fish prefab's bait table and fight numbers, plus the float's reeling numbers, read
    /// from ZNetScene at runtime (PLAN §1.1, §1.5) so modded fish work. Rebuilt per world.
    /// </summary>
    internal static class FishCatalog
    {
        private static readonly Dictionary<string, FishInfo> ByPrefab = new Dictionary<string, FishInfo>();
        private static ZNetScene _builtFor;

        public static RodParams Rod = new RodParams();
        public static float FloatRange = 10f;
        public static float MaxDistance = 30f;

        public static IReadOnlyDictionary<string, FishInfo> All
        {
            get
            {
                EnsureBuilt();
                return ByPrefab;
            }
        }

        public static void EnsureBuilt()
        {
            ZNetScene zs = ZNetScene.instance;
            if (zs == null || zs == _builtFor)
                return;
            ByPrefab.Clear();
            foreach (GameObject go in zs.m_prefabs)
            {
                if (go == null)
                    continue;
                Fish f = go.GetComponent<Fish>();
                if (f != null)
                    ByPrefab[go.name] = Build(go.name, f);
                FishingFloat ff = go.GetComponent<FishingFloat>();
                if (ff != null)
                    ReadFloat(ff);
            }
            _builtFor = zs;
        }

        public static FishInfo For(Fish f)
        {
            EnsureBuilt();
            string key = Utils.GetPrefabName(f.gameObject);
            FishInfo info;
            if (!ByPrefab.TryGetValue(key, out info))
            {
                info = Build(key, f);
                ByPrefab[key] = info;
            }
            return info;
        }

        /// <summary>The fish's level (1 = no stars), from its ItemDrop (PLAN §1.1).</summary>
        public static int Quality(Fish f)
        {
            ItemDrop d = f.m_itemDrop != null ? f.m_itemDrop : f.GetComponent<ItemDrop>();
            return d != null ? Math.Max(1, d.m_itemData.m_quality) : 1;
        }

        private static FishInfo Build(string prefab, Fish f)
        {
            var baits = new List<BaitOption>();
            foreach (Fish.BaitSetting b in f.m_baits)
            {
                if (b == null || b.m_bait == null)
                    continue;
                string token = b.m_bait.m_itemData.m_shared.m_name;
                string name = Localization.instance != null ? Localization.instance.Localize(token) : token;
                baits.Add(new BaitOption(b.m_bait.name, name, b.m_chance));
            }
            return new FishInfo
            {
                Prefab = prefab,
                NameToken = f.m_name,
                Baits = baits,
                Params = new FishParams
                {
                    StaminaUse = f.m_staminaUse,
                    EscapeStaminaUse = f.m_escapeStaminaUse,
                    EscapeMin = f.m_escapeMin,
                    EscapeMax = f.m_escapeMax,
                    EscapeMaxPerLevel = f.m_escapeMaxPerLevel,
                    EscapeWaitMin = f.m_escapeWaitMin,
                    EscapeWaitMax = f.m_escapeWaitMax
                }
            };
        }

        private static void ReadFloat(FishingFloat ff)
        {
            Rod = new RodParams
            {
                PullStaminaUse = ff.m_pullStaminaUse,
                PullStaminaUseMaxSkillMultiplier = ff.m_pullStaminaUseMaxSkillMultiplier,
                PullLineSpeed = ff.m_pullLineSpeed,
                PullLineSpeedMaxSkill = ff.m_pullLineSpeedMaxSkill,
                HookedStaminaPerSec = ff.m_hookedStaminaPerSec,
                HookedStaminaPerSecMaxSkill = ff.m_hookedStaminaPerSecMaxSkill
            };
            FloatRange = ff.m_range;
            MaxDistance = ff.m_maxDistance;
        }
    }
}
```

- [ ] **Step 2: `src/AnglersEye/Core/Tackle.cs`**

```csharp
using System.Collections.Generic;
using AnglersEye.Core.Model;

namespace AnglersEye.Core
{
    internal static class Tackle
    {
        /// <summary>A fishing rod is a weapon whose projectile is a FishingFloat. No names hardcoded.</summary>
        public static bool IsRod(ItemDrop.ItemData w)
        {
            if (w == null || string.IsNullOrEmpty(w.m_shared.m_ammoType))
                return false;
            Attack a = w.m_shared.m_attack;
            return a != null && a.m_attackProjectile != null && a.m_attackProjectile.GetComponent<FishingFloat>() != null;
        }

        /// <summary>Carried stack totals by prefab name (the key fish bait tables use).</summary>
        public static Dictionary<string, int> Carried(Player p)
        {
            var d = new Dictionary<string, int>();
            foreach (ItemDrop.ItemData item in p.GetInventory().GetAllItems())
            {
                if (item.m_dropPrefab == null)
                    continue;
                int n;
                d.TryGetValue(item.m_dropPrefab.name, out n);
                d[item.m_dropPrefab.name] = n + item.m_stack;
            }
            return d;
        }

        public static BaitAdvice Advise(Player p, FishInfo info)
        {
            return info == null ? null : BaitAdvisor.Advise(info.Baits, Carried(p));
        }
    }
}
```

- [ ] **Step 3: `src/AnglersEye/Core/Runtime.cs`** (first version, extended in Tasks 9 and 12)

```csharp
using System;

namespace AnglersEye.Core
{
    /// <summary>Per-frame work, driven from the plugin's Update.</summary>
    internal static class Runtime
    {
        public static void Tick()
        {
            try
            {
                FishCatalog.EnsureBuilt();
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("Runtime.Tick", e);
            }
        }
    }
}
```

- [ ] **Step 4: `src/AnglersEye/Core/ConsoleCommands.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using AnglersEye.Core.Model;

namespace AnglersEye.Core
{
    /// <summary>
    /// "anglerseye" prints status; "anglerseye fish" every fish's bait table and fight numbers.
    /// Output is mirrored to the BepInEx log for build/logs.sh.
    /// </summary>
    internal static class ConsoleCommands
    {
        public static void Register()
        {
            new Terminal.ConsoleCommand("anglerseye", "Angler's Eye: feature and compat status (fish = bait table for every fish)",
                delegate (Terminal.ConsoleEventArgs args)
                {
                    string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "";
                    if (sub == "fish")
                        Fish(args.Context);
                    else
                        Status(args.Context);
                });
        }

        internal static void Say(Terminal ctx, string line)
        {
            if (ctx != null)
                ctx.AddString(line);
            AnglersEyePlugin.Log.LogInfo(line);
        }

        private static void Status(Terminal ctx)
        {
            Say(ctx, "Angler's Eye " + AnglersEyePlugin.PluginVersion + ": " + FishCatalog.All.Count + " fish known.");
        }

        private static void Fish(Terminal ctx)
        {
            try
            {
                RodParams r = FishCatalog.Rod;
                Say(ctx, "Angler's Eye:   float: reel " + r.PullStaminaUse + "/s x" + r.PullStaminaUseMaxSkillMultiplier +
                         " at max skill, speed " + r.PullLineSpeed + "-" + r.PullLineSpeedMaxSkill + " m/s, hooked drain " +
                         r.HookedStaminaPerSec + "-" + r.HookedStaminaPerSecMaxSkill + "/s, range " + FishCatalog.FloatRange + "m");
                foreach (FishInfo f in FishCatalog.All.Values.OrderBy(f => f.Prefab, StringComparer.Ordinal))
                {
                    FishParams p = f.Params;
                    string baits = f.Baits.Count == 0 ? "no bait" :
                        string.Join(", ", f.Baits.Select(b => b.BaitName + " " + Labels.Odds(b.Chance)));
                    Say(ctx, "Angler's Eye:   " + f.Prefab + " " + f.Name + ": " + baits + " | stamina " + p.StaminaUse + "/" +
                             p.EscapeStaminaUse + ", struggle " + p.EscapeMin + "-" + p.EscapeMax + " +" + p.EscapeMaxPerLevel +
                             "/level, pause " + p.EscapeWaitMin + "-" + p.EscapeWaitMax + "s");
                }
            }
            catch (Exception e)
            {
                Say(ctx, "Angler's Eye: couldn't list fish: " + e.Message);
                AnglersEyePlugin.WarnOnce("anglerseye fish", e);
            }
        }
    }
}
```

- [ ] **Step 5: Wire into `Plugin.cs`**

In `Awake`, after `PluginConfig.Bind(base.Config);`:
```csharp
            Core.ConsoleCommands.Register();
```
Add to the class:
```csharp
        private void Update()
        {
            Core.Runtime.Tick();
        }
```

- [ ] **Step 6: Build, deploy, check in game**

Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal && ./build/deploy.sh`
Expected: tests pass; deploy succeeds.
Ask the user to load a world and run `anglerseye fish` in the console. Then `./build/logs.sh`.
Expected: one float line and one line per fish, matching the values recorded in PLAN §1.7.4, with localised names (e.g. "Perch", "Fishing bait").

- [ ] **Step 7: Commit**

```bash
git add src/AnglersEye/Core/FishCatalog.cs src/AnglersEye/Core/Tackle.cs src/AnglersEye/Core/ConsoleCommands.cs src/AnglersEye/Core/Runtime.cs src/AnglersEye/Plugin.cs
git commit -m "Fish catalogue, tackle helpers, anglerseye console command" && git push
```

---

### Task 9: Compat detection and feature gate

**Files:**
- Create: `src/AnglersEye/Core/Compat.cs`, `src/AnglersEye/Core/Features.cs`
- Modify: `src/AnglersEye/PluginConfig.cs` (add `Toggle`), `src/AnglersEye/Core/Runtime.cs`, `src/AnglersEye/Core/ConsoleCommands.cs` (`Status`)

**Interfaces:**
- Consumes: `Feature`, `CompatVerdict`, `CompatRules` (Task 7).
- Produces: `static bool Features.On(Feature f)`; `static class Compat { static CompatVerdict Verdict; static bool IsOff(Feature f); static void EnsureEvaluated(); }`; `static bool PluginConfig.Toggle(Feature f)`.

- [ ] **Step 1: `PluginConfig.Toggle`**

Add `using AnglersEye.Core.Model;` at the top of `PluginConfig.cs`, and to the class:
```csharp
        /// <summary>The config switch for one feature.</summary>
        public static bool Toggle(Feature f)
        {
            switch (f)
            {
                case Feature.HoverInfo: return HoverInfo.Value;
                case Feature.FloatLabel: return FloatLabel.Value;
                case Feature.SmartBait: return SmartBait.Value;
                case Feature.BiteCue: return BiteCue.Value;
                case Feature.StruggleIndicator: return StruggleIndicator.Value;
                case Feature.Forecast: return Forecast.Value;
                case Feature.SmartReel: return SmartReel.Value;
                case Feature.HookWindow: return ExtendedHookWindow.Value;
                default: return false;
            }
        }
```

- [ ] **Step 2: `src/AnglersEye/Core/Compat.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using AnglersEye.Core.Model;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace AnglersEye.Core
{
    /// <summary>
    /// Detects other fishing mods once all plugins have loaded (the first frame a world exists)
    /// and applies CompatRules. Each decision is logged once.
    /// </summary>
    internal static class Compat
    {
        public static CompatVerdict Verdict { get; private set; }

        public static bool IsOff(Feature f)
        {
            return Verdict != null && Verdict.IsOff(f);
        }

        public static void EnsureEvaluated()
        {
            if (Verdict != null || ZNetScene.instance == null)
                return;
            var guids = new HashSet<string>(Chainloader.PluginInfos.Keys);
            var owners = new Dictionary<string, IList<string>>
            {
                { CompatRules.FixedUpdate, Owners(typeof(FishingFloat), "FixedUpdate") },
                { CompatRules.TryToHook, Owners(typeof(FishingFloat), "TryToHook") },
                { CompatRules.GetStaminaUse, Owners(typeof(Fish), "GetStaminaUse") }
            };
            Verdict = CompatRules.Evaluate(guids, owners);
            foreach (string r in Verdict.Reasons)
                AnglersEyePlugin.Log.LogInfo("Angler's Eye compat: " + r);
        }

        private static IList<string> Owners(Type type, string method)
        {
            var list = new List<string>();
            MethodBase m = AccessTools.Method(type, method);
            // Fully qualified: our own AnglersEye.Patches namespace would shadow HarmonyLib.Patches.
            HarmonyLib.Patches p = m != null ? Harmony.GetPatchInfo(m) : null;
            if (p == null)
                return list;
            foreach (string o in p.Owners)
                if (o != AnglersEyePlugin.PluginGuid && !list.Contains(o))
                    list.Add(o);
            return list;
        }
    }
}
```

- [ ] **Step 3: `src/AnglersEye/Core/Features.cs`**

```csharp
using AnglersEye.Core.Model;

namespace AnglersEye.Core
{
    internal static class Features
    {
        /// <summary>Master switch, the feature's own toggle, and not stood down for another mod.</summary>
        public static bool On(Feature f)
        {
            return PluginConfig.Enabled.Value && PluginConfig.Toggle(f) && !Compat.IsOff(f);
        }
    }
}
```

- [ ] **Step 4: Evaluate from `Runtime.Tick` and report from `Status`**

In `Runtime.Tick`, after `FishCatalog.EnsureBuilt();`:
```csharp
                Compat.EnsureEvaluated();
```

Replace `ConsoleCommands.Status` with:
```csharp
        private static void Status(Terminal ctx)
        {
            Say(ctx, "Angler's Eye " + AnglersEyePlugin.PluginVersion + (PluginConfig.Enabled.Value ? "" : " (disabled)") +
                     ": " + FishCatalog.All.Count + " fish known.");
            var on = new List<string>();
            var off = new List<string>();
            foreach (Feature f in Enum.GetValues(typeof(Feature)))
            {
                if (f == Feature.None)
                    continue;
                (Features.On(f) ? on : off).Add(CompatRules.Describe(f));
            }
            Say(ctx, "Angler's Eye:   on: " + (on.Count > 0 ? string.Join(", ", on) : "nothing"));
            Say(ctx, "Angler's Eye:   off: " + (off.Count > 0 ? string.Join(", ", off) : "nothing"));
            if (Compat.Verdict == null)
                Say(ctx, "Angler's Eye:   compat: not checked yet (load a world)");
            else if (Compat.Verdict.Reasons.Count == 0)
                Say(ctx, "Angler's Eye:   compat: no other fishing mods found");
            else
                foreach (string r in Compat.Verdict.Reasons)
                    Say(ctx, "Angler's Eye:   compat: " + r);
        }
```

- [ ] **Step 5: Verify the patch scan with a dummy patch (temporary, not committed)**

Add a temporary file `src/AnglersEye/Core/CompatSelfTest.cs`:
```csharp
using HarmonyLib;

namespace AnglersEye.Core
{
    // THROWAWAY: proves Compat sees foreign patches. Delete before committing.
    internal static class CompatSelfTest
    {
        public static void Apply()
        {
            new Harmony("test.anglerseye.dummy").Patch(AccessTools.Method(typeof(Fish), "GetStaminaUse"),
                postfix: new HarmonyMethod(typeof(CompatSelfTest), nameof(Noop)));
        }

        private static void Noop() { }
    }
}
```
and in `Plugin.Awake`, temporarily: `Core.CompatSelfTest.Apply(); // THROWAWAY`.
Deploy, have the user load a world and run `anglerseye`.
Expected: `compat: test.anglerseye.dummy patches Fish.GetStaminaUse: forecast off`, with `forecast` listed under off. Also check that `smart reel, extended hook window` appear under **off** (config defaults) and everything else under **on**.
Then remove it:
```bash
rm src/AnglersEye/Core/CompatSelfTest.cs
sed -i '/CompatSelfTest.Apply(); \/\/ THROWAWAY/d' src/AnglersEye/Plugin.cs
```

- [ ] **Step 6: Build and commit**

```bash
dotnet build src/AnglersEye/AnglersEye.csproj -c Release --nologo -v minimal
git add src/AnglersEye/Core/Compat.cs src/AnglersEye/Core/Features.cs src/AnglersEye/PluginConfig.cs src/AnglersEye/Core/Runtime.cs src/AnglersEye/Core/ConsoleCommands.cs
git status --short   # CompatSelfTest.cs must not be listed
git commit -m "Compat detection and per-feature gate" && git push
```

---

### Task 10: Hover info

**Files:**
- Create: `src/AnglersEye/UI/UiUtil.cs`, `src/AnglersEye/Patches/HoverPatch.cs`

**Interfaces:**
- Consumes: `Labels`, `Glyphs` (Task 6); `FishCatalog`, `Tackle` (Task 8); `Features` (Task 9).
- Produces: `static class UiUtil { static Sprite White; static RectTransform Rect(string name, Transform parent); static TMP_FontAsset Font; static TextMeshProUGUI Text(Transform parent, string name, float size, TextAlignmentOptions align); static Glyphs Glyphs; }`

- [ ] **Step 1: `src/AnglersEye/UI/UiUtil.cs`**

```csharp
using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;

namespace AnglersEye.UI
{
    internal static class UiUtil
    {
        private static Sprite _white;
        private static Glyphs _glyphs;
        private static TMP_FontAsset _glyphFont;

        /// <summary>A plain white sprite for backgrounds.</summary>
        public static Sprite White
        {
            get
            {
                if (_white != null)
                    return _white;
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++)
                    px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px);
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                _white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                _white.hideFlags = HideFlags.HideAndDontSave;
                return _white;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>The HUD hover text's font, so our text matches the crosshair's.</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                Hud hud = Hud.instance;
                return hud != null && hud.m_hoverName != null ? hud.m_hoverName.font : null;
            }
        }

        /// <summary>Label glyphs the HUD font can render, ASCII for the rest (PLAN §1.7.6).</summary>
        public static Glyphs Glyphs
        {
            get
            {
                TMP_FontAsset f = Font;
                if (_glyphs == null || f != _glyphFont)
                {
                    _glyphFont = f;
                    _glyphs = f == null ? Glyphs.Ascii() : Glyphs.Resolve(c => f.HasCharacter(c));
                }
                return _glyphs;
            }
        }

        public static TextMeshProUGUI Text(Transform parent, string name, float size, TextAlignmentOptions align)
        {
            RectTransform rt = Rect(name, parent);
            // Added while inactive so TMP's Awake runs after the font is set; otherwise it looks
            // for its default LiberationSans (not shipped with the game) and logs a warning.
            rt.gameObject.SetActive(false);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Font;
            if (font != null)
                t.font = font;
            rt.gameObject.SetActive(true);
            t.fontSize = size;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.richText = true;
            t.raycastTarget = false;
            t.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            return t;
        }
    }
}
```

- [ ] **Step 2: `src/AnglersEye/Patches/HoverPatch.cs`**

```csharp
using System;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using AnglersEye.UI;
using HarmonyLib;

namespace AnglersEye.Patches
{
    /// <summary>Stars and bait advice under a fish's hover name (PLAN §2.2 item 1).</summary>
    [HarmonyPatch(typeof(Fish), nameof(Fish.GetHoverText))]
    internal static class HoverPatch
    {
        private static void Postfix(Fish __instance, ref string __result)
        {
            try
            {
                // Out of the water the game shows the ItemDrop's own hover text (with stars); leave it.
                if (!Features.On(Feature.HoverInfo) || __instance.IsOutOfWater() || Player.m_localPlayer == null)
                    return;
                FishInfo info = FishCatalog.For(__instance);
                BaitAdvice advice = Tackle.Advise(Player.m_localPlayer, info);
                __result += Labels.HoverSuffix(FishCatalog.Quality(__instance), advice, UiUtil.Glyphs, PluginConfig.ShowOdds.Value);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("HoverPatch", e);
            }
        }
    }
}
```

- [ ] **Step 3: Build, deploy, check in game**

Run: `./build/deploy.sh`
Ask the user to look at fish in the water with and without the right bait, and with `ShowOdds` toggled in ConfigurationManager (F1).
Expected: `Perch` becomes e.g. `Perch ★` plus a line `Bait: Fishing bait ✔ (x23)`, or `... ✖` without it; `60%` appears with ShowOdds. A fish on land shows the vanilla item text unchanged. If PLAN §1.7.2 found that hover doesn't reach fish under water, note it in PLAN §1.7.2 and go on (the float label and strip still cover underwater fish).

- [ ] **Step 4: Commit**

```bash
git add src/AnglersEye/UI/UiUtil.cs src/AnglersEye/Patches/HoverPatch.cs
git commit -m "Hover info: stars and bait on a fish's hover text" && git push
```

---

### Task 11: Aim targeting and smart bait

**Files:**
- Create: `src/AnglersEye/Core/Targeting.cs`, `src/AnglersEye/Core/SmartBait.cs`, `src/AnglersEye/Patches/CastPatches.cs`

**Interfaces:**
- Consumes: `TargetPicker`, `FishSighting`, `Vec3` (Task 5); `Labels` (Task 6); `FishCatalog`, `Tackle` (Task 8); `Features` (Task 9).
- Produces: `static FishSighting Targeting.Aim(Player p)`; `static FishSighting Targeting.Sight(Fish f)`; `static void SmartBait.BeforeCast(Humanoid character, ItemDrop.ItemData weapon)`

Precondition: PLAN §1.7.1 confirmed that `EquipItem` on bait works and is used by the cast. If it didn't, this task is blocked on the user's sign-off (Task 2 gate).

- [ ] **Step 1: `src/AnglersEye/Core/Targeting.cs`**

```csharp
using System.Collections.Generic;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>The fish the player is aiming at, for smart bait and the pre-cast strip line.</summary>
    internal static class Targeting
    {
        private static readonly List<FishSighting> Buffer = new List<FishSighting>();

        public static FishSighting Aim(Player p)
        {
            GameCamera cam = GameCamera.instance;
            if (p == null || cam == null)
                return null;

            FishSighting cross = null;
            GameObject hover = p.GetHoverObject();
            Fish hovered = hover != null ? hover.GetComponentInParent<Fish>() : null;
            if (hovered != null && !hovered.IsOutOfWater())
                cross = Sight(hovered);

            Buffer.Clear();
            foreach (IMonoUpdater u in Fish.Instances)
            {
                Fish f = u as Fish;
                if (f == null || f.IsOutOfWater() || f.IsHooked())
                    continue;
                Buffer.Add(Sight(f));
            }
            Transform t = cam.transform;
            return TargetPicker.Pick(cross, V(t.position), V(t.forward), PluginConfig.AimConeDegrees.Value,
                FishCatalog.MaxDistance, Buffer);
        }

        public static FishSighting Sight(Fish f)
        {
            return new FishSighting(Utils.GetPrefabName(f.gameObject), FishCatalog.Quality(f), V(f.transform.position));
        }

        private static Vec3 V(Vector3 v)
        {
            return new Vec3(v.x, v.y, v.z);
        }
    }
}
```

- [ ] **Step 2: `src/AnglersEye/Core/SmartBait.cs`**

```csharp
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>
    /// Before the rod resolves its ammo (Attack.StartDraw / Attack.Start both call HaveAmmo then
    /// EquipAmmoItem, PLAN §1.6), equip the carried bait that works best on the aimed fish. Only a
    /// normal equip of an item you own; bait rules are untouched.
    /// </summary>
    internal static class SmartBait
    {
        private static float _lastMessage = -10f;

        public static void BeforeCast(Humanoid character, ItemDrop.ItemData weapon)
        {
            Player p = character as Player;
            if (p == null || p != Player.m_localPlayer || !Features.On(Feature.SmartBait) || !Tackle.IsRod(weapon))
                return;

            FishSighting target = Targeting.Aim(p);
            FishInfo info;
            if (target == null || !FishCatalog.All.TryGetValue(target.Species, out info))
                return;
            BaitAdvice advice = Tackle.Advise(p, info);
            if (advice == null)
                return;

            if (!advice.Carried)
            {
                // StartDraw and Start both run for one cast; say it once.
                if (Time.time - _lastMessage > 2f)
                {
                    _lastMessage = Time.time;
                    p.Message(MessageHud.MessageType.Center, Labels.NeedsBait(info.Name, advice.Best.BaitName));
                }
                return;
            }

            string type = weapon.m_shared.m_ammoType;
            ItemDrop.ItemData current = VanillaChoice(p, type);
            if (current != null && current.m_dropPrefab != null && current.m_dropPrefab.name == advice.Best.BaitId)
                return;
            ItemDrop.ItemData pick = p.GetInventory().GetAmmoItem(type, advice.Best.BaitId);
            if (pick == null)
                return;
            bool ok = p.EquipItem(pick, false);
            if (PluginConfig.Verbose.Value)
                AnglersEyePlugin.Log.LogInfo("Angler's Eye: smart bait " + info.Prefab + " -> " + advice.Best.BaitId + (ok ? "" : " (equip refused)"));
        }

        /// <summary>What Attack.FindAmmo would use: the equipped ammo if valid, else the lowest grid slot.</summary>
        private static ItemDrop.ItemData VanillaChoice(Player p, string type)
        {
            ItemDrop.ItemData eq = p.GetAmmoItem();
            if (eq != null && p.GetInventory().ContainsItem(eq) && eq.m_shared.m_ammoType == type)
                return eq;
            return p.GetInventory().GetAmmoItem(type);
        }
    }
}
```

- [ ] **Step 3: `src/AnglersEye/Patches/CastPatches.cs`**

```csharp
using System;
using AnglersEye.Core;
using HarmonyLib;

namespace AnglersEye.Patches
{
    [HarmonyPatch]
    internal static class CastPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Attack), nameof(Attack.StartDraw))]
        private static void StartDraw(Humanoid character, ItemDrop.ItemData weapon)
        {
            Run(character, weapon);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static void Start(Humanoid character, ItemDrop.ItemData weapon)
        {
            Run(character, weapon);
        }

        private static void Run(Humanoid character, ItemDrop.ItemData weapon)
        {
            try
            {
                SmartBait.BeforeCast(character, weapon);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("SmartBait", e);
            }
        }
    }
}
```

- [ ] **Step 4: Build, deploy, check in game**

Run: `./build/deploy.sh`
Set `Verbose = true` on the rig (ConfigurationManager, or edit `BepInEx/config/com.jumpingmushroom.anglerseye.cfg`). Ask the user to:
1. Put the wrong bait top-left and the right bait elsewhere, aim at a fish and cast.
2. Cast at a fish whose bait they don't carry.
3. Cast at open water with no fish in range.

Then `./build/logs.sh`.
Expected: (1) a `smart bait <fish> -> <bait>` log line and the float carries the right bait (vanilla shows no wrong-bait message on nibble); (2) a centre message `Angler's Eye: <fish> needs <bait>`, once, and the cast still happens; (3) nothing logged, vanilla behaviour.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/Core/Targeting.cs src/AnglersEye/Core/SmartBait.cs src/AnglersEye/Patches/CastPatches.cs
git commit -m "Smart bait: equip the best carried bait for the aimed fish on cast" && git push
```

---

### Task 12: Fishing state and the crosshair strip (target, struggle, forecast)

**Files:**
- Create: `src/AnglersEye/Core/FishingState.cs`, `src/AnglersEye/UI/Palette.cs`, `src/AnglersEye/UI/Strip.cs`
- Modify: `src/AnglersEye/Core/Runtime.cs`

**Interfaces:**
- Consumes: everything from Tasks 3–11.
- Produces:
  - `sealed class FishingSnapshot { Player Player; ItemDrop.ItemData Rod; bool RodEquipped; FishingFloat Float; Fish Catch; bool Escaping; float RemainingEscape; float LineLength; Fish Subject; }`
  - `static class FishingState { static FishingSnapshot Current; static bool IsLocal(FishingFloat ff); static FishingFloat LocalFloat(); static void Refresh(); }`
  - `static class Strip { static void Show(string text, Color color); static void Hide(); }`
  - `static class Palette { static readonly Color Idle, Missing, Calm, Struggle, Bite; }`
  - `Runtime.UpdateStrip` has no bite branch yet; Task 13 adds one that checks `BiteCue.Active`.

- [ ] **Step 1: `src/AnglersEye/Core/FishingState.cs`**

```csharp
using UnityEngine;

namespace AnglersEye.Core
{
    internal sealed class FishingSnapshot
    {
        public Player Player;
        public ItemDrop.ItemData Rod;
        public bool RodEquipped;
        public FishingFloat Float;
        public Fish Catch;
        public bool Escaping;
        public float RemainingEscape;
        public float LineLength;
        /// <summary>The fish nibbling or heading for the float, else the nearest in its range.</summary>
        public Fish Subject;
    }

    /// <summary>The local player's fishing, read once per frame (PLAN §1.2–1.4).</summary>
    internal static class FishingState
    {
        public static readonly FishingSnapshot Current = new FishingSnapshot();

        /// <summary>The float belongs to the local player (ZDO rodOwner, as FishingFloat.GetOwner reads it).</summary>
        public static bool IsLocal(FishingFloat ff)
        {
            Player p = Player.m_localPlayer;
            if (p == null || ff == null || ff.m_nview == null || !ff.m_nview.IsValid())
                return false;
            return ff.m_nview.GetZDO().GetLong(ZDOVars.s_rodOwner, 0L) == p.GetZDOID().UserID;
        }

        public static FishingFloat LocalFloat()
        {
            foreach (FishingFloat ff in FishingFloat.GetAllInstances())
                if (IsLocal(ff))
                    return ff;
            return null;
        }

        public static void Refresh()
        {
            FishingSnapshot s = Current;
            Player p = Player.m_localPlayer;
            s.Player = p;
            s.Rod = p != null ? p.GetCurrentWeapon() : null;
            s.RodEquipped = Tackle.IsRod(s.Rod);
            s.Float = p != null ? LocalFloat() : null;
            s.Catch = s.Float != null ? s.Float.GetCatch() : null;
            // The hooker owns the fish (Fish.OnHooked claims ownership), so its escape timer is live here.
            s.Escaping = s.Catch != null && s.Catch.IsEscaping();
            s.RemainingEscape = s.Catch != null ? Mathf.Max(0f, s.Catch.m_escapeTime) : 0f;
            s.LineLength = s.Float != null ? s.Float.m_lineLength : 0f;
            s.Subject = s.Float != null && s.Catch == null ? SubjectFor(s.Float) : null;
        }

        private static Fish SubjectFor(FishingFloat ff)
        {
            if (ff.m_nibbler != null && Time.time - ff.m_nibbleTime < 2f)
                return ff.m_nibbler;
            Fish nearest = null;
            float best = ff.m_range;
            Vector3 at = ff.transform.position;
            foreach (IMonoUpdater u in Fish.Instances)
            {
                Fish f = u as Fish;
                if (f == null || f.IsOutOfWater())
                    continue;
                // Only meaningful when we own the fish (its AI runs here); otherwise null (PLAN §1.1).
                if (f.m_waypointFF == ff)
                    return f;
                float d = Vector3.Distance(f.transform.position, at);
                if (d <= best)
                {
                    best = d;
                    nearest = f;
                }
            }
            return nearest;
        }
    }
}
```

- [ ] **Step 2: `src/AnglersEye/UI/Palette.cs`**

```csharp
using UnityEngine;

namespace AnglersEye.UI
{
    internal static class Palette
    {
        public static readonly Color Idle = new Color(0.9f, 0.9f, 0.9f, 1f);
        public static readonly Color Missing = new Color(0.95f, 0.45f, 0.4f, 1f);
        public static readonly Color Calm = new Color(0.55f, 0.85f, 0.45f, 1f);
        public static readonly Color Struggle = new Color(1f, 0.7f, 0.25f, 1f);
        public static readonly Color Bite = new Color(1f, 0.9f, 0.4f, 1f);
        public static readonly Color Backing = new Color(0f, 0f, 0f, 0.45f);
    }
}
```

- [ ] **Step 3: `src/AnglersEye/UI/Strip.cs`**

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    /// <summary>One line of text under the crosshair on a subtle dark backing (PLAN §2.3).</summary>
    internal static class Strip
    {
        private const float BelowCrosshair = 40f;

        private static RectTransform _root;
        private static TextMeshProUGUI _text;
        private static Hud _builtFor;

        public static void Show(string text, Color color)
        {
            if (!Ensure())
                return;
            if (!_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);
            if (_text.text != text)
                _text.text = text;
            _text.color = color;
            Place();
        }

        public static void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
                _root.gameObject.SetActive(false);
        }

        private static bool Ensure()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_crosshair == null)
                return false;
            // No ?? on Unity objects: a destroyed HUD compares equal to null only through Unity's ==.
            if (_root != null && _builtFor == hud)
                return true;

            _root = UiUtil.Rect("AnglersEyeStrip", hud.m_crosshair.transform.parent);
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = UiUtil.White;
            bg.color = Palette.Backing;
            bg.raycastTarget = false;
            var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 3, 3);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fit = _root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = UiUtil.Text(_root, "Text", 18f, TextAlignmentOptions.Center);
            _builtFor = hud;
            return true;
        }

        private static void Place()
        {
            var cross = (RectTransform)Hud.instance.m_crosshair.transform;
            _root.anchorMin = cross.anchorMin;
            _root.anchorMax = cross.anchorMax;
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = cross.anchoredPosition +
                new Vector2(PluginConfig.OffsetX.Value, -BelowCrosshair + PluginConfig.OffsetY.Value);
            _root.localScale = Vector3.one * PluginConfig.Scale.Value;
        }
    }
}
```

- [ ] **Step 4: Replace `src/AnglersEye/Core/Runtime.cs`**

```csharp
using System;
using AnglersEye.Core.Model;
using AnglersEye.UI;
using UnityEngine;

namespace AnglersEye.Core
{
    /// <summary>Per-frame work, driven from the plugin's Update: state, then the strip (PLAN §2.3).</summary>
    internal static class Runtime
    {
        private const float AimInterval = 0.25f;

        private static float _nextAim;
        private static FishSighting _aim;

        public static void Tick()
        {
            try
            {
                FishCatalog.EnsureBuilt();
                Compat.EnsureEvaluated();
                FishingState.Refresh();
                UpdateStrip(FishingState.Current);
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("Runtime.Tick", e);
                Strip.Hide();
            }
        }

        private static void UpdateStrip(FishingSnapshot s)
        {
            if (!PluginConfig.Enabled.Value || s.Player == null || !s.RodEquipped)
            {
                Strip.Hide();
                return;
            }
            Glyphs g = UiUtil.Glyphs;

            if (s.Catch != null)
            {
                bool struggle = Features.On(Feature.StruggleIndicator);
                Verdict? v = Features.On(Feature.Forecast) ? Forecast(s, s.Catch, true) : (Verdict?)null;
                if (!struggle && !v.HasValue)
                {
                    Strip.Hide();
                    return;
                }
                if (struggle)
                    Strip.Show(Labels.Hooked(s.Escaping, M(s.LineLength), v, g), s.Escaping ? Palette.Struggle : Palette.Calm);
                else
                    Strip.Show(Labels.Distance(M(s.LineLength)) + "  " + Labels.Verdict(v.Value, g, false), Palette.Idle);
                return;
            }

            if (s.Float != null)
            {
                if (!Features.On(Feature.Forecast) && !Features.On(Feature.FloatLabel))
                {
                    Strip.Hide();
                    return;
                }
                if (s.Subject == null)
                {
                    Strip.Show(Labels.Distance(M(s.LineLength)), Palette.Idle);
                    return;
                }
                FishInfo info = FishCatalog.For(s.Subject);
                Verdict? v = Features.On(Feature.Forecast) ? Forecast(s, s.Subject, false) : (Verdict?)null;
                Strip.Show(Labels.Waiting(info.Name, FishCatalog.Quality(s.Subject), M(s.LineLength), v, g), Palette.Idle);
                return;
            }

            // Rod out, nothing cast: the fish smart bait would bait for.
            if (!Features.On(Feature.SmartBait))
            {
                Strip.Hide();
                return;
            }
            if (Time.time >= _nextAim)
            {
                _nextAim = Time.time + AimInterval;
                _aim = Targeting.Aim(s.Player);
            }
            FishInfo target;
            if (_aim == null || !FishCatalog.All.TryGetValue(_aim.Species, out target))
            {
                Strip.Hide();
                return;
            }
            BaitAdvice a = Tackle.Advise(s.Player, target);
            Strip.Show(Labels.Target(target.Name, _aim.Quality, a, g, PluginConfig.ShowOdds.Value),
                a != null && !a.Carried ? Palette.Missing : Palette.Idle);
        }

        private static Verdict Forecast(FishingSnapshot s, Fish f, bool hooked)
        {
            FishInfo info = FishCatalog.For(f);
            return CatchForecast.Estimate(FishCatalog.Rod, info.Params, FishCatalog.Quality(f), s.LineLength,
                s.Player.GetSkillFactor(Skills.SkillType.Fishing), s.Player.GetStamina(), hooked,
                hooked ? s.RemainingEscape : 0f).Verdict;
        }

        private static int M(float metres)
        {
            return Mathf.Max(0, Mathf.RoundToInt(metres));
        }
    }
}
```

- [ ] **Step 5: Build, deploy, check in game**

Run: `./build/deploy.sh`
Ask the user to: equip the rod and aim at fish; cast; hook a fish and reel through a few struggles; unequip the rod. Take a screenshot while hooked: `./build/shot.sh probe-hooked`.
Expected: rod out → `Pike ★ · Cold bait ✔ (x12)` (red `needs … ✖` without the bait); float out → `Pike · 18m · ✓` or just `18m`; hooked → green `● REEL  12m  ✓ can land` switching to amber `▲ WAIT` during struggles; nothing without a rod. The strip sits under the crosshair and doesn't overlap the vanilla hover text; if it does, adjust `BelowCrosshair` and note the value in the commit message.

- [ ] **Step 6: Commit**

```bash
git add src/AnglersEye/Core/FishingState.cs src/AnglersEye/UI/Palette.cs src/AnglersEye/UI/Strip.cs src/AnglersEye/Core/Runtime.cs
git commit -m "Crosshair strip: target, struggle state and catch forecast" && git push
```

---

### Task 13: Bite cue (flash and sound)

**Files:**
- Create: `src/AnglersEye/UI/BiteCue.cs`, `src/AnglersEye/Patches/NibblePatch.cs`
- Modify: `src/AnglersEye/Core/Runtime.cs` (bite branch in `UpdateStrip`)

**Interfaces:**
- Consumes: `ReelPolicy.HookWindow` (Task 5); `Labels.Bite` (Task 6); `FishingState.IsLocal` (Task 12); `Strip`, `Palette` (Task 12).
- Produces: `static class BiteCue { static bool Active; static void Fire(); }`

- [ ] **Step 1: `src/AnglersEye/UI/BiteCue.cs`**

Set `Candidates` to the clip names recorded in PLAN §1.7.3, most preferred first. For example, if the probe listed `sfx_gui_click` and `sfx_ui_ding`, use `{ "sfx_ui_ding", "sfx_gui_click" }`.
```csharp
using System.Collections.Generic;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using UnityEngine;

namespace AnglersEye.UI
{
    /// <summary>
    /// A hookable nibble: the strip flashes BITE! for as long as it can be hooked, and a short
    /// vanilla UI sound plays in 2D through the game's GUI mixer (PLAN §2.2 item 3).
    /// </summary>
    internal static class BiteCue
    {
        // Vanilla clip names from the runtime probe (PLAN §1.7.3), most preferred first.
        private static readonly string[] Candidates = { /* names from PLAN §1.7.3 */ };

        private static float _until;
        private static bool _lookedUp;
        private static AudioClip _clip;
        private static AudioSource _source;

        public static bool Active => Time.time < _until;

        public static void Fire()
        {
            if (!Features.On(Feature.BiteCue))
                return;
            _until = Time.time + ReelPolicy.HookWindow(Features.On(Feature.HookWindow), PluginConfig.HookWindowSeconds.Value);
            if (PluginConfig.BiteSound.Value)
                Play();
        }

        private static void Play()
        {
            if (!_lookedUp)
            {
                _lookedUp = true;
                var byName = new Dictionary<string, AudioClip>();
                foreach (AudioClip c in Resources.FindObjectsOfTypeAll<AudioClip>())
                    if (c != null && !byName.ContainsKey(c.name))
                        byName[c.name] = c;
                foreach (string n in Candidates)
                    if (byName.TryGetValue(n, out _clip))
                        break;
                if (_clip == null)
                    AnglersEyePlugin.Log.LogWarning("Angler's Eye: no bite sound found; the visual cue still works.");
            }
            if (_clip == null)
                return;
            if (_source == null)
            {
                _source = AnglersEyePlugin.Instance.gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
            }
            if (AudioMan.instance != null && _source.outputAudioMixerGroup == null)
                _source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            _source.PlayOneShot(_clip, PluginConfig.BiteVolume.Value);
        }
    }
}
```
Before building, check that the array holds at least one real name: `grep -n 'Candidates = {' src/AnglersEye/UI/BiteCue.cs` must not show the comment placeholder.

- [ ] **Step 2: `src/AnglersEye/Patches/NibblePatch.cs`**

```csharp
using System;
using AnglersEye.Core;
using AnglersEye.UI;
using HarmonyLib;

namespace AnglersEye.Patches
{
    /// <summary>
    /// RPC_Nibble sets m_nibbleTime only for a correct-bait nibble it accepts (PLAN §1.3), so a
    /// changed m_nibbleTime means a hookable bite on our float.
    /// </summary>
    [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.RPC_Nibble))]
    internal static class NibblePatch
    {
        private static void Prefix(FishingFloat __instance, out float __state)
        {
            __state = __instance.m_nibbleTime;
        }

        private static void Postfix(FishingFloat __instance, bool correctBait, float __state)
        {
            try
            {
                if (correctBait && __instance.m_nibbleTime != __state && FishingState.IsLocal(__instance))
                    BiteCue.Fire();
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("NibblePatch", e);
            }
        }
    }
}
```

- [ ] **Step 3: Bite branch in `Runtime.UpdateStrip`**

In `Runtime.UpdateStrip`, directly before `if (s.Float != null)` (after the hooked block), insert:
```csharp
            if (s.Float != null && BiteCue.Active)
            {
                Strip.Show(Labels.Bite(g), Palette.Bite);
                return;
            }
```

- [ ] **Step 4: Build, deploy, check in game**

Run: `./build/deploy.sh`
Ask the user to fish with the right bait and wait for nibbles, then with the wrong bait, then with `BiteSound` off.
Expected: a correct-bait nibble flashes `» BITE! «` for 0.5 s and plays the sound; hooking inside that flash works. A wrong-bait nibble gives only vanilla's message, no flash or sound. `BiteSound = false` gives the flash only. `BiteVolume` changes the volume live.

- [ ] **Step 5: Commit**

```bash
git add src/AnglersEye/UI/BiteCue.cs src/AnglersEye/Patches/NibblePatch.cs src/AnglersEye/Core/Runtime.cs
git commit -m "Bite cue: flash and sound on a hookable nibble" && git push
```

---

### Task 14: Float label

**Files:**
- Create: `src/AnglersEye/UI/FloatLabel.cs`
- Modify: `src/AnglersEye/Core/Runtime.cs` (call `FloatLabel.Update`)

**Interfaces:**
- Consumes: `FishingSnapshot` (Task 12); `Labels.OnFloat`, `BaitAdvisor` (Tasks 3, 6); `FishCatalog`, `Tackle` (Task 8).
- Produces: `static class FloatLabel { static void Update(FishingSnapshot s); static void Hide(); }`

- [ ] **Step 1: `src/AnglersEye/UI/FloatLabel.cs`**

```csharp
using AnglersEye.Core;
using AnglersEye.Core.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnglersEye.UI
{
    /// <summary>
    /// A small label above your float naming the fish nibbling or heading for it, and whether the
    /// bait on the float works on it (PLAN §2.2 item 1). Hidden once a fish is hooked.
    /// </summary>
    internal static class FloatLabel
    {
        private const float Above = 0.8f;

        private static RectTransform _root;
        private static TextMeshProUGUI _text;
        private static Hud _builtFor;

        public static void Update(FishingSnapshot s)
        {
            if (!Features.On(Feature.FloatLabel) || s.Float == null || s.Catch != null || s.Subject == null)
            {
                Hide();
                return;
            }
            Camera cam = Utils.GetMainCamera();
            if (cam == null)
            {
                Hide();
                return;
            }
            Vector3 screen = cam.WorldToScreenPointScaled(s.Float.transform.position + Vector3.up * Above);
            if (screen.z < 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height)
            {
                Hide();
                return;
            }
            if (!Ensure())
                return;

            FishInfo info = FishCatalog.For(s.Subject);
            string onFloat = s.Float.GetBait();
            bool works = false;
            foreach (BaitOption b in info.Baits)
                if (b.BaitId == onFloat)
                    works = true;
            BaitAdvice best = BaitAdvisor.Advise(info.Baits, Tackle.Carried(s.Player));
            string needed = best != null ? best.Best.BaitName : "?";
            string text = Labels.OnFloat(info.Name, FishCatalog.Quality(s.Subject), works, needed, UiUtil.Glyphs);

            if (!_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);
            if (_text.text != text)
                _text.text = text;
            _text.color = works ? Palette.Idle : Palette.Missing;
            _root.position = screen;
            _root.localScale = Vector3.one * PluginConfig.Scale.Value;
        }

        public static void Hide()
        {
            if (_root != null && _root.gameObject.activeSelf)
                _root.gameObject.SetActive(false);
        }

        private static bool Ensure()
        {
            Hud hud = Hud.instance;
            if (hud == null || hud.m_crosshair == null)
                return false;
            if (_root != null && _builtFor == hud)
                return true;
            _root = UiUtil.Rect("AnglersEyeFloatLabel", hud.m_crosshair.transform.parent);
            _root.pivot = new Vector2(0.5f, 0f);
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = UiUtil.White;
            bg.color = Palette.Backing;
            bg.raycastTarget = false;
            var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fit = _root.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _text = UiUtil.Text(_root, "Text", 16f, TextAlignmentOptions.Center);
            _builtFor = hud;
            return true;
        }
    }
}
```

- [ ] **Step 2: Call it from `Runtime.Tick`**

In `Runtime.Tick`, after `UpdateStrip(FishingState.Current);`:
```csharp
                FloatLabel.Update(FishingState.Current);
```
And in the `catch` block, after `Strip.Hide();`:
```csharp
                FloatLabel.Hide();
```

- [ ] **Step 3: Build, deploy, check in game**

Run: `./build/deploy.sh`
Ask the user to cast near fish with the right bait, then the wrong bait, and to turn the camera away from the float. Capture a screenshot: `./build/shot.sh probe-float`.
Expected: `Pike ★ · ✔` above the float with the right bait; red `Pike · needs Cold bait ✖` with the wrong one; the label disappears when the float is off-screen, when nothing is in range, and once a fish is hooked. The label follows the float without jitter.

- [ ] **Step 4: Commit**

```bash
git add src/AnglersEye/UI/FloatLabel.cs src/AnglersEye/Core/Runtime.cs
git commit -m "Float label: the fish at your float and whether your bait works on it" && git push
```

---

### Task 15: Assists: smart reel and extended hook window

**Files:**
- Create: `src/AnglersEye/Patches/ReelPatches.cs`

**Interfaces:**
- Consumes: `ReelPolicy` (Task 5); `Features` (Task 9); `FishingState.IsLocal` (Task 12).
- Produces: nothing new for other tasks.

Both are off by default. Smart reel makes `Humanoid.IsBlocking` return false **only** while the local float's `FixedUpdate` runs and its hooked fish is struggling, so nothing else sees a change. The hook-window prefix re-implements `TryToHook` (PLAN §1.3) with the configured window.

- [ ] **Step 1: `src/AnglersEye/Patches/ReelPatches.cs`**

```csharp
using System;
using AnglersEye.Core;
using AnglersEye.Core.Model;
using HarmonyLib;
using UnityEngine;

namespace AnglersEye.Patches
{
    [HarmonyPatch]
    internal static class ReelPatches
    {
        /// <summary>True only inside the local float's FixedUpdate while its fish struggles.</summary>
        [ThreadStatic]
        private static bool _suppressBlock;

        [HarmonyPrefix, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
        private static void FloatUpdatePrefix(FishingFloat __instance)
        {
            _suppressBlock = false;
            try
            {
                if (!Features.On(Feature.SmartReel) || !FishingState.IsLocal(__instance))
                    return;
                Fish f = __instance.GetCatch();
                _suppressBlock = ReelPolicy.SuppressReel(true, f != null, f != null && f.IsEscaping());
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("SmartReel", e);
            }
        }

        [HarmonyFinalizer, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.FixedUpdate))]
        private static void FloatUpdateFinalizer()
        {
            _suppressBlock = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
        private static bool IsBlockingPrefix(ref bool __result)
        {
            if (!_suppressBlock)
                return true;
            __result = false;
            return false;
        }

        /// <summary>FishingFloat.TryToHook with the configured window instead of 0.5 s.</summary>
        [HarmonyPrefix, HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.TryToHook))]
        private static bool TryToHookPrefix(FishingFloat __instance)
        {
            try
            {
                if (!Features.On(Feature.HookWindow) || !FishingState.IsLocal(__instance))
                    return true;
                float window = ReelPolicy.HookWindow(true, PluginConfig.HookWindowSeconds.Value);
                if (__instance.m_nibbler != null && ReelPolicy.InHookWindow(Time.time, __instance.m_nibbleTime, window) &&
                    __instance.GetCatch() == null)
                {
                    __instance.Message("$msg_fishing_hooked", prioritized: true);
                    __instance.SetCatch(__instance.m_nibbler);
                    __instance.m_nibbler = null;
                    Game.instance.IncrementPlayerStat(PlayerStatType.FishHooked);
                }
                return false;
            }
            catch (Exception e)
            {
                AnglersEyePlugin.WarnOnce("HookWindow", e);
                return true;
            }
        }
    }
}
```

- [ ] **Step 2: Build, deploy, check in game**

Run: `./build/deploy.sh`
Ask the user to:
1. With both assists off: hook and reel a fish holding Block throughout. Vanilla: the line shortens (slowly) during struggles too.
2. `SmartReel = true`: hold Block throughout. The line stops shortening while the strip shows `▲ WAIT` and resumes on `● REEL`. Reeling still shortens the line normally when calm, and the fish can still be lost at 0 stamina.
3. `ExtendedHookWindow = true`, `HookWindowSeconds = 1.5`: hook about 1 s after a nibble; it hooks, and the bite flash lasts 1.5 s. With the option off, a hook attempt 1 s late misses.
4. Run `anglerseye`: both assists listed under **on** while enabled.

- [ ] **Step 3: Commit**

```bash
git add src/AnglersEye/Patches/ReelPatches.cs
git commit -m "Assists: smart reel and extended hook window (both off by default)" && git push
```

---

### Task 16: Full in-game verification pass

**Files:**
- Modify: `PLAN.md` (append §3 "In-game findings"), and any file a failed check needs fixing (each fix is its own commit).

- [ ] **Step 1: Reset config to defaults on the rig**

```bash
ssh equ@192.168.1.160 "rm -f /home/equ/.config/r2modmanPlus-local/Valheim/profiles/Default/BepInEx/config/com.jumpingmushroom.anglerseye.cfg"
./build/deploy.sh
```

- [ ] **Step 2: Run the checklist from PLAN §2.7 with the user**

For each item, record pass/fail:
1. Right bait / wrong bait / no bait carried: hover line, pre-cast strip, smart bait equip, "needs" message.
2. Hovering a fish under water (or the documented limitation).
3. Float label: right, wrong, off-screen, nothing in range.
4. Bite flash and sound; wrong-bait nibble silent.
5. REEL/WAIT tracks struggles; forecast shows ✓ on an easy fish and ✖ on a high-level fish at low stamina. Also lose a fish at 0 stamina after a ✖ forecast.
6. Smart reel on/off; hook window on/off.
7. Master `Enabled = false` hides everything and the vanilla game behaves normally.
8. The strip and label look right at `Scale` 0.5 and 2, and `OffsetX`/`OffsetY` move the strip.
9. `anglerseye` status and `anglerseye fish` output.
10. The BepInEx log has no Angler's Eye warnings after a 10-minute session (`./build/logs.sh`).

Fix any failure with systematic debugging (superpowers:systematic-debugging) before moving on, one commit per fix.

- [ ] **Step 3: Record findings and commit**

Append to `PLAN.md`:
```markdown
## 3. In-game findings (0.1.0, <date>)
- <one bullet per checklist item: result, and any tuning such as BelowCrosshair or clip choice>
```
```bash
git add PLAN.md && git commit -m "PLAN: in-game verification for 0.1.0" && git push
```

---

### Task 17: Docs, icon, package and release 0.1.0

**Files:**
- Create: `build/make_icon.py`, `thunderstore/icon.png`, `README.md`, `thunderstore/README.md`, `CHANGELOG.md`, `docs/images/*.jpg`

- [ ] **Step 1: Screenshots**

With the user in game: `./build/shot.sh probe-hooked` (hooked, strip visible) and `./build/shot.sh probe-float` (float label + strip). Crop each to the interesting area with `./build/crop.sh probe-hooked hooked <WxH+X+Y>` and `./build/crop.sh probe-float float <WxH+X+Y>` (pick the geometry by viewing the PNG with the Read tool), producing `docs/images/hooked.jpg` and `docs/images/float.jpg`.

- [ ] **Step 2: `build/make_icon.py`**

```python
"""Thunderstore icon: 256x256 PNG. A dark plate with a golden border, a pale eye whose iris is
water-blue, and a red-and-white fishing float as the pupil: "an eye on your fishing".
Written without PIL, which the build box lacks.
Run from the repo root: python3 build/make_icon.py"""
import math
import struct
import zlib

S = 256
SS = 3                       # supersample
W = S * SS

px = bytearray(W * W * 4)

def blend(x, y, col, a=1.0):
    if x < 0 or y < 0 or x >= W or y >= W:
        return
    i = (y * W + x) * 4
    ia = 1.0 - a
    px[i] = int(col[0] * a + px[i] * ia)
    px[i + 1] = int(col[1] * a + px[i + 1] * ia)
    px[i + 2] = int(col[2] * a + px[i + 2] * ia)
    px[i + 3] = int(min(255, 255 * a + px[i + 3] * ia))

def fill(inside, bbox, col, a=1.0):
    x0, y0, x1, y1 = (int(v * SS) for v in bbox)
    for y in range(max(0, y0), min(W, y1)):
        for x in range(max(0, x0), min(W, x1)):
            if inside((x + 0.5) / SS, (y + 0.5) / SS):
                blend(x, y, col, a)

def rounded(x0, y0, x1, y1, r):
    def f(x, y):
        dx = max(x0 + r - x, 0, x - (x1 - r))
        dy = max(y0 + r - y, 0, y - (y1 - r))
        return x0 <= x < x1 and y0 <= y < y1 and dx * dx + dy * dy <= r * r
    return f

def circle(cx, cy, r):
    return lambda x, y: (x - cx) ** 2 + (y - cy) ** 2 <= r * r

def eye(cx, cy, w, h):
    # Almond: intersection of two circles through the eye corners.
    half = w / 2
    R = (half * half + h * h) / (2 * h)
    return lambda x, y: ((x - cx) ** 2 + (y - (cy + R - h)) ** 2 <= R * R and
                         (x - cx) ** 2 + (y - (cy - R + h)) ** 2 <= R * R)

PLATE = (0x1c, 0x1a, 0x17)
BORDER = (0xc8, 0xa0, 0x50)
WHITE = (0xee, 0xe8, 0xd8)
IRIS = (0x3a, 0x8f, 0xb0)
IRIS_DARK = (0x1f, 0x5a, 0x75)
RED = (0xd8, 0x44, 0x3a)
FLOAT_WHITE = (0xf4, 0xf0, 0xe6)
LINE = (0x9a, 0x94, 0x88)

fill(rounded(0, 0, 256, 256, 34), (0, 0, 256, 256), BORDER)
fill(rounded(10, 10, 246, 246, 26), (0, 0, 256, 256), PLATE)

cx, cy = 128, 132
fill(eye(cx, cy, 200, 70), (20, 55, 236, 210), WHITE)
fill(circle(cx, cy, 52), (70, 75, 190, 190), IRIS_DARK)
fill(circle(cx, cy, 46), (70, 75, 190, 190), IRIS)

# the float as the pupil: white top half, red bottom half, a thin line up to the top edge
fill(lambda x, y: abs(x - cx) <= 1.5 and 30 <= y <= cy - 22, (120, 25, 136, cy), LINE)
fill(lambda x, y: circle(cx, cy, 22)(x, y) and y < cy, (100, 105, 156, 160), FLOAT_WHITE)
fill(lambda x, y: circle(cx, cy, 22)(x, y) and y >= cy, (100, 105, 156, 160), RED)
fill(circle(cx - 8, cy - 9, 5), (110, 115, 130, 130), (255, 255, 255), 0.8)

# downsample
out = bytearray()
for y in range(S):
    row = bytearray([0])
    for x in range(S):
        r = g = b = a = 0
        for sy in range(SS):
            for sx in range(SS):
                i = ((y * SS + sy) * W + (x * SS + sx)) * 4
                r += px[i]; g += px[i + 1]; b += px[i + 2]; a += px[i + 3]
        n = SS * SS
        row += bytes((r // n, g // n, b // n, a // n))
    out += row

def chunk(tag, data):
    c = tag + data
    return struct.pack(">I", len(data)) + c + struct.pack(">I", zlib.crc32(c) & 0xffffffff)

png = b"\x89PNG\r\n\x1a\n"
png += chunk(b"IHDR", struct.pack(">IIBBBBB", S, S, 8, 6, 0, 0, 0))
png += chunk(b"IDAT", zlib.compress(bytes(out), 9))
png += chunk(b"IEND", b"")
open("thunderstore/icon.png", "wb").write(png)
```
Run `python3 build/make_icon.py`, then view `thunderstore/icon.png` with the Read tool. It must read clearly as an eye with a fishing float for a pupil. Adjust sizes if not.

- [ ] **Step 3: `README.md`** (GitHub; images via relative paths)

```markdown
# Angler's Eye

Angler's Eye is a fishing assistant for Valheim. It tells you which fish is in the water and
whether you carry the bait it wants, puts the right bait on your hook when you cast, tells you
the moment a fish bites, shows whether a hooked fish is struggling or calm, and estimates whether
you have the stamina to land it. It doesn't change how fishing works: stamina costs, fish
spawns, bait rules and catches are all vanilla. Client-side, works on vanilla servers, BepInEx
only.

![Reeling in: the struggle indicator and catch forecast under the crosshair](docs/images/hooked.jpg)

*Hooked: REEL while the fish is calm, WAIT while it struggles, and whether you can land it.*

![A cast float with the label naming the fish heading for it](docs/images/float.jpg)

*Waiting for a bite: the fish at your float, and whether the bait on it works.*

## Features

- **Fish identification.** Look at a fish and its hover text adds its stars and the bait it takes:
  ✔ with how many you carry, or ✖. A small label above your float names the fish nibbling or
  heading for it and whether the bait on your hook works on it.
- **Smart bait.** When you cast, Angler's Eye equips the bait you carry that works best on the fish
  you're aiming at, instead of whatever sits nearest the top-left of your inventory. If you carry
  none of its bait, it tells you which bait it needs, and you still cast.
- **Bite cue.** A hookable nibble flashes **BITE!** under your crosshair for exactly as long as you
  can hook it, with a short sound.
- **Struggle indicator.** While a fish is hooked: **REEL** when it's calm, **WAIT** when it
  struggles. Reeling during a struggle costs more stamina and gains half the line.
- **Catch forecast.** ✓ can land, ~ tight, or ✖ unlikely, from the line length, the fish's level,
  your Fishing skill and your stamina.
- **Optional assists, off by default:**
  - **Smart reel**: while you hold Block, it reels only when the fish is calm. Stamina costs stay
    vanilla; it just does the waiting for you.
  - **Extended hook window**: up to 1.5 s to hook a nibble instead of vanilla's 0.5 s.

## How it works

Everything comes from data the game already has on your machine: each fish's bait table, its
level, and the state of your own float and hooked fish. Nothing is sent to the server, and no
fish, bait or drop is changed. The right bait still only works on a roll each nibble, as in
vanilla. Turn on `ShowOdds` to see that chance.

The forecast assumes you reel only while the fish is calm and uses average struggle lengths, so
it's an estimate. It ignores stamina regeneration, which makes it slightly cautious.

In multiplayer, the fish near your float are run by whoever owns that area. If that's another
player, the float label shows the nearest fish in bite range instead of the one heading for your
float.

## Configuration

Everything is in `BepInEx/config/com.jumpingmushroom.anglerseye.cfg`, or in-game with
ConfigurationManager (F1). Changes apply immediately.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | on | Master switch |
| `HoverInfo` | on | Stars and bait on a fish's hover text |
| `FloatLabel` | on | Label above your float |
| `ShowOdds` | off | Show the bait's chance per nibble |
| `SmartBait` | on | Equip the right bait on cast |
| `AimConeDegrees` | 10 | How far off your aim a fish still counts (3–30) |
| `BiteCue` | on | BITE! flash |
| `BiteSound` / `BiteVolume` | on / 0.7 | Bite sound and its volume |
| `StruggleIndicator` | on | REEL / WAIT while hooked |
| `Forecast` | on | ✓ / ~ / ✖ catch forecast |
| `SmartReel` | off | Reel only while the fish is calm when holding Block |
| `ExtendedHookWindow` / `HookWindowSeconds` | off / 1.0 | Longer hook window (0.5–1.5 s) |
| `Scale`, `OffsetX`, `OffsetY` | 1, 0, 0 | Size of the text, and where the strip sits |

Console: `anglerseye` shows what's on and why; `anglerseye fish` lists every fish's bait and fight
numbers.

## Compatibility

Angler's Eye steps aside for mods that take over fishing, and says so in the log and in the
`anglerseye` command:

- **Hooked**: smart bait, the forecast, both assists and the bite and struggle cues turn off. Fish
  identification stays.
- **Trolling Fishing**: smart bait, the forecast and both assists turn off.
- Any mod that patches the float's reeling turns off smart reel; one that patches hooking turns off
  the hook window; one that changes fish stamina costs (e.g. Reely Good Rod) turns off the forecast.

## Installation

Install with r2modman or Thunderstore Mod Manager, or copy `AnglersEye.dll` into
`BepInEx/plugins/`. Requires BepInExPack Valheim. Client-side only; nothing is needed on the server.

## Building

`dotnet test tests/AnglersEye.Tests` runs the model tests; `./build/package.sh` builds the
Thunderstore zip. Reference assemblies go in `lib/` (see `Directory.Build.props`).
```

`thunderstore/README.md`: the same content, with the two image lines changed to absolute URLs so they render on Thunderstore:
```bash
sed -e 's#(docs/images/#(https://raw.githubusercontent.com/jumpingmushroom/AnglersEye/main/docs/images/#g' \
    -e '/^## Building/,$d' README.md > thunderstore/README.md
```

- [ ] **Step 4: `CHANGELOG.md`**

```markdown
# Changelog

## 0.1.0 — first cut

- Fish identification: stars and bait (✔ carried / ✖ not) on a fish's hover text, and a label
  above your float naming the fish at it and whether your bait works on it.
- Smart bait: casting equips the carried bait that works best on the fish you're aiming at, or
  tells you which bait it needs.
- Bite cue: a BITE! flash for exactly the hook window, plus a short sound.
- Struggle indicator: REEL while the hooked fish is calm, WAIT while it struggles.
- Catch forecast: ✓ can land, ~ tight, ✖ unlikely.
- Optional assists (off by default): smart reel and an extended hook window (up to 1.5 s).
- Steps aside automatically for Hooked, Trolling Fishing and mods that patch reeling, hooking or
  fish stamina costs.
- `anglerseye` and `anglerseye fish` console commands.
```

- [ ] **Step 5: Package and verify**

Run: `./build/package.sh`
Expected: `ok: AnglersEye 0.1.0, 1 dependencies` and a listing with `manifest.json`, `README.md`, `icon.png`, `CHANGELOG.md`, `LICENSE`, `plugins/AnglersEye/AnglersEye.dll` at the zip root.
Run: `dotnet test tests/AnglersEye.Tests --nologo -v minimal`. Expected: `Failed: 0`.
Check that there's no AI attribution anywhere: `grep -rniE 'claude|anthropic|co-authored|generated with' README.md thunderstore/README.md CHANGELOG.md; git log --format=%B | grep -niE 'claude|anthropic|co-authored' || echo clean`. Expected: `clean` and no file matches.

- [ ] **Step 6: Commit, tag, release, copy to the rig**

```bash
git add build/make_icon.py thunderstore/icon.png thunderstore/README.md README.md CHANGELOG.md docs/images/hooked.jpg docs/images/float.jpg
git commit -m "0.1.0" && git push
git tag v0.1.0 && git push --tags
gh release create v0.1.0 dist/AnglersEye-0.1.0.zip --title "Angler's Eye 0.1.0" --notes-file <(sed -n '/## 0.1.0/,$p' CHANGELOG.md | tail -n +2)
scp dist/AnglersEye-0.1.0.zip equ@192.168.1.160:~/Downloads/
ssh equ@192.168.1.160 "ls -l ~/Downloads/AnglersEye-0.1.0.zip"
```
Expected: the release URL is printed and the zip is listed in the rig's Downloads. Tell the user it's ready to upload to Thunderstore, with categories Client-side, Utility, Deep North Update and AI Generated (or `TS_TEAM=... TCLI_AUTH_TOKEN=... ./build/publish.sh`, which sets them).
