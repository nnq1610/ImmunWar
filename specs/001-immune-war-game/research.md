# Phase 0 Research: Immune War Playable Game

**Date**: 2026-09-18

All technical unknowns identified from the feature specification and local project were resolved in this phase. The project already contains Unity `6000.3.24f1`, URP 2D, Input System, uGUI, 2D packages, and Unity Test Framework, so the plan avoids new runtime frameworks.

## 1. Authored data model

**Decision**: Use immutable `ScriptableObject` assets for defender, enemy, wave, map, status-effect, Fever, and presentation definitions. Create separate mutable runtime objects for every battle.

**Rationale**: Authored assets provide Inspector references and reuse without duplicating prefab data. Unity supports reading ScriptableObject assets in deployed builds but does not position them as a runtime save mechanism. Serializable lists and stable IDs also respect Unity serialization limits.

**Alternatives considered**: JSON/CSV as primary authored data was rejected because prefab and asset references become fragile. Hard-coded constants were rejected because three maps, waves, and six defenders require balancing without code changes.

**Sources**: [ScriptableObject manual](https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html), [serialization rules](https://docs.unity3d.com/6000.0/Documentation/Manual/script-serialization-rules.html)

## 2. Battle lifecycle

**Decision**: Give one `BattleController` exclusive authority over explicit states: `Preparing`, `Running`, `Paused`, `Victory`, `Defeat`, `Restarting`, and `Exited`. Gate commands/spawners by state and make terminal transitions idempotent.

**Rationale**: Central ownership prevents duplicate costs, rewards, spawns, and result screens at pause/end boundaries. Setting time scale to zero helps pause scaled gameplay but does not replace explicit command and transition guards.

**Alternatives considered**: Independent booleans permit contradictory states. Animator graphs obscure rule ownership. A third-party FSM is unnecessary for this scope.

**Source**: [Time.timeScale](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-timeScale.html)

### Scene/session ownership

**Decision**: Bootstrap creates one dependency-composition `GameSession` object retained through `DontDestroyOnLoad`. It owns catalogs, persistence, committed settings, and the validated selected-map payload, then injects scene entry points. Tests and shutdown explicitly dispose it.

**Rationale**: Main Menu and Battle need shared save/session state across single-mode scene loads, while a composition root avoids mutable global static singletons and makes teardown verifiable.

**Alternatives considered**: Static service locators hide lifecycle and test state. Duplicating save/catalog services in every scene risks divergent state. An always-loaded additive Bootstrap scene is viable but adds scene orchestration not required for this small project.

**Source**: [Object.DontDestroyOnLoad](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.DontDestroyOnLoad.html)

## 3. Route movement and targeting

**Decision**: Author ordered waypoint routes. Run authoritative route/combat rules on a fixed 30 Hz simulation tick, track each enemy's route ID, segment, and normalized progress, and interpolate Unity presentation between committed ticks. Use explicit blocking state and deterministic target priorities.

**Rationale**: Fixed lane paths satisfy the design, are inspectable, and make arrival exactly-once semantics straightforward. A fixed tick plus tick-stamped commands, a seeded random source, and sequence-derived instance IDs makes deterministic snapshot testing meaningful. Normalized progress supports targeting and preserves continuity through mutation/boss changes.

**Alternatives considered**: NavMesh and Rigidbody2D movement add nondeterminism and complexity. Splines remain an optional presentation enhancement, not an MVP requirement.

**Sources**: [Vector3.MoveTowards](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Vector3.MoveTowards.html), [Time.deltaTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-deltaTime.html)

## 4. Pooling

**Decision**: Use typed `UnityEngine.Pool.ObjectPool<T>` instances for enemies, projectiles, and VFX with complete reset callbacks and prewarming from wave concurrency. Instantiate defenders normally.

**Rationale**: High-churn objects benefit from reuse, while low-churn defenders do not justify added lifecycle complexity. Reset contracts are testable and avoid stale effects/listeners.

**Alternatives considered**: Repeated instantiate/destroy risks avoidable allocations. A custom pool duplicates built-in behavior. ECS is disproportionate for 30 enemies.

**Sources**: [ObjectPool](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pool.ObjectPool_1.html), [reusable-memory patterns](https://docs.unity3d.com/6000.0/Documentation/Manual/performance-reusable-code.html)

## 5. Input and runtime UI

**Decision**: Use the installed Input System and uGUI. Define point, confirm, cancel, pause, Fever, and optional hotkey actions; use `InputSystemUIInputModule` and one placement controller.

**Rationale**: The Input System is Unity's recommended input path and supports future device bindings. uGUI fits a GameObject-based battle HUD and direct scene/prefab references already present in this project.

**Alternatives considered**: Legacy Input Manager is not preferred for new work. UI Toolkit is viable for screen UI but mixing systems adds focus/event complexity without current benefit.

**Sources**: [Unity input overview](https://docs.unity3d.com/6000.0/Documentation/Manual/Input.html), [UI system comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html), [Input System UI support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/UISupport.html)

## 6. Local save and recovery

**Decision**: Serialize a versioned DTO as JSON under `Application.persistentDataPath`. Write a temporary file, validate it, replace the primary file, retain one backup, and fall back to defaults when recovery is impossible.

**Rationale**: Versioned human-readable data supports migration and corruption tests. Stable string IDs decouple progress from asset ordering. ScriptableObjects and PlayerPrefs are unsuitable as the only structured campaign store.

**Alternatives considered**: Full PlayerPrefs storage lacks structured validation/migration. Binary serialization is opaque. Mutable ScriptableObject saves are inappropriate in deployed builds.

**Sources**: [persistentDataPath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Application-persistentDataPath.html), [JsonUtility](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/JsonUtility.html), [File.Replace](https://learn.microsoft.com/dotnet/api/system.io.file.replace)

## 7. Test layering and determinism

**Decision**: Use the installed Unity Test Framework `1.6.0`. Put synchronous rule and persistence tests in EditMode; reserve PlayMode for scene, frame, UI, lifecycle, and presentation integration. Inject time/random sources into rule logic and assert deterministic snapshots for identical seeds and tick-stamped commands.

**Rationale**: EditMode is fast and isolates failures, while PlayMode catches missing references, scene list, subscriptions, and lifecycle defects. Seeded state reproduces boundary bugs.

**Alternatives considered**: All-PlayMode testing is slow and timing-sensitive. Directly testing `MonoBehaviour.Update` for every rule makes reproduction and state inspection harder.

**Sources**: [Unity Test Framework 1.6 manual](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/manual/index.html), [Random.state](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Random-state.html)

## 8. Performance verification

**Decision**: Target 60 FPS using a custom `ProfilerRecorder` harness and a 30-minute pre-release soak in a Windows Development Build. On the documented reference machine, disable VSync/frame cap, warm up 10 seconds, sample 7,200 frames, compute nearest-rank percentiles, and record total, CPU main-thread, GPU, allocations, pools, batches, and memory.

**Rationale**: Short regression catches routine changes while soak testing exposes leaks and pool-reset failures. Player profiling avoids Editor distortion.

**Alternatives considered**: Average FPS in Editor hides percentile spikes, allocations, and target-build differences.

**Sources**: [ProfilerRecorder](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html), [profiling target device](https://docs.unity3d.com/6000.0/Documentation/Manual/profiling-target-device.html)

## 9. Online-generated and third-party assets

**Decision**: Apply a `Quarantined → Working → Review → Approved → Runtime` pipeline for shippable content. Keep reference inputs, source masters, evidence, and tool/editor packages in evidence-only records that never become runtime assets. Store versioned JSON records and SHA-256 hashes under `Docs/AssetProvenance`.

**Rationale**: Provider terms and pages change, and file presence does not prove distribution rights. Source-type-specific evidence, tri-state rights decisions, immutable snapshots, recheck dates, and an allowlist reduce accidental release of raw, reference, rejected, expired-review, or uncertain content. `AssetPostprocessor` and batch validation can enforce technical import rules.

**Alternatives considered**: Direct import with manual review is easy to bypass. A spreadsheet path alone is insufficient after renames. Treating all AI output or all free downloads as automatically safe ignores input, third-party, license, and jurisdiction risks.

**Sources**: [AssetPostprocessor](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetPostprocessor.html), [Unity command-line arguments](https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html), [current OpenAI terms landing page](https://openai.com/policies/terms-of-use/), [CC0](https://creativecommons.org/publicdomain/zero/1.0/), [Unity Asset Store terms](https://unity.com/legal/as-terms), [U.S. Copyright Office AI initiative](https://www.copyright.gov/ai/)

## 10. Art/audio normalization

**Decision**: Keep lossless masters outside runtime, then export category-specific PNG/WAV files after human cleanup. Standardize sprite canvas, alpha, pivot, PPU, palette, outline, lighting, and gameplay-scale contrast. Standardize audio boundaries, fades/loops, loudness balance, channels, and import profile.

**Rationale**: Generated outputs commonly need cleanup and consistency work. Unity sprite/audio import settings must match usage and platform trade-offs.

**Alternatives considered**: Shipping raw generated output weakens coherence/readability. Precompressing masters causes avoidable quality loss and double compression.

**Sources**: [Sprite import settings](https://docs.unity3d.com/6000.0/Documentation/Manual/texture-type-sprite.html), [audio compression](https://docs.unity3d.com/6000.0/Documentation/Manual/AudioFiles-compression.html), [AudioImporterSampleSettings](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioImporterSampleSettings.html)

## 11. Legacy Spine content

**Decision**: Do not add a Spine 3.7 runtime or custom parser. P0 may use a static export only after its shippable asset record is approved. For production, only an authorized compatible Spine editor may bake required animations into PNG sequences; otherwise redraw/regenerate a replacement with the same gameplay silhouette and timing contract.

**Rationale**: The old runtime targets historical Unity versions and brings compatibility/license risk. Baked sprites remove the runtime dependency while preserving approved animation when source rights and tools permit it.

**Alternatives considered**: Adding the legacy runtime, upgrading the skeleton, or writing a parser all expand compatibility/licensing scope. A static frame is acceptable for prototype but not necessarily final animation quality.

**Sources**: [Spine export guide](https://esotericsoftware.com/spine-export/), [spine-unity version matrix](https://esotericsoftware.com/spine-unity-installation), [Spine Runtimes License](https://esotericsoftware.com/licenses/Spine-Runtimes-License-Agreement.pdf)

## 12. Build gate

**Decision**: Require clean import/compile, EditMode, PlayMode, Player smoke, asset/provenance validation, clean Windows build, executable smoke, and error-log scan. Profile a Development Build and separately smoke the release build.

**Rationale**: Editor-only success does not prove scene inclusion, serialization, stripping, performance, asset allowlisting, or executable startup.

**Alternatives considered**: Manual Editor playtesting alone leaves packaging and repeatability gaps.

**Sources**: [Unity Test Framework 1.6 command-line reference](https://docs.unity3d.com/Packages/com.unity.test-framework@1.6/manual/reference-command-line.html), [BuildReport](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Reporting.BuildReport.html), [build from command line](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html)

## Legal uncertainty retained

This plan defines a risk-control workflow, not legal advice. A provider statement that users own output does not guarantee copyright protection, non-infringement, or permission in every territory. Brand-critical art, music, likeness/voice, disputed output, and content resembling existing intellectual property require qualified review before release.
