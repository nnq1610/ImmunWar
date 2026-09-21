# Immune War

Offline Unity 6 URP 2D lane-defense game. The current implementation includes the lung vertical slice, tactical infection/mutation/Fever systems, a three-organ campaign, a multi-phase Super Pathogen boss, responsive/accessibility UI, independent music/SFX settings, deterministic saves, and automated performance/player-smoke harnesses.

## Open the project

- Unity: `6000.3.24f1` with Windows Build Support.
- Primary scenes: `Assets/ImmunWar/Scenes/Bootstrap.unity`, `MainMenu.unity`, and `Battle.unity`.
- Detailed commands: `specs/001-immune-war-game/quickstart.md`.

## Verification status (2026-09-20)

- US1: EditMode 17/17 and PlayMode 3/3 passed.
- US2: EditMode 7/7; tactical journey plus US1 regression 4/4 passed.
- US3: EditMode 4/4; campaign journey plus prior regression 5/5 passed.
- US4: presentation routing 1/1 and responsive/audio PlayMode 2/2 passed.
- Performance/memory: 2/2 passed; p50 0.114 ms, p95 0.161 ms, p99 0.212 ms in batchmode NullGfx.
- Asset records: 65 approved records, including 14 generated visual outputs across two batches.
- Full visual batch: imported together from `AssetSource/Incoming/Batches/2026-09-20-full-visuals/batch.json`; all six defenders and four enemies now have sprite animation controllers. Brain and stomach have dedicated map plates, alongside the existing lung map and UI kit.
- PlayMode journey 2/2 passed after the full batch import. It checks all roster controllers, map art, themed buttons, route movement, visible sprite frame changes, and animated unit health bars.
- Windows release build passed after the full visual batch. Executable visual smoke entered a battle and saved 1280 x 720 menu/battle screenshots for lung, brain, and stomach in `Logs/FinalVisual_map_lung`, `Logs/FinalVisual_map_brain`, and `Logs/FinalVisual_map_stomach`.

## Known limitations

- Several generated idle/move frames differ subtly at gameplay scale. The in-game bob and scale motion adds visible movement, while action sheets provide clearer effects for defenders.
- Performance figures measure deterministic simulation in batchmode and do not replace GPU profiling on the reference Direct3D 11 machine.
- The project is offline-only and contains no Spine runtime or custom Spine parser.

## Asset approval

For future batches, follow [the batch art workflow](Docs/AssetPipeline/BatchArt.md). Review each record under `Docs/AssetProvenance/records`, inspect its runtime export at gameplay scale, verify audio/visual quality and similarity/IP risk, and verify evidence and hashes before importing. Re-run the full audit and release commands afterward.
