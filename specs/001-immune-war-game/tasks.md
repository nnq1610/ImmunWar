---

description: "Dependency-ordered implementation tasks for the Immune War playable Unity game"
---

# Tasks: Immune War Playable Game

**Input**: Design documents from `/specs/001-immune-war-game/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Tests are required by the feature specification and verification strategy. Within each user story, create the listed tests first and confirm they fail for the intended reason before implementing the story.

**Organization**: Tasks are grouped by user story so each increment has a clear independent verification path. Asset-production tasks include both approved runtime output and provenance records; raw/generated sources never enter runtime folders before approval.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other `[P]` tasks in the same phase because it targets different files and has no unresolved dependency.
- **[Story]**: Maps the task to a user story from `spec.md`.
- Every task names the exact file or directory it changes.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the Unity project layout, packages, scenes, input, and governed asset workspace.

- [X] T001 Create the planned runtime folder structure under `Assets/ImmunWar/{Art,Audio,Data,Input,Materials,Prefabs,Scenes,Scripts,Tests}` with the subfolders defined in `specs/001-immune-war-game/plan.md` and preserve Unity `.meta` files.
- [X] T002 [P] Add runtime, editor, EditMode, and PlayMode assembly definitions at `Assets/ImmunWar/Scripts/ImmunWar.Runtime.asmdef`, `Assets/ImmunWar/Scripts/Editor/ImmunWar.Editor.asmdef`, `Assets/ImmunWar/Tests/EditMode/ImmunWar.EditModeTests.asmdef`, and `Assets/ImmunWar/Tests/PlayMode/ImmunWar.PlayModeTests.asmdef`.
- [X] T003 [P] Pin Unity 6000.3-compatible Input System, URP 2D, uGUI, and Test Framework dependencies in `Packages/manifest.json` and `Packages/packages-lock.json`.
- [X] T004 [P] Create gameplay, menu, and pause action maps with keyboard/mouse bindings in `Assets/ImmunWar/Input/ImmuneWar.inputactions`.
- [X] T005 Create scene shells and register them in build order at `Assets/ImmunWar/Scenes/Bootstrap.unity`, `Assets/ImmunWar/Scenes/MainMenu.unity`, `Assets/ImmunWar/Scenes/Battle.unity`, and `ProjectSettings/EditorBuildSettings.asset`.
- [X] T006 [P] Create governed source and evidence directories at `AssetSource/Incoming`, `AssetSource/Working`, `AssetSource/ApprovedMasters/LegacySpine`, `AssetSource/RejectedArchive`, `Docs/AssetProvenance/records`, and `Docs/AssetProvenance/evidence` with workflow guidance in `AssetSource/README.md`.
- [X] T007 Move legacy Spine source-only inputs out of runtime import scope into `AssetSource/ApprovedMasters/LegacySpine` and document any retained static sprite exports in `Docs/AssetProvenance/legacy-spine-migration.md`.
- [X] T008 [P] Map all 51 manifest IDs to planned source, approved runtime, prefab/config, and provenance paths in `Docs/AssetProvenance/manifest-path-map.json`.

**Checkpoint**: Unity opens without package or assembly errors; Bootstrap, MainMenu, and Battle scenes are registered; governed asset folders exist.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build deterministic simulation, authored-data, persistence, scene-flow, pooling, build, and asset-governance foundations used by every story.

**CRITICAL**: No user story implementation begins until this phase passes its EditMode tests.

### Foundation Tests

- [X] T009 [P] Add failing tests for stable IDs, 30 Hz tick advancement, command ordering, and seeded randomness in `Assets/ImmunWar/Tests/EditMode/CoreDeterminismTests.cs`.
- [X] T010 [P] Add failing tests for save migration, sanitization, primary/backup recovery, and highest `saveSequence` selection in `Assets/ImmunWar/Tests/EditMode/SaveDataTests.cs`.
- [X] T011 [P] Add failing tests for asset-record schema fields, SHA-256 matching, approval state, license evidence, and runtime-path allowlisting in `Assets/ImmunWar/Tests/EditMode/AssetProvenanceTests.cs`.

### Foundation Implementation

- [X] T012 [P] Implement immutable authored definitions in `Assets/ImmunWar/Scripts/Core/Config/DefenderConfig.cs`, `EnemyConfig.cs`, `OrganMapConfig.cs`, `RouteConfig.cs`, `DefenseNodeConfig.cs`, `WaveSet.cs`, `StatusEffectConfig.cs`, `FeverConfig.cs`, `AbilityConfig.cs`, `MutationDefinition.cs`, `BossPhaseConfig.cs`, and `PresentationConfig.cs`.
- [X] T013 [P] Implement mutable plain-C# runtime models in `Assets/ImmunWar/Scripts/Battle/State/BattleState.cs`, `EconomyState.cs`, `VitalityState.cs`, `DefenseNodeState.cs`, `DefenderState.cs`, `EnemyState.cs`, `WaveState.cs`, `FeverState.cs`, and `StatusEffectState.cs`.
- [X] T014 Implement stable identifiers, simulation sequence IDs, and the fixed-step clock in `Assets/ImmunWar/Scripts/Core/StableId.cs`, `SequenceId.cs`, and `FixedSimulationClock.cs` until T009 tick assertions pass.
- [X] T015 [P] Implement seeded random abstractions in `Assets/ImmunWar/Scripts/Core/IRandomSource.cs` and `SeededRandomSource.cs` until T009 replay assertions pass.
- [X] T016 [P] Implement tick-stamped command envelopes, battle commands, and domain events from the battle contract in `Assets/ImmunWar/Scripts/Battle/Commands/BattleCommand.cs` and `Assets/ImmunWar/Scripts/Battle/Events/BattleEvent.cs`.
- [X] T017 Implement config ID/reference validation and role-mask validation in `Assets/ImmunWar/Scripts/Core/Config/ConfigValidator.cs` and `Assets/ImmunWar/Scripts/Editor/ConfigValidationMenu.cs`.
- [X] T018 Implement the `DontDestroyOnLoad` composition root without a mutable global static singleton in `Assets/ImmunWar/Scripts/Core/GameSession.cs` and `Assets/ImmunWar/Scripts/Core/BootstrapController.cs`.
- [X] T019 [P] Implement current save DTOs, version envelope, migrations, and sanitization in `Assets/ImmunWar/Scripts/Persistence/SaveData.cs`, `SaveMigration.cs`, and `SaveDataValidator.cs` until T010 schema assertions pass.
- [X] T020 Implement atomic temp/primary/backup JSON persistence and settings persistence-on-Apply in `Assets/ImmunWar/Scripts/Persistence/SaveRepository.cs` and `SettingsRepository.cs` until all T010 recovery assertions pass.
- [X] T021 Implement typed config catalogs and startup loading in `Assets/ImmunWar/Scripts/Core/Config/GameCatalog.cs` and `CatalogLoader.cs`.
- [X] T022 [P] Add reusable enemy, projectile, and VFX pool adapters using `UnityEngine.Pool` in `Assets/ImmunWar/Scripts/Presentation/ComponentPool.cs` and `PoolRegistry.cs`.
- [X] T023 Implement asset record DTO/schema validation, import defaults, approval labels, and build-time rejection of unapproved runtime assets in `Assets/ImmunWar/Scripts/Editor/AssetProvenance/AssetRecord.cs`, `AssetRecordValidator.cs`, `ImmuneWarAssetPostprocessor.cs`, and `ApprovedAssetBuildGuard.cs` until T011 passes.
- [X] T024 [P] Implement asynchronous Bootstrap/MainMenu/Battle transitions and loading error handling in `Assets/ImmunWar/Scripts/Core/SceneFlowService.cs` and `Assets/ImmunWar/Scripts/UI/LoadingScreenController.cs`.
- [X] T025 [P] Add Windows development/release build entry points and catalog validation gates in `Assets/ImmunWar/Scripts/Editor/BuildCommands.cs`.
- [X] T026 [P] Add `ProfilerRecorder` frame-time, allocation, and memory capture infrastructure in `Assets/ImmunWar/Tests/PlayMode/Performance/PerformanceProbe.cs` and `Assets/ImmunWar/Scripts/Editor/PerformanceTestCommands.cs`.

**Checkpoint**: Foundation EditMode tests pass; invalid configs/assets stop validation; a clean Bootstrap scene can create one session and transition scenes.

---

## Phase 3: User Story 1 - Defend an Organ in a Complete Battle (Priority: P1) MVP

**Goal**: Deliver one complete lung battle using Macrophage, T-Cell, Energy Cell, Basic Virus, ATP, vessel routes, vitality, wave flow, pause, victory/defeat, and restart.

**Independent Test**: From a fresh save, launch the lung map, place all three defender roles, start waves, observe movement/attacks/ATP/vitality, reach victory or defeat, pause/resume, and restart without stale state.

### Tests for User Story 1

> Write these tests first and confirm they fail because the P0 battle behavior is not yet implemented.

- [X] T027 [P] [US1] Add placement-node, role-mask, ATP affordability, and Energy Cell income tests in `Assets/ImmunWar/Tests/EditMode/PlacementEconomyTests.cs`.
- [X] T028 [P] [US1] Add waypoint movement, blocking, spawn order, wave completion, and route-exit damage tests in `Assets/ImmunWar/Tests/EditMode/RouteWaveTests.cs`.
- [X] T029 [P] [US1] Add deterministic target selection, cooldown, damage, death, and pooled-projectile result tests in `Assets/ImmunWar/Tests/EditMode/CombatResolutionTests.cs`.
- [X] T030 [P] [US1] Add exactly-once command/event and invalid-state rejection tests from `battle-interface-contract.md` in `Assets/ImmunWar/Tests/EditMode/BattleCommandContractTests.cs`.
- [X] T031 [P] [US1] Add a full fresh-save lung victory/defeat PlayMode journey in `Assets/ImmunWar/Tests/PlayMode/LungBattleJourneyTests.cs`.
- [X] T032 [P] [US1] Add pause, resume, restart, and stale-runtime-state PlayMode tests in `Assets/ImmunWar/Tests/PlayMode/PauseRestartTests.cs`.
- [X] T033 [P] [US1] Add P0 HUD ATP/vitality/wave/result contract tests in `Assets/ImmunWar/Tests/PlayMode/BattleHudContractTests.cs`.

### Implementation for User Story 1

- [X] T034 [P] [US1] Produce/import approved static sprite sheets for `DEF-001`, `DEF-002`, `DEF-006`, and `ENE-001` under `Assets/ImmunWar/Art/Defenders/P0` and `Assets/ImmunWar/Art/Enemies/P0`, retaining raw sources in `AssetSource/Working/P0Units` and records in `Docs/AssetProvenance/records/P0Units`.
- [X] T035 [P] [US1] Produce/import approved lung vessel tiles and defense node art for `ENV-001` through `ENV-004` under `Assets/ImmunWar/Art/Environment/Lung`, with raw sources in `AssetSource/Working/LungEnvironment` and records in `Docs/AssetProvenance/records/LungEnvironment`.
- [X] T036 [P] [US1] Produce/import approved P0 combat/ATP effects and HUD icons for `VFX-001`, `VFX-003`, `VFX-007`, `UI-001`, `UI-002`, `UI-006`, and `UI-007` under `Assets/ImmunWar/Art/VFX/P0` and `Assets/ImmunWar/Art/UI/P0`, with matching records under `Docs/AssetProvenance/records/P0Feedback`.
- [X] T037 [P] [US1] Produce/import approved T-Cell attack and virus-death SFX for `AUD-005` and `AUD-006` under `Assets/ImmunWar/Audio/SFX/P0`, with raw masters in `AssetSource/Working/P0Audio` and records in `Docs/AssetProvenance/records/P0Audio`.
- [X] T038 [US1] Create sprite atlases, pivots, pixels-per-unit, animation clips, and animator controllers for P0 unit/environment art in `Assets/ImmunWar/Art/Atlases/P0.spriteatlas`, `Assets/ImmunWar/Art/Animations/Defenders`, and `Assets/ImmunWar/Art/Animations/Enemies`.
- [X] T039 [P] [US1] Author Macrophage, T-Cell, Energy Cell, and Basic Virus config assets in `Assets/ImmunWar/Data/Defenders/P0` and `Assets/ImmunWar/Data/Enemies/P0`.
- [X] T040 [P] [US1] Author the lung route, defense nodes, starting ATP/vitality, and P0 wave set in `Assets/ImmunWar/Data/Maps/LungMap.asset`, `Assets/ImmunWar/Data/Maps/LungRoute.asset`, and `Assets/ImmunWar/Data/Waves/LungP0Waves.asset`.
- [X] T041 [P] [US1] Create P0 defender, enemy, projectile, and hit-effect prefabs in `Assets/ImmunWar/Prefabs/Defenders/P0`, `Assets/ImmunWar/Prefabs/Enemies/P0`, and `Assets/ImmunWar/Prefabs/VFX/P0`.
- [X] T042 [US1] Implement node selection, placement preview, role validation, purchase, cancellation, and removal cleanup in `Assets/ImmunWar/Scripts/Battle/Placement/PlacementSystem.cs` and `Assets/ImmunWar/Scripts/UI/PlacementController.cs` until T027 placement cases pass.
- [X] T043 [P] [US1] Implement ATP spending, Energy Cell income ticks, and change events in `Assets/ImmunWar/Scripts/Economy/AtpEconomySystem.cs` until T027 economy cases pass.
- [X] T044 [US1] Implement route interpolation, deterministic spawn order, blocker engagement, exit handling, and wave state changes in `Assets/ImmunWar/Scripts/Battle/Waves/WaveSystem.cs`, `Assets/ImmunWar/Scripts/Battle/Movement/RouteFollower.cs`, and `Assets/ImmunWar/Scripts/Battle/Blocking/BlockSystem.cs` until T028 passes.
- [X] T045 [US1] Implement deterministic defender targeting, attack cooldowns, projectile resolution, health, death, and reward emission in `Assets/ImmunWar/Scripts/Combat/TargetingSystem.cs`, `CombatSystem.cs`, and `DamageResolver.cs` until T029 passes.
- [X] T046 [US1] Implement ordered command dispatch, exactly-once event publishing, battle lifecycle transitions, and organ-vitality resolution in `Assets/ImmunWar/Scripts/Battle/BattleController.cs` and `Assets/ImmunWar/Scripts/Battle/BattleCommandProcessor.cs` until T030 passes.
- [X] T047 [P] [US1] Bind pooled runtime state to defender/enemy/projectile/VFX views in `Assets/ImmunWar/Scripts/Presentation/BattleEntityPresenter.cs` and `Assets/ImmunWar/Scripts/Presentation/BattleVfxPresenter.cs`.
- [X] T048 [US1] Compose camera, route, defense nodes, pools, systems, and lung data through `Assets/ImmunWar/Scripts/Battle/BattleSceneInstaller.cs` and `Assets/ImmunWar/Scenes/Battle.unity`.
- [X] T049 [P] [US1] Implement ATP, vitality, wave, selection, start-wave, pause, and result UI controllers/prefabs in `Assets/ImmunWar/Scripts/UI/BattleHudController.cs`, `Assets/ImmunWar/Scripts/UI/PauseMenuController.cs`, `Assets/ImmunWar/Scripts/UI/BattleResultController.cs`, and `Assets/ImmunWar/Prefabs/UI/P0BattleHud.prefab`.
- [X] T050 [P] [US1] Route P0 animation, VFX, and SFX from domain events in `Assets/ImmunWar/Scripts/Presentation/P0BattleFeedback.cs` and `Assets/ImmunWar/Scripts/Audio/SfxService.cs`.
- [X] T051 [US1] Run T027-T033 plus the standalone lung battle flow and record passing evidence in `specs/001-immune-war-game/evidence/us1-mvp-verification.md`.

**Checkpoint**: US1 is a shippable offline MVP and can be demonstrated without US2-US4 content.

---

## Phase 4: User Story 2 - Make Tactical Immune-System Choices (Priority: P2)

**Goal**: Add distinct defender roles, infection/cleanse, mutation, and Fever Mode so tactical choices materially change battle outcomes.

**Independent Test**: In a controlled lung scenario, use B-Cell, NK Cell, and Platelet roles; allow infection and cleanse it; trigger a deterministic mutation; charge and activate Fever Mode; verify each mechanic changes the expected targets/stats without breaking US1.

### Tests for User Story 2

- [X] T052 [P] [US2] Add B-Cell support, NK Cell burst, and Platelet repair/control behavior tests in `Assets/ImmunWar/Tests/EditMode/ExpandedDefenderRoleTests.cs`.
- [X] T053 [P] [US2] Add infection application, stacking/refresh, cleanse, expiration, and event tests in `Assets/ImmunWar/Tests/EditMode/InfectionStatusTests.cs`.
- [X] T054 [P] [US2] Add seeded mutation selection, stat changes, immunity rules, and replay tests in `Assets/ImmunWar/Tests/EditMode/MutationSystemTests.cs`.
- [X] T055 [P] [US2] Add Fever charge, activation threshold, duration, modifiers, and reactivation tests in `Assets/ImmunWar/Tests/EditMode/FeverModeTests.cs`.
- [X] T056 [P] [US2] Add a tactical-mechanics PlayMode journey covering infection, cleanse, mutation, and Fever Mode in `Assets/ImmunWar/Tests/PlayMode/TacticalBattleJourneyTests.cs`.

### Implementation for User Story 2

- [X] T057 [P] [US2] Produce/import approved art for `DEF-003` through `DEF-005`, `ENE-002`, and `ENE-003` under `Assets/ImmunWar/Art/Defenders/P1` and `Assets/ImmunWar/Art/Enemies/P1`, with raw sources in `AssetSource/Working/TacticalUnits` and records in `Docs/AssetProvenance/records/TacticalUnits`.
- [X] T058 [P] [US2] Produce/import approved tactical VFX, status icons, defender icons, and associated SFX under `Assets/ImmunWar/Art/VFX/Tactical`, `Assets/ImmunWar/Art/UI/Tactical`, and `Assets/ImmunWar/Audio/SFX/Tactical`, with manifest-ID records in `Docs/AssetProvenance/records/TacticalFeedback`.
- [X] T059 [P] [US2] Author B-Cell, NK Cell, Platelet, Bacteria, and Mutant configs/prefabs in `Assets/ImmunWar/Data/Defenders/P1`, `Assets/ImmunWar/Data/Enemies/P1`, `Assets/ImmunWar/Prefabs/Defenders/P1`, and `Assets/ImmunWar/Prefabs/Enemies/P1`.
- [X] T060 [P] [US2] Author infection, cleanse, mutation, Fever Mode, and tactical presentation configs in `Assets/ImmunWar/Data/StatusEffects`, `Assets/ImmunWar/Data/Fever`, `Assets/ImmunWar/Data/Mutations`, and `Assets/ImmunWar/Data/Presentation/TacticalPresentation.asset`.
- [X] T061 [US2] Implement deterministic status application, refresh/stack policy, ticking, expiration, and cleanse in `Assets/ImmunWar/Scripts/StatusEffects/StatusEffectSystem.cs` until T053 passes.
- [X] T062 [P] [US2] Implement infection-specific targeting/stat effects and infection events in `Assets/ImmunWar/Scripts/StatusEffects/InfectionSystem.cs`.
- [X] T063 [US2] Implement seeded mutation eligibility, selection, application, and presentation events in `Assets/ImmunWar/Scripts/Combat/MutationSystem.cs` until T054 passes.
- [X] T064 [US2] Implement Fever charge, activation command, timed modifiers, cooldown, and cleanup in `Assets/ImmunWar/Scripts/Battle/Fever/FeverSystem.cs` until T055 passes.
- [X] T065 [US2] Implement B-Cell support/cleanse, NK burst, and Platelet repair/control abilities in `Assets/ImmunWar/Scripts/Combat/Abilities/BCellAbility.cs`, `NkCellAbility.cs`, and `PlateletAbility.cs` until T052 passes.
- [X] T066 [P] [US2] Add Fever meter/button, infection/mutation badges, tooltips, and tactical feedback bindings in `Assets/ImmunWar/Scripts/UI/TacticalHudController.cs` and `Assets/ImmunWar/Prefabs/UI/TacticalHudExtension.prefab`.
- [X] T067 [US2] Integrate tactical systems into `Assets/ImmunWar/Scripts/Battle/BattleSceneInstaller.cs`, run T052-T056 plus US1 regression tests, and record evidence in `specs/001-immune-war-game/evidence/us2-tactics-verification.md`.

**Checkpoint**: US2 mechanics are deterministic, readable, and independently testable in a controlled battle fixture; all US1 tests still pass.

---

## Phase 5: User Story 3 - Progress Through Organs and Defeat a Boss (Priority: P3)

**Goal**: Add lung/brain/stomach campaign progression, safe persistence, map unlocks, and a multi-phase Super Pathogen boss.

**Independent Test**: Complete the lung map, return to map select, verify the next organ unlocks, reload the app and confirm progress persists, then run a boss fixture through every phase and complete the campaign.

### Tests for User Story 3

- [X] T068 [P] [US3] Add map completion, unlock ordering, idempotent rewards, and campaign-finished tests in `Assets/ImmunWar/Tests/EditMode/CampaignProgressionTests.cs`.
- [X] T069 [P] [US3] Add save/reload and corrupted-save recovery cases for campaign progress in `Assets/ImmunWar/Tests/EditMode/CampaignSaveIntegrationTests.cs`.
- [X] T070 [P] [US3] Add brain/stomach route, node, wave-reference, and stable-ID validation tests in `Assets/ImmunWar/Tests/EditMode/OrganMapConfigTests.cs`.
- [X] T071 [P] [US3] Add Super Pathogen threshold, ordered phase transition, ability, and death tests in `Assets/ImmunWar/Tests/EditMode/BossPhaseTests.cs`.
- [X] T072 [P] [US3] Add map-select-to-battle-to-results-to-reload campaign PlayMode tests in `Assets/ImmunWar/Tests/PlayMode/CampaignJourneyTests.cs`.

### Implementation for User Story 3

- [X] T073 [P] [US3] Produce/import approved brain and stomach environment tiles/landmarks under `Assets/ImmunWar/Art/Environment/Brain` and `Assets/ImmunWar/Art/Environment/Stomach`, with sources in `AssetSource/Working/OrganMaps` and records in `Docs/AssetProvenance/records/OrganMaps`.
- [X] T074 [P] [US3] Produce/import approved Super Pathogen phase art, effects, and boss audio under `Assets/ImmunWar/Art/Enemies/Boss`, `Assets/ImmunWar/Art/VFX/Boss`, and `Assets/ImmunWar/Audio/SFX/Boss`, with sources in `AssetSource/Working/SuperPathogen` and records in `Docs/AssetProvenance/records/SuperPathogen`.
- [X] T075 [P] [US3] Author brain/stomach route, node, wave, and map assets in `Assets/ImmunWar/Data/Maps/BrainMap.asset`, `BrainRoute.asset`, `StomachMap.asset`, `StomachRoute.asset`, and `Assets/ImmunWar/Data/Waves/CampaignWaves.asset` until T070 passes.
- [X] T076 [P] [US3] Author Super Pathogen enemy/phase/ability assets and prefab in `Assets/ImmunWar/Data/Enemies/SuperPathogen.asset`, `Assets/ImmunWar/Data/Bosses/SuperPathogenPhases.asset`, and `Assets/ImmunWar/Prefabs/Enemies/Boss/SuperPathogen.prefab`.
- [X] T077 [US3] Implement idempotent map completion, ordered unlocks, selected-map validation, and campaign completion in `Assets/ImmunWar/Scripts/Progression/CampaignProgressionService.cs` until T068 passes.
- [X] T078 [US3] Integrate campaign state with save migration/recovery and save after result confirmation in `Assets/ImmunWar/Scripts/Persistence/CampaignSaveCoordinator.cs` until T069 passes.
- [X] T079 [P] [US3] Implement map cards, lock/completion states, selection, and launch flow in `Assets/ImmunWar/Scripts/UI/MapSelectController.cs` and `Assets/ImmunWar/Prefabs/UI/MapSelectScreen.prefab`.
- [X] T080 [US3] Implement health-threshold boss phases, phase abilities, transition invulnerability, and boss events in `Assets/ImmunWar/Scripts/Combat/Boss/BossPhaseController.cs` until T071 passes.
- [X] T081 [P] [US3] Bind organ-specific tiles/landmarks/routes and boss phase presentation in `Assets/ImmunWar/Scripts/Presentation/OrganMapPresenter.cs` and `Assets/ImmunWar/Scripts/Presentation/BossPresenter.cs`.
- [X] T082 [US3] Connect Main Menu, Map Select, Battle, and Results transitions through `Assets/ImmunWar/Scripts/UI/MainMenuController.cs`, `Assets/ImmunWar/Scripts/UI/BattleResultController.cs`, and `Assets/ImmunWar/Scenes/MainMenu.unity`.
- [X] T083 [US3] Run T068-T072 plus US1/US2 regression suites and record three-organ/boss persistence evidence in `specs/001-immune-war-game/evidence/us3-campaign-verification.md`.

**Checkpoint**: The complete offline campaign unlocks and persists correctly, and the boss finishes deterministically across save/reload cycles.

---

## Phase 6: User Story 4 - Understand the Battle Through Feedback (Priority: P4)

**Goal**: Make state, threats, actions, results, and settings understandable through coherent visual/audio feedback at supported resolutions.

**Independent Test**: Run a battle at 1280x720, 1920x1080, and 2560x1440; identify ATP/vitality/wave/status/boss states without opening debug UI; independently adjust music/SFX, verify Apply/Cancel persistence, and confirm critical information is never color-only.

### Tests for User Story 4

- [X] T084 [P] [US4] Add one-to-one battle-event-to-feedback mapping and duplicate suppression tests in `Assets/ImmunWar/Tests/EditMode/PresentationEventRouterTests.cs`.
- [X] T085 [P] [US4] Add HUD safe-area, overlap, readable-scale, and input tests for the three target resolutions in `Assets/ImmunWar/Tests/PlayMode/ResponsiveHudTests.cs`.
- [X] T086 [P] [US4] Add music/SFX independence and settings Apply/Cancel persistence tests in `Assets/ImmunWar/Tests/PlayMode/AudioSettingsTests.cs`.
- [X] T087 [P] [US4] Add manifest coverage tests proving every one of the 51 asset IDs has an approved runtime mapping and valid record in `Assets/ImmunWar/Tests/EditMode/AssetManifestCoverageTests.cs`.

### Implementation for User Story 4

- [X] T088 [P] [US4] Produce/import remaining approved UI, background, and optional decorative manifest assets under `Assets/ImmunWar/Art/UI/Final` and `Assets/ImmunWar/Art/Environment/Final`, with sources in `AssetSource/Working/FinalPresentation` and records in `Docs/AssetProvenance/records/FinalPresentation` until the visual portion of T087 passes.
- [X] T089 [P] [US4] Produce/import distinct battle/menu music and remaining approved feedback SFX under `Assets/ImmunWar/Audio/Music` and `Assets/ImmunWar/Audio/SFX/Final`, with sources in `AssetSource/Working/FinalAudio` and records in `Docs/AssetProvenance/records/FinalAudio` until the audio portion of T087 passes.
- [X] T090 [P] [US4] Author event-to-animation/VFX/SFX/camera feedback mappings in `Assets/ImmunWar/Data/Presentation/BattlePresentation.asset` and `Assets/ImmunWar/Data/Presentation/BossPresentation.asset`.
- [X] T091 [US4] Implement exactly-once presentation routing, duplicate suppression, and pooled feedback dispatch in `Assets/ImmunWar/Scripts/Presentation/PresentationEventRouter.cs` until T084 passes.
- [X] T092 [P] [US4] Configure music, SFX, and master mixer groups/snapshots in `Assets/ImmunWar/Audio/Mixers/ImmuneWarAudio.mixer` and implement playback/fades in `Assets/ImmunWar/Scripts/Audio/AudioService.cs`.
- [X] T093 [US4] Implement settings preview, Apply, Cancel, persisted music/SFX sliders, and pause-menu integration in `Assets/ImmunWar/Scripts/UI/SettingsController.cs` and `Assets/ImmunWar/Prefabs/UI/SettingsPanel.prefab` until T086 passes.
- [X] T094 [P] [US4] Implement Canvas Scaler, safe-area handling, anchored HUD groups, tooltip bounds, and controller/keyboard focus in `Assets/ImmunWar/Scripts/UI/SafeAreaController.cs` and `Assets/ImmunWar/Prefabs/UI/BattleHud.prefab` until T085 passes.
- [X] T095 [P] [US4] Add shape/icon/text companions for infection, mutation, Fever, boss phase, affordability, victory, and defeat cues in `Assets/ImmunWar/Scripts/UI/BattleAccessibilityPresenter.cs` and `Assets/ImmunWar/Prefabs/UI/StatusIndicator.prefab`.
- [X] T096 [P] [US4] Add concise mechanics/help copy and first-battle guidance in `Assets/ImmunWar/Data/Localization/en-US.json`, `Assets/ImmunWar/Data/Localization/vi-VN.json`, and `Assets/ImmunWar/Scripts/UI/TutorialPromptController.cs`.
- [X] T097 [US4] Integrate final presentation configs, audio, responsive HUD, and accessibility presenters into `Assets/ImmunWar/Scenes/MainMenu.unity` and `Assets/ImmunWar/Scenes/Battle.unity`.
- [X] T098 [US4] Run T084-T087 plus all prior regression suites and record multi-resolution/readability/audio evidence in `specs/001-immune-war-game/evidence/us4-feedback-verification.md`.

**Checkpoint**: All critical battle information has coherent visual and audio feedback, supported layouts pass, and all 51 manifest assets are approved and traceable.

---

## Phase 7: Polish & Cross-Cutting Release Readiness

**Purpose**: Validate performance, stability, content safety, licenses, executable behavior, and reproducible release commands across all stories.

- [X] T099 [P] Remove per-tick allocations and tune enemy/projectile/VFX pool capacities using profiler evidence in `Assets/ImmunWar/Scripts/Battle/BattleController.cs` and `Assets/ImmunWar/Scripts/Presentation/PoolRegistry.cs`.
- [X] T100 [P] Add the specified 30-enemy/15-defender, 10-second warmup, 7,200-frame performance regression scenario in `Assets/ImmunWar/Tests/PlayMode/Performance/BattlePerformanceTests.cs`.
- [X] T101 [P] Add repeated restart and campaign-transition memory soak assertions in `Assets/ImmunWar/Tests/PlayMode/Performance/MemorySoakTests.cs`.
- [X] T102 [P] Add packaged-player launch, menu, lung battle, pause, restart, and exit smoke automation in `Assets/ImmunWar/Tests/Player/WindowsPlayerSmokeTests.cs`.
- [X] T103 Add EditMode, PlayMode, provenance, performance, and scene/catalog validation gates to release builds in `Assets/ImmunWar/Scripts/Editor/BuildCommands.cs`.
- [X] T104 [P] Generate third-party attribution from approved asset records into `Assets/ImmunWar/StreamingAssets/THIRD_PARTY_NOTICES.txt` using `Assets/ImmunWar/Scripts/Editor/AssetProvenance/ThirdPartyNoticeGenerator.cs`.
- [X] T105 [P] Review educational/medical wording for unsupported claims and document approved copy changes in `Docs/ContentReview/medical-content-review.md`.
- [X] T106 Run the full 51-item provenance/license/hash audit and save the validator report to `specs/001-immune-war-game/evidence/asset-audit-report.json`.
- [X] T107 Run all EditMode and PlayMode commands from `specs/001-immune-war-game/quickstart.md` and save JUnit outputs under `TestResults/EditMode-results.xml` and `TestResults/PlayMode-results.xml`.
- [X] T108 Run the performance and memory scenarios on the reference Windows machine and record p50/p95/p99, allocations, and soak results in `specs/001-immune-war-game/evidence/performance-report.md`.
- [X] T109 Build the release player into `Builds/Windows/ImmuneWar.exe`, execute T102, and record packaged-player results in `specs/001-immune-war-game/evidence/windows-smoke-report.md`.
- [X] T110 Update developer/release instructions and known limitations from actual verification results in `README.md` and `specs/001-immune-war-game/quickstart.md`.

**Final Checkpoint**: Tests, asset audit, performance thresholds, clean Windows build, and packaged-player smoke all pass with evidence committed.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 — Setup**: Starts immediately. T005 depends on T001; T007 depends on T006; otherwise marked setup tasks can proceed in parallel.
- **Phase 2 — Foundational**: Depends on Phase 1 and blocks every user story. Write T009-T011 first; implementations T012-T026 then make them pass.
- **Phase 3 — US1 (MVP)**: Depends on Phase 2. It establishes the playable battle shell used by later increments.
- **Phase 4 — US2**: Depends on the US1 battle shell and Phase 2 contracts; its controlled tests remain independently runnable.
- **Phase 5 — US3**: Depends on US1 battle/result flow. Boss tactical effects may reuse US2 systems, while progression/save tests can start after Phase 2.
- **Phase 6 — US4**: Depends on at least US1 events/HUD; final manifest coverage and full feedback integration depend on the desired US1-US3 content scope.
- **Phase 7 — Polish**: Depends on all stories selected for release.

### User Story Dependency Graph

```text
Setup -> Foundation -> US1 MVP -> US2 Tactical
                         |          |
                         +-------> US3 Campaign/Boss
                         |
                         +-------> US4 Feedback
US2 + US3 + US4 ----------------> Release Readiness
```

### Within Each User Story

1. Create all listed tests and confirm they fail for the expected missing behavior.
2. Approve source provenance before copying generated/downloaded output into `Assets/ImmunWar`.
3. Author config assets and prefabs before wiring systems into `BattleSceneInstaller`.
4. Implement deterministic domain rules before presentation/UI bindings.
5. Run the story's independent test plus all completed-story regressions before its checkpoint.

### Parallel Opportunities

- T002-T004, T006, and T008 can run concurrently after T001 where their parent folders are required.
- T009-T011 can run concurrently; T012, T013, T015, T016, T019, T022, and T024-T026 target separate foundations and can run concurrently subject to their test contracts.
- Within US1, T027-T033 can run concurrently; T034-T037 can run concurrently; T039-T041 can run concurrently after approved imports.
- Within US2, T052-T056 can run concurrently; T057-T060 can run concurrently; T062 and T066 can proceed alongside independent core-system work.
- Within US3, T068-T072 can run concurrently; T073-T076 can run concurrently; T079 and T081 can proceed after their data/art inputs exist.
- Within US4, T084-T087 can run concurrently; T088-T090 and T092 can run concurrently; T094-T096 target separate files.
- Once US1 is stable, progression/save work from US3 and presentation work from US4 can proceed in parallel with US2, with final integration deferred to each story checkpoint.

---

## Parallel Examples

### User Story 1

```text
Parallel test batch: T027, T028, T029, T030, T031, T032, T033
Parallel asset batch: T034, T035, T036, T037
Parallel authored-content batch after approval: T039, T040, T041
```

### User Story 2

```text
Parallel test batch: T052, T053, T054, T055, T056
Parallel content batch: T057, T058, T059, T060
```

### User Story 3

```text
Parallel test batch: T068, T069, T070, T071, T072
Parallel content batch: T073, T074, T075, T076
```

### User Story 4

```text
Parallel test batch: T084, T085, T086, T087
Parallel presentation batch: T088, T089, T090, T092
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete T027-T051 for US1 only.
3. Stop and run the independent fresh-save lung battle test.
4. Produce a Windows development build for stakeholder playtesting before expanding scope.

### Incremental Delivery

1. **US1**: Complete lung battle loop and verify the MVP.
2. **US2**: Add tactical roles/status/Fever; rerun US1 regression.
3. **US3**: Add campaign maps/persistence/boss; rerun US1-US2 regression.
4. **US4**: Complete feedback/accessibility/assets; rerun all story suites.
5. **Release**: Complete performance, provenance, packaged-player, and documentation gates.

### Suggested Ownership Boundaries

- **Simulation**: `Assets/ImmunWar/Scripts/{Battle,Combat,Economy,StatusEffects}` and related EditMode tests.
- **Content/Assets**: `AssetSource`, `Assets/ImmunWar/{Art,Audio,Data,Prefabs}`, and `Docs/AssetProvenance`.
- **Presentation/UI**: `Assets/ImmunWar/Scripts/{Presentation,UI,Audio}` and PlayMode UI tests.
- **Platform/Quality**: `Assets/ImmunWar/Scripts/{Core,Persistence,Editor}`, build commands, performance tests, and release evidence.

---

## Notes

- `[P]` means the task is safe to execute concurrently only after its stated phase dependencies are satisfied.
- No generated or downloaded asset may ship without source URL/tool metadata, license terms, evidence, SHA-256, review state, and manifest ID.
- Runtime Unity folders contain approved static exports only; Spine source/project files stay under `AssetSource`.
- Simulation behavior must depend on tick/state/config, never animation timing or frame rate.
- Prefer one commit per task or tightly related task group, and attach test/audit evidence at every story checkpoint.
