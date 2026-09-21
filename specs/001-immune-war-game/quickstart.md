# Quickstart: Immune War Development

## Current implementation status (2026-09-20)

US1-US4 gameplay, campaign, presentation, responsive UI, persistence, and performance harnesses are implemented. Automated story suites and the performance/memory suite pass. The 51 original manifest records and 14 new visual batch records are approved. Two batch imports now provide animation sheets for all six defenders and all four enemies, art for all three organ maps, and UI skins. The full visual batch passed its import and targeted PlayMode tests; see `README.md`, `Docs/AssetPipeline/BatchArt.md`, and the evidence directory for build/player verification.

## Prerequisites

- Unity Editor `6000.3.24f1` with Windows Build Support.
- Repository opened from its root directory.
- No unresolved package import/compile errors.
- Source asset work kept outside runtime folders until approved.

The repository already declares URP 2D, Input System, uGUI, 2D packages, and Unity Test Framework. Do not add a gameplay framework, networking package, runtime AI package, Spine runtime, or custom Spine parser at any milestone in this feature.

## Open and verify the project

1. Open the repository root in Unity Hub with Editor `6000.3.24f1`.
2. Allow the existing packages and assets to import.
3. Confirm the active render pipeline uses `Assets/Settings/UniversalRP.asset` with the 2D renderer.
4. Confirm Input Handling uses the installed Input System.
5. Open `Assets/ImmunWar/Scenes/Prototype.unity` only as a visual reference. New gameplay must target the planned `Bootstrap`, `MainMenu`, and reusable `Battle` scenes.
6. Resolve compile/import errors before authoring data or prefabs.

## Implement in vertical slices

### 1. Foundation

- Create runtime, editor, EditMode test, and PlayMode test assembly definitions under `Assets/ImmunWar`.
- Implement stable ID validation, deterministic time/random abstractions, battle states, command/event contracts, and versioned persistence interfaces.
- Create the Input Actions asset and the Bootstrap/Main Menu/Battle scene shell.
- Add config validators that reject duplicate/missing IDs and mutable runtime use of authored configuration.

### 2. P0 lung battle

- Author one lung map config with at least one continuous route, defense nodes, ATP/vitality values, and a small wave set.
- Author Macrophage, T-Cell, Energy Cell, and Basic Virus configs and presentation prefabs.
- Implement placement, ATP, route movement, targeting, combat, rewards, vitality, pause/restart, victory/defeat, HUD, audio mixer, and fallback presentation.
- Integrate all 17 P0 manifest items or approved substitutes only after the asset gate passes.

### 3. Expanded game

- Add infection/cleanse, mutation, Fever, remaining defenders/enemies, progression, brain/stomach configs, and boss phases.
- Keep one reusable battle scene; new maps are authored data and presentation sets, not duplicated gameplay scenes.
- Complete the 51-record asset catalog and release evidence.

## Asset intake

1. Place downloaded/generated originals in `AssetSource/Incoming`, never directly in `Assets/ImmunWar`.
2. Create/update `Docs/AssetProvenance/records/<assetId>.json` according to `contracts/asset-record.schema.json` and retain source-type-specific evidence at acquisition time.
3. Move editable masters to `AssetSource/Working`; record raw and working hashes.
4. Review rights, references, watermark/IP/likeness risk, visual/audio quality, and gameplay-scale readability.
5. Export approved PNG/WAV content, record its hash, set status to `Approved`, then copy only that export into the corresponding `Assets/ImmunWar` runtime folder.
6. Run the import/provenance validator. Do not bypass failures by changing the record to approved without evidence.

Move legacy Spine source/archive files out of runtime folders into `AssetSource/ApprovedMasters/LegacySpine` or an external archive. For P0, use a static export only after its shippable record is approved. Do not install a Spine runtime or custom parser. Bake to ordinary PNG animation only when source rights and a compatible authorized editor have been confirmed; otherwise replace the art.

## Run automated tests

Replace `<UNITY_EDITOR>` with the full Unity executable path and `<PROJECT_ROOT>` with this repository root.

Create output directories and fail the calling shell when Unity returns a nonzero exit code or the expected NUnit XML/build artifact is missing:

```powershell
New-Item -ItemType Directory -Force "<PROJECT_ROOT>/Logs", "<PROJECT_ROOT>/Builds/Windows", "<PROJECT_ROOT>/Builds/PlayerTests"
```

### EditMode

```powershell
& "<UNITY_EDITOR>" -batchmode -projectPath "<PROJECT_ROOT>" -runTests -testPlatform EditMode -testResults "<PROJECT_ROOT>/Logs/EditMode.xml"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path "<PROJECT_ROOT>/Logs/EditMode.xml")) { throw 'EditMode tests failed or produced no XML.' }
```

### PlayMode

```powershell
& "<UNITY_EDITOR>" -batchmode -projectPath "<PROJECT_ROOT>" -runTests -testPlatform PlayMode -testResults "<PROJECT_ROOT>/Logs/PlayMode.xml"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path "<PROJECT_ROOT>/Logs/PlayMode.xml")) { throw 'PlayMode tests failed or produced no XML.' }
```

### Standalone Player tests

```powershell
& "<UNITY_EDITOR>" -batchmode -projectPath "<PROJECT_ROOT>" -runTests -testPlatform StandaloneWindows64 -buildPlayerPath "<PROJECT_ROOT>/Builds/PlayerTests/ImmunWarTests.exe" -testResults "<PROJECT_ROOT>/Logs/PlayerTests.xml"
if ($LASTEXITCODE -ne 0 -or -not (Test-Path "<PROJECT_ROOT>/Logs/PlayerTests.xml")) { throw 'Player tests failed or produced no XML.' }
```

### Clean Windows build

After `ImmunWar.Editor.BuildCommands.BuildWindows` exists, it must delete only its validated feature build output, build the configured scenes for `StandaloneWindows64`, inspect `BuildReport`, and return failure on any build error:

```powershell
& "<UNITY_EDITOR>" -batchmode -projectPath "<PROJECT_ROOT>" -buildTarget StandaloneWindows64 -executeMethod ImmunWar.Editor.BuildCommands.BuildWindows -logFile "<PROJECT_ROOT>/Logs/BuildWindows.log" -quit
if ($LASTEXITCODE -ne 0 -or -not (Test-Path "<PROJECT_ROOT>/Builds/Windows/ImmunWar.exe")) { throw 'Windows build failed or produced no executable.' }
```

### Executable smoke

After `ImmunWar.Editor.SmokeTestRunner` exists, the `-smokeTest` launch must automatically load menu → lung battle → exit within 120 seconds and return a nonzero code on failure:

```powershell
$process = Start-Process "<PROJECT_ROOT>/Builds/Windows/ImmunWar.exe" -ArgumentList '--immunwar-smoke','-logFile','<PROJECT_ROOT>/Logs/ExecutableSmoke.log' -PassThru
Wait-Process -Id $process.Id -Timeout 120
$process.Refresh()
if (-not $process.HasExited -or $process.ExitCode -ne 0) { throw 'Executable smoke failed or timed out.' }
if (Select-String -Path "<PROJECT_ROOT>/Logs/ExecutableSmoke.log" -Pattern 'Exception|Assertion failed|NullReferenceException') { throw 'Executable smoke log contains errors.' }
```

Minimum green coverage before the P0 milestone:

- Valid placement spends ATP once; every invalid path spends none.
- Enemy death/reward and organ arrival/damage resolve exactly once.
- Pause, restart, victory, and defeat are idempotent.
- Same seed plus command sequence yields the same rule-state snapshot.
- Missing/corrupt save falls back safely; valid save survives 20 reload cycles.
- Legacy saves migrate before current-schema validation; semantic validation enforces completed ⊆ unlocked, selected map unlocked, and campaign unlock order.
- Primary/backup selection uses the highest valid `saveSequence`; if both are corrupt, only lung is unlocked by default.
- Settings preview followed by Cancel writes nothing; Apply commits exactly once.
- Real Battle scene completes a short placement → wave → result flow.

## Manual P0 smoke test

1. Start from Bootstrap with no save file.
2. Enter the lung map without a network connection.
3. Place Macrophage, T-Cell, and Energy Cell and verify their distinct roles.
4. Attempt insufficient-ATP, occupied-node, outside-node, paused, and terminal-state placements.
5. Complete one victory and one defeat; confirm only one result and one progress decision per attempt.
6. Restart after both outcomes and confirm no duplicated listeners, rewards, spawns, or pooled state.
7. Change music and SFX independently, close, relaunch, and confirm persistence.
8. Repeat at 1920×1080 and 1280×720; critical HUD content must remain visible.

## Performance check

- Use a Windows Development Build on the reference machine: Windows 10/11 x64, Intel Core i5-8400-class CPU, GeForce GTX 1050 2 GB-class GPU, 8 GB RAM, Direct3D 11, Medium quality, 1920×1080.
- The custom `ProfilerRecorder` harness disables VSync and frame cap, warms up for 10 seconds, then samples 7,200 frames with 30 enemies, 15 defenders, projectiles, VFX, HUD, and Fever active.
- Calculate nearest-rank p95/p99 for total frame time and report CPU main-thread/GPU counters separately. A missing GPU counter means GPU acceptance was not measured.
- Target p95 ≤ 16.67 ms, p99 ≤ 25 ms, no recurring gameplay spike >50 ms, no command acknowledgement >250 ms, and zero steady-state managed allocation per frame where practical.
- Before release, run the scenario for 30 minutes. Reject pools that do not return to inter-wave baseline or sustained managed-plus-native memory growth above the greater of 32 MB or 5% of the post-warm-up baseline.

## Build gate

A distributable Windows build requires all of the following:

1. Clean import and compilation.
2. Passing EditMode and PlayMode suites.
3. Passing Player smoke tests.
4. Passing asset/provenance/import validation.
5. A successful clean `StandaloneWindows64` build.
6. Menu → lung battle → exit executable smoke with no unexpected error/exception log.
7. Required attribution/NOTICE material included.
8. Zero runtime assets with unresolved, rejected, or revoked rights status.

## Design references

- [Feature specification](spec.md)
- [Implementation plan](plan.md)
- [Research decisions](research.md)
- [Data model](data-model.md)
- [Battle interface contract](contracts/battle-interface-contract.md)
- [UI flow contract](contracts/ui-flow-contract.md)
- [Asset provenance contract](contracts/asset-provenance-contract.md)
- [Asset record schema](contracts/asset-record.schema.json)
- [Save schema](contracts/save-data.schema.json)
