# Data Model: Immune War Playable Game

**Date**: 2026-09-18  
**Model rule**: Authored configuration is immutable during play. Mutable state belongs to a battle/session instance or to versioned local save data. All cross-file references use stable, unique string IDs rather than list indexes or display names.

## Relationship Overview

```text
CampaignProgress ──unlocks──> OrganMapConfig
OrganMapConfig ──contains──> RouteConfig, DefenseNodeConfig, WaveConfig
WaveConfig ──spawns──> EnemyConfig
BattleState ──instantiates──> EnemyState, DefenderState, DefenseNodeState
DefenderState ──defined by──> DefenderConfig
EnemyState ──defined by──> EnemyConfig
DefenderConfig ──uses──> AbilityConfig
EnemyConfig ──uses──> MutationDefinition, BossPhaseConfig
BattleState ──owns──> EconomyState, VitalityState, FeverState, WaveState
DefenderState/EnemyState/DefenseNodeState ──receive──> StatusEffectState
Config presentation references ──resolve through──> PresentationConfig ──must map to──> approved Shippable AssetRecord
```

## Authored Configuration Entities

### DefenderConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required, unique, immutable after release; e.g. `def_macrophage` |
| `displayNameKey` | string | Required player-facing localization key |
| `role` | enum | `Blocker`, `Damage`, `Economy`, `Support`, `Burst`, `HealCleanse` |
| `atpCost` | integer | Greater than zero |
| `maxHealth` | number | Greater than zero when the defender can be attacked |
| `attackRange` | number | Non-negative world distance |
| `attackInterval` | number | Greater than zero when the role attacks |
| `baseDamage` | number | Non-negative |
| `blockCapacity` | integer | Non-negative; P0 Macrophage greater than zero |
| `generationAmount` | integer | Non-negative; P0 Energy Cell greater than zero |
| `generationInterval` | number | Greater than zero when generation is enabled |
| `targetingRule` | enum | Deterministic target priority identifier |
| `abilityIds` | list<string> | Every ID must resolve to an approved ability/effect definition |
| `presentationId` | string | Resolves to prefab/icon/audio references and approved assets |

**Validation**: P0 must contain exactly one config each for Macrophage, T-Cell, and Energy Cell with distinct primary roles. Expanded content adds B-Cell, NK Cell, and Platelet without duplicate IDs.

### EnemyConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required and unique; e.g. `ene_basic_virus` |
| `displayNameKey` | string | Required |
| `threatRole` | enum | `Fast`, `Tank`, `Mutant`, `Boss` |
| `maxHealth` | number | Greater than zero |
| `moveSpeed` | number | Greater than zero |
| `organDamage` | integer | Greater than zero |
| `atpReward` | integer | Non-negative |
| `blockWeight` | integer | At least one |
| `mutationDefinitionId` | string? | Required only for mutation-capable types |
| `bossPhaseIds` | list<string> | At least two for Super Pathogen, empty for non-bosses |
| `presentationId` | string | Resolves to approved runtime presentation |

**Validation**: P0 includes Basic Virus. Expanded game includes Bacteria, Mutant Virus, and Super Pathogen with distinguishable roles.

### OrganMapConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required, unique; `map_lung`, `map_brain`, `map_stomach` |
| `displayNameKey` | string | Required |
| `initialAtp` | integer | Non-negative |
| `maxVitality` | integer | Greater than zero |
| `routeIds` | list<string> | At least one valid continuous route |
| `defenseNodeIds` | list<string> | At least one valid node |
| `waveSetId` | string | Required valid wave set |
| `availableDefenderIds` | list<string> | Non-empty, all valid and unique |
| `organPresentationId` | string | Approved landmark/environment reference |
| `nextMapId` | string? | Valid next campaign map or empty for final map |

**Validation**: Each route begins at a spawn point and ends at the organ objective. Every node is addressable and does not overlap another node's occupancy area.

### RouteConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Unique within catalog |
| `waypoints` | list<Vector2> | At least two ordered, non-identical points |
| `spawnPresentationId` | string? | Optional visual marker |
| `goalPresentationId` | string | Must match the protected organ objective |

### DefenseNodeConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Unique within map |
| `position` | Vector2 | Inside playable field |
| `allowedRoleMask` | serialized flags enum | Non-zero; uses `[Flags]`, not `HashSet<T>` |
| `infectionEligible` | boolean | Determines whether infection can target the node |
| `presentationId` | string | Approved base/selection visuals |

### WaveSetConfig and WaveConfig

| Field | Type | Rules |
|------|------|-------|
| `waveSet.id` | string | Unique |
| `waveSet.waves` | list<WaveConfig> | Non-empty, ordered |
| `wave.index` | integer | Contiguous from zero |
| `wave.groups` | list<SpawnGroup> | Non-empty |
| `group.enemyId` | string | Valid enemy config |
| `group.routeId` | string | Valid route for owning map |
| `group.count` | integer | Greater than zero |
| `group.startDelay` | number | Non-negative |
| `group.spawnInterval` | number | Non-negative |
| `wave.completionRule` | enum | Default: all configured enemies spawned and resolved |

### StatusEffectConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Unique |
| `kind` | enum | `Infection`, `Cleanse`, `Slow`, `DamageOverTime`, `Mutation`, `FeverBuff`, or approved extension |
| `targetKind` | enum | `Enemy`, `Defender`, `Node`, `Battle` |
| `duration` | number | Positive for timed effects; zero only for condition-bound effects |
| `stackRule` | enum | `Reject`, `Refresh`, `Replace`, `StackToLimit` |
| `stackLimit` | integer | At least one when stacking |
| `magnitude` | number | Meaning defined by kind and validated accordingly |
| `presentationId` | string | Feedback contract reference |

### FeverConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | One active campaign definition for initial release |
| `maxCharge` | number | Greater than zero |
| `chargeRules` | list<rule> | At least one deterministic source |
| `duration` | number | Greater than zero |
| `effectIds` | list<string> | Non-empty valid battle/defender effects |
| `presentationId` | string | Approved HUD, overlay, VFX, and audio mappings |

### AbilityConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required and unique |
| `trigger` | enum | `Attack`, `Periodic`, `OnPlaced`, `OnBlocked`, `Manual`, or approved extension |
| `targetKind` | enum | `Enemy`, `Defender`, `Node`, `Battle` |
| `effectIds` | list<string> | Non-empty and resolves to StatusEffectConfig or direct validated effect |
| `cooldownTicks` | integer | Non-negative fixed simulation ticks |
| `presentationId` | string | Valid PresentationConfig |

### MutationDefinition

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required and unique |
| `trigger` | enum | Health threshold, elapsed ticks, wave condition, or explicit effect |
| `triggerValue` | number | Valid for chosen trigger |
| `resultEnemyConfigId` | string | Valid enemy config and not self-cyclic |
| `preserveHealthRatio` | boolean | Defines deterministic health transfer |
| `effectIds` | list<string> | Optional effects applied at transition |
| `presentationId` | string | Required transition feedback |

### BossPhaseConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required and unique within boss definition |
| `order` | integer | Contiguous from zero |
| `entryCondition` | record | Deterministic health/tick/wave threshold |
| `statModifiers` | record | Finite validated values |
| `abilityIds` | list<string> | All resolve to AbilityConfig |
| `presentationId` | string | Required phase-transition feedback |

### PresentationConfig

| Field | Type | Rules |
|------|------|-------|
| `id` | string | Required and unique |
| `prefab` | asset reference? | Optional only when a validated fallback exists |
| `icon` | asset reference? | Optional by presentation role |
| `audioCueIds` | list<string> | All map to approved runtime audio records |
| `vfxAssetIds` | list<string> | All map to approved runtime VFX records |
| `fallbackPresentationId` | string? | No cycles; required for release-critical missing presentation |

**Validation**: Every `abilityId`, `mutationDefinitionId`, `bossPhaseId`, and `presentationId` referenced by other configs must resolve. Every runtime asset in a PresentationConfig must map to an approved shippable AssetRecord with a matching SHA-256 hash.

## Runtime State Entities

### GameSession

- Persistent composition root created only by Bootstrap and retained with `DontDestroyOnLoad`.
- Owns validated config catalogs, `SaveService`, committed settings, and nullable `selectedMapId` scene-load payload.
- Main Menu writes only a validated unlocked `selectedMapId`; Battle consumes and clears/retains it according to navigation flow.
- Scene entry points receive dependencies from GameSession; gameplay systems do not access mutable global static state.
- Explicit test reset/application shutdown disposes services, clears subscriptions, and destroys the root so a second Bootstrap cannot inherit prior state.

### BattleState

| Field | Type | Rules |
|------|------|-------|
| `battleId` | string | Unique per attempt |
| `mapId` | string | Valid map config |
| `phase` | enum | `Preparing`, `Running`, `Paused`, `Victory`, `Defeat`, `Restarting`, `Exited` |
| `seed` | integer | Fixed for deterministic attempt |
| `economy` | EconomyState | Always present |
| `vitality` | VitalityState | Always present |
| `fever` | FeverState | Always present |
| `wave` | WaveState | Always present |
| `nodes` | list<DefenseNodeState> | One per authored node |
| `defenders` | list<DefenderState> | Active placed defenders only |
| `enemies` | list<EnemyState> | Active/resolving enemies only |
| `eventSequence` | integer | Monotonically increasing for observable battle events |
| `simulationTick` | integer | Monotonically increasing fixed 30 Hz authoritative tick |
| `nextInstanceSequence` | integer | Generates deterministic placement/spawn instance IDs |
| `resultCommitted` | boolean | Prevents duplicate result/progress writes |

### EconomyState and VitalityState

- `EconomyState.currentAtp`: integer from zero to configured display cap; every transaction has a unique reason/event ID and is applied once.
- `VitalityState.current`: integer clamped from zero to `maximum`; each enemy can record organ arrival only once.

### DefenseNodeState

- `nodeId`: authored node ID.
- `occupantDefenderInstanceId`: empty or one active defender.
- `infectionState`: `Clear`, `Infected`, or `Cleansing`.
- `activeEffectIds`: runtime effect instances targeting the node.

### DefenderState

- `instanceId`, `configId`, `nodeId` are required and stable for the placement lifetime.
- `currentHealth`, attack/generation timers, target instance ID, and active effects are mutable.
- An instance transitions through `Placing → Active ↔ Disabled → Removed`; it cannot attack or generate outside `Active`.

### EnemyState

- `instanceId`, `configId`, `routeId` are required.
- `currentHealth`, segment index, segment distance, normalized route progress, target blocker, effects, mutation state, and boss phase are mutable.
- Resolution flags `deathCommitted`, `rewardCommitted`, and `organHitCommitted` enforce exactly-once outcomes.
- State transitions: `Spawning → Advancing ↔ Blocked → Dying|ReachedOrgan`; mutation may change definition/presentation while preserving instance identity and route progress.

### WaveState

- Tracks wave index, per-group spawn cursors, active enemy count, and inter-wave status.
- Transitions: `NotStarted → Spawning → Resolving → Intermission|Completed`.
- A wave completes only after all configured spawns occurred and every spawned enemy reached one terminal result.

### FeverState

- Fields: `charge`, `maximum`, `phase`, `remainingDuration`.
- Transitions: `Charging → Ready → Active → Charging`.
- Activation is accepted only in `Running + Ready`; terminal battle state cancels active effects and prevents reactivation.

### StatusEffectState

- Fields: `instanceId`, `configId`, `sourceInstanceId`, `targetId`, `stacks`, `remainingDuration`, `appliedSequence`.
- An effect applies, refreshes/stacks/replaces according to its config, then expires or is explicitly cleansed exactly once.

## Persistence Entities

### CampaignProgress / SaveData

| Field | Type | Rules |
|------|------|-------|
| `schemaVersion` | integer | Positive; version 1 initially |
| `saveSequence` | integer | Non-negative and increases after successful commit |
| `savedAtUtc` | string | ISO-8601 UTC timestamp |
| `unlockedMapIds` | list<string> | Unique valid IDs; always includes lung |
| `completedMapIds` | list<string> | Unique subset of valid campaign maps |
| `lastSelectedMapId` | string | Must be unlocked; defaults to lung |
| `musicVolume` | number | Inclusive range 0–1 |
| `sfxVolume` | number | Inclusive range 0–1 |

**Validation/recovery**: Parse the version envelope first, migrate supported legacy DTOs to the current shape, then apply current schema and semantic validation. Unknown catalog IDs are removed with diagnostics; invalid volumes are clamped; completed maps must be unlocked, the last selected map must be unlocked, and unlocks cannot skip campaign order. When primary and backup are valid, the higher `saveSequence` wins. If both are unusable, defaults unlock only lung. An unsupported future version never overwrites its source.

## Asset Governance Entity

### AssetRecord

| Field group | Required content |
|------------|------------------|
| Identity | `recordKind`, manifest `assetId` where applicable, category, purpose, nullable runtime destination, nullable fallback ID |
| Source | Source type, author/provider, source URL/job ID, acquisition/generation UTC date |
| Generation | Provider/model/version when known, prompt/settings, reference inputs and their rights |
| Evidence | Source-type-specific declaration/agreement/license/provider-terms snapshot and SHA-256 hash |
| Rights | `Allowed`, `Prohibited`, `Unknown`, or `N/A` for commercial use, modification, embedded redistribution, attribution, and AI-input use |
| Integrity | SHA-256 of exact raw bytes and approved export bytes, edit history |
| Review | Technical reviewer/date, rights reviewer/date, quality findings, attribution text |
| Status | `Quarantined`, `Working`, `Review`, `Approved`, `Rejected`, `Revoked` |

**State transitions**:

```text
Quarantined → Working → Review → Approved
                         └──────→ Rejected
Approved → Revoked → Review|Rejected
```

`recordKind` is `ShippableAsset`, `ReferenceInput`, `SourceMaster`, or `ToolPackage`. Only an `Approved` `ShippableAsset` whose SHA-256 approved-export hash matches the runtime file may pass release validation. Evidence-only kinds never enter runtime folders. `Unknown` rights block approval. A fallback is nullable and becomes mandatory only for a release-critical asset; optional revoked content may be removed from mappings/build. A status change to `Revoked` immediately removes the asset from the allowlist and requires new evidence to return through `Review`.

## Global Validation Rules

1. IDs are case-stable, unique within their entity catalog, and never silently reused for a different meaning.
2. Every referenced ID resolves before entering Play mode or producing a build.
3. Authored config assets are never mutated by runtime gameplay or tests.
4. Runtime state does not retain direct mutable references into a prior battle after restart/exit.
5. Every runtime sprite/audio/prefab reference used by the release catalog maps to an approved AssetRecord.
6. P0 validation requires all 17 manifest P0 IDs or explicitly documented approved substitutes.
7. Full-campaign validation requires the 51-record category totals from the specification.
