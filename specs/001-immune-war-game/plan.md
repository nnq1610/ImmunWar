# Implementation Plan: Immune War Playable Game

**Branch**: `001-immune-war-game` | **Date**: 2026-09-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-immune-war-game/spec.md`

## Summary

Build Immune War as a data-driven, offline 2D lane-defense game in the existing Unity 6 URP 2D project. One reusable battle scene loads authored map, route, wave, defender, enemy, status, and presentation configurations. Plain C# runtime state owns deterministic battle rules; Unity components adapt that state to sprites, input, UI, audio, particles, and scene lifecycle. The first delivery is the 17-asset P0 lung-map slice, followed by infection/mutation/Fever systems, the remaining roster, two more organ maps, and the multi-phase boss.

All approved runtime content stays under `Assets/ImmunWar`; raw sources, references, editor/tool packages, evidence, and rejected work remain outside runtime folders. Online-generated and third-party content passes through quarantine, provenance/license review, technical validation, and explicit approval before it is referenced by runtime content. The existing Spine 3.7.94 artwork may provide a static prototype export only after that export's record is approved; otherwise it is replaced. No Spine runtime or custom parser is part of this feature.

## Technical Context

**Language/Version**: C# using the compiler/runtime profile supplied by Unity `6000.3.24f1`

**Primary Dependencies**: Universal Render Pipeline `17.3.0` with 2D Renderer, Input System `1.20.0`, uGUI `2.0.0`, Unity 2D packages, Unity Test Framework `1.6.0`; built-in `UnityEngine.Pool` for high-churn objects

**Storage**: Immutable authored `ScriptableObject` assets for game definitions; versioned JSON under `Application.persistentDataPath` for local campaign progress and audio settings; asset provenance records stored with project documentation/source material

**Testing**: Unity Test Framework `1.6.0` with NUnit/EditMode tests for deterministic rules and persistence; PlayMode tests for scene, UI, lifecycle, and presentation integration; Player smoke tests; a custom `ProfilerRecorder` performance harness in Development Builds

**Target Platform**: Windows 10/11 x64 desktop, landscape, mouse-first with keyboard shortcuts; baseline presentation at 1920×1080 with supported scaling down to 1280×720

**Project Type**: Single Unity desktop game project

**Performance Goals**: 60 FPS target at 1920×1080 in a representative battle; p95 total frame time at or below 16.67 ms and p99 at or below 25 ms on the reference machine; valid commands acknowledged within 250 ms; steady-state gameplay targets zero managed allocation per frame after warm-up

**Constraints**: Runtime must work offline; no account, telemetry, cloud save, multiplayer, advertisements, or in-game asset generation; 30 simultaneous enemies and 15 defenders must remain readable and responsive for a 30-minute soak; no unresolved-license asset enters a distributable build; no Spine runtime or custom Spine parser is added at any milestone

**Scale/Scope**: One reusable battle scene; 3 organ maps; 6 defender types; 4 enemy types; infection, mutation, Fever, progression, settings, and save recovery; 51 manifest assets total with 17 required for P0

## Constitution Check

*GATE: Evaluated before Phase 0 and re-checked after Phase 1 design.*

The repository constitution is still an unratified placeholder and contains no enforceable project principles, platform mandates, or quality gates. Planning therefore introduces no constitutional violation. The following feature-derived gates are applied without claiming constitutional authority:

- **Specification traceability**: PASS — each design area maps to FR/SC groups and the interface contracts.
- **Offline and privacy boundary**: PASS — runtime persistence is local and stores no account or personal data.
- **Asset release safety**: PASS — runtime content is allowlisted only after provenance, license, quality, and import checks.
- **Testability**: PASS — mutable rules are separated from presentation and have deterministic EditMode coverage; Unity lifecycle is covered in PlayMode/Player tests.
- **Scope control**: PASS — no multiplayer, backend, runtime AI, ECS, third-party DI/FSM framework, or Spine runtime is introduced.
- **Clarifications**: PASS — Phase 0 research resolved all technical choices; no `NEEDS CLARIFICATION` markers remain.

**Post-design re-check**: PASS. The data model, contracts, and quickstart preserve the same boundaries and add no gate violation.

## Project Structure

### Documentation (this feature)

```text
specs/001-immune-war-game/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── asset-provenance-contract.md
│   ├── asset-record.schema.json
│   ├── battle-interface-contract.md
│   ├── save-data.schema.json
│   └── ui-flow-contract.md
├── checklists/
│   └── requirements.md
└── tasks.md                       # created later by /speckit-tasks
```

### Source Code (repository root)

```text
Assets/ImmunWar/
├── Art/
│   ├── Defenders/
│   ├── Enemies/                   # approved runtime exports only
│   ├── Environment/
│   ├── UI/
│   └── VFX/
├── Audio/
│   ├── Mixers/
│   ├── Music/
│   └── SFX/
├── Data/
│   ├── Defenders/
│   ├── Enemies/
│   ├── Maps/
│   ├── StatusEffects/
│   └── Waves/
├── Input/
├── Materials/
├── Prefabs/
│   ├── Defenders/
│   ├── Enemies/
│   ├── Environment/
│   ├── UI/
│   └── VFX/
├── Scenes/
│   ├── Bootstrap.unity
│   ├── MainMenu.unity
│   ├── Battle.unity
│   └── Prototype.unity            # retained as a disposable reference scene
├── Scripts/
│   ├── Core/
│   ├── Battle/
│   ├── Combat/
│   ├── Economy/
│   ├── StatusEffects/
│   ├── Progression/
│   ├── Persistence/
│   ├── Presentation/
│   ├── UI/
│   ├── Audio/
│   └── Editor/
└── Tests/
    ├── EditMode/
    └── PlayMode/

AssetSource/                       # excluded from Player builds
├── Incoming/
├── Working/
├── ApprovedMasters/
│   └── LegacySpine/               # source/archive only; never a runtime path
└── RejectedArchive/

Docs/AssetProvenance/
├── records/                       # versioned machine-readable JSON records
└── evidence/                      # immutable source/license snapshots
```

**Structure Decision**: Keep a single Unity project and a single feature root at `Assets/ImmunWar`. The paths in `09_ASSET_MANIFEST.xlsx` that currently point to `Assets/_Game` are planning destinations, not a reason to create a parallel content tree; implementation must normalize them to the structure above and keep the manifest/provenance mapping by stable asset ID. Static definitions live in `Data`, mutable state lives in `Scripts`, scene-facing representations live in `Prefabs` and `Presentation`, and test assemblies mirror the runtime boundary.

## Architecture and Design Decisions

### Runtime composition

- `Bootstrap` loads validated save/settings data and enters the main menu.
- A single `GameSession` composition root created by `Bootstrap` survives scene loads with `DontDestroyOnLoad`. It owns the validated catalogs, selected map ID, save service, and committed settings; it is dependency-injected into scene entry points and is destroyed by explicit test reset or application shutdown. No mutable global static singleton is used.
- `MainMenu` handles continue/new game, map selection, settings, and quit.
- `Battle` is the only gameplay scene. It receives an `OrganMapConfig` ID and composes map visuals, routes, nodes, waves, HUD, pools, and audio.
- `BattleController` is the sole owner of battle-state transitions: `Preparing → Running ↔ Paused → Victory|Defeat → Restarting|Exited`.
- Services accept commands only when the current battle state allows them. Terminal transitions, costs, rewards, organ hits, and progress writes are idempotent.
- C# events connect rule state to presentation. Static global event buses and mutable singleton data are prohibited because they hide lifecycle and test dependencies.

### Authored data and runtime state

- `ScriptableObject` configs contain stable IDs, balance values, prefab/presentation references, and validation metadata.
- Config objects are treated as immutable during play. Each battle creates separate runtime instances for ATP, vitality, enemy health, route progress, node occupancy, effects, waves, Fever, and boss phases.
- Lists and serializable records are preferred over dictionaries in authored/persisted data; runtime lookup dictionaries may be built after validation.
- One stable string ID namespace links configs, saves, manifest rows, provenance records, UI, and tests.

### Paths, targeting, and combat

- Each map contains one or more ordered waypoint routes. Enemies advance by segment and normalized route progress; reaching the final waypoint is recorded exactly once.
- Blocking is an explicit enemy/defender interaction state, not a physics push simulation.
- Targeting filters eligibility first, then chooses a deterministic priority such as greatest route progress with stable ID tie-breaking.
- Enemy, projectile, and VFX instances use typed pools with complete reset on release. Defenders may be instantiated normally because placement churn is low.
- Authoritative rules advance on a fixed 30 Hz simulation tick; rendering interpolates independently. Commands are assigned a tick and monotonic sequence. Random variation uses an injected seeded source, and runtime instance IDs derive from deterministic placement/spawn sequences, so identical seed plus tick-stamped commands produces identical rule-state snapshots.

### Input, UI, audio, and feedback

- Input actions: `Point`, `PrimaryClick`, `Cancel`, `Pause`, `ActivateFever`, plus optional defender hotkeys.
- uGUI plus `InputSystemUIInputModule` owns runtime menus and HUD. The placement controller rejects world clicks over UI and owns select, preview, confirm, cancel, and rejection feedback.
- An `AudioMixer` exposes separate music and SFX controls. Core battle events map to approved visual/audio cues; missing nonessential presentation uses a safe placeholder without removing rules or controls.
- Canvas scaling and safe layout are verified at 1920×1080 and 1280×720. Critical HUD state remains visible at both.

### Persistence

- `SaveData` is a versioned DTO containing stable map IDs, completed/unlocked IDs, audio settings, and minimal metadata.
- Loading follows `parse version envelope → migrate supported legacy DTO → sanitize and validate current DTO → expose state`. The JSON Schema is a contract/test artifact, not a validator executed by `JsonUtility`; a dedicated runtime validator enforces catalog membership, uniqueness, subset, ordering, range, and timestamp rules.
- Save writes go to a temporary file, are read back and validated, then replace the primary file while retaining one last-known-good backup. When primary and backup are both valid, the highest `saveSequence` wins.
- Missing, corrupt, truncated, unsupported, or out-of-range data falls back to a validated default or supported migration without blocking startup.
- Save operations occur at committed progression checkpoints and a settings `Apply` event, never on slider preview, cancel, or every frame.

### Asset production and acceptance

- New external/generated files enter `AssetSource/Incoming` and cannot be referenced by runtime content. Reference inputs, source masters, license evidence, and tool/editor packages remain evidence-only even when approved.
- Working files retain the raw hash and provenance link. Only approved exports enter `Assets/ImmunWar`.
- Editor validation applies category-specific sprite/audio import rules and rejects runtime files without an approved record.
- Release validation reads versioned JSON records from `Docs/AssetProvenance/records`, verifies SHA-256 hashes, source-type-specific evidence, tri-state rights decisions, recheck dates, attribution/NOTICE needs, import profiles, missing references, and absence of quarantine/source files from the Player build.
- The existing Spine 3.7.94 source moves to `AssetSource/ApprovedMasters/LegacySpine` or an external archive. An approved static export may serve P0. If production animation is needed, an authorized compatible editor may bake required animations to PNG sequences; otherwise the asset is redrawn or replaced. No parser or legacy runtime is added.

## Verification Strategy

### EditMode

- ATP spend/gain and invalid placement invariants.
- Target selection, damage, death, rewards, and lost-projectile targets.
- Route arrival and simultaneous organ damage exactly once per enemy.
- Wave progression, pause/restart, terminal-state idempotence.
- Infection, cleanse, mutation, Fever, and boss phase boundary cases.
- Campaign unlocks and replay behavior.
- Save round-trip, 20 reload cycles, migrate-before-current-validation, invalid ranges, truncation, random bytes, primary-write interruption, backup selection by highest valid sequence, semantic map-order/subset validation, and both-files-corrupt fallback to lung-only defaults.
- Deterministic replay: identical seed plus command sequence yields identical state snapshots.

### PlayMode and Player

- Load the real battle scene with each map configuration and verify required references.
- Drive placement through UI, complete a short wave, and observe spawn, target, hit, death, reward, organ damage, pause, restart, victory, and defeat.
- Confirm only one result screen/event and no duplicated rewards/listeners after restart.
- Validate HUD scaling, input-over-UI command suppression, settings Apply/Cancel semantics, audio controls, and persistence across relaunch.
- Run a Player smoke flow from menu to lung battle and clean exit.

### Performance and release

- Reference machine: Windows 10/11 x64, Intel Core i5-8400-class 6-core CPU, GeForce GTX 1050 2 GB-class GPU, 8 GB RAM, Direct3D 11, Medium quality, 1920×1080. A formally changed minimum specification must update these gates and their recorded baseline.
- Custom `ProfilerRecorder` harness: disable VSync and frame cap, warm up for 10 seconds, then sample 7,200 frames with 30 enemies, 15 defenders, projectiles, VFX, HUD, and Fever active. Compute nearest-rank p95/p99 from total frame time and record CPU main-thread and GPU time separately; a missing GPU counter invalidates GPU acceptance rather than silently passing it.
- Run a 30-minute soak before release. Reject recurring gameplay spikes above 50 ms, command acknowledgement beyond 250 ms, pools that fail to return to their inter-wave baseline, or sustained managed-plus-native memory growth exceeding the greater of 32 MB or 5% of the post-warm-up baseline.
- Clean import/compile, EditMode, PlayMode, Player smoke, asset validator, Windows build, executable smoke, and log scan must all pass before distribution.

## Delivery Sequence

1. **Foundation**: assemblies, stable IDs, deterministic rule layer, battle state machine, input actions, persistence shell, validation utilities, and test harness.
2. **P0 vertical slice**: one lung configuration, waypoint route, nodes, ATP/vitality/waves, Macrophage/T-Cell/Energy Cell, Basic Virus, placement/HUD/results, required P0 VFX/UI/SFX, and 17 approved P0 asset records.
3. **Tactical systems**: infection/cleanse, mutation, Fever, B-Cell/NK Cell/Platelet, Bacteria/Mutant Virus, expanded feedback and balance tests.
4. **Campaign**: map selection, lung/brain/stomach configurations, progression persistence, Super Pathogen phases, full 51-item catalog integration.
5. **Hardening**: accessibility/readability review, save recovery, asset rights audit, performance regression/soak, clean Windows build, release smoke and notices.

## Risk Controls

- **Rights uncertainty**: quarantine and replace; do not ship while review is unresolved.
- **Generated-art inconsistency**: approve a master style sheet, generate in batches, and require manual cleanup/gameplay-scale review.
- **Legacy Spine incompatibility**: use an approved static or baked sprite fallback; no milestone may depend on a Spine runtime or custom parser.
- **State-boundary duplication**: central state owner, idempotent commands/events, and deterministic boundary tests.
- **High object churn**: typed pools with reset tests and steady-state allocation profiling.
- **Save incompatibility**: schema version, validation, migration, temp write, backup, and recovery tests.
- **Scope expansion**: one project, one battle scene, no backend, and no framework additions unless a later measured need changes the plan.
