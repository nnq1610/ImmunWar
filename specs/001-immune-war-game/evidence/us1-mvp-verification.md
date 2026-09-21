# US1 MVP verification

Date: 2026-09-18

## Automated results

- EditMode: 17 passed, 0 failed (`TestResults/EditMode-results.xml`).
- PlayMode: 3 passed, 0 failed (`TestResults/PlayMode-results.xml`).
- Runtime, editor, EditMode test, and PlayMode test assemblies compiled under Unity 6000.3.24f1.
- The PlayMode journey loaded the registered `Battle` scene, found its `BattleSceneInstaller`, started `map_lung`, reached defeat through organ damage, restarted with a new battle ID and clean state, then reached victory.
- Deterministic tests cover fixed 30 Hz timing, seeded random replay, command ordering/deduplication, placement/ATP, route/wave arrival, targeting, damage, death, persistence recovery, and provenance validation.

## Content generated

- P0 config assets: Macrophage, T-Cell, Energy Cell, Basic Virus, lung route/nodes/map, and two waves.
- P0 prefabs: three defenders, Basic Virus, T-Cell projectile, and hit burst.
- P0 presentation: generated unit sprites, vessel tiles, node, VFX, HUD icons, two synthesized SFX, sprite atlas, idle controllers, HUD prefab, and composed battle scene.

## Open release gate

The 17 P0 asset records are present, hashed, and in `Review`. Human gameplay-scale visual/audio and similarity review must change each accepted record to `Approved` before the release build guard may pass. This does not invalidate simulation/UI tests, but it prevents claiming the asset acceptance tasks T034–T037 complete.
