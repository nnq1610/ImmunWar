# Battle Interface Contract

This contract defines the boundary between deterministic battle rules and Unity presentation. Commands request state changes. Events report committed state changes. Presentation may animate an event but may not independently change authoritative battle state.

## Command Envelope

Every command contains:

- `commandId`: unique within the battle attempt.
- `battleId`: target battle attempt.
- `type`: command type below.
- `targetTick`: next eligible fixed 30 Hz simulation tick.
- `sequence`: monotonic ordering for commands assigned to the same tick.
- `issuedAtUnscaledTime`: diagnostic timestamp only; never decides simulation outcome.
- Type-specific payload.

Duplicate `commandId` values return the original disposition and do not apply effects twice.

## Commands

| Command | Allowed state | Payload | Accepted result | Rejection examples |
|--------|---------------|---------|-----------------|--------------------|
| `SelectDefender` | `Preparing`, `Running` | `defenderConfigId` | Placement preview opens | Locked/unknown defender, battle terminal |
| `ConfirmPlacement` | `Preparing`, `Running` | `defenderConfigId`, `nodeId` | ATP spent once; node occupied; defender created | Insufficient ATP, occupied/invalid node, role blocked |
| `CancelPlacement` | Any non-terminal state | none | Preview closes; no ATP change | None; idempotent |
| `StartNextWave` | `Preparing`, eligible intermission | none | Wave enters spawning | Already running, no next wave |
| `ActivateFever` | `Running` | none | Fever changes `Ready → Active` | Not ready, paused, terminal |
| `PauseBattle` | `Running` | none | State becomes `Paused` | Already paused/terminal |
| `ResumeBattle` | `Paused` | none | State becomes `Running` | Not paused/terminal |
| `RestartBattle` | `Paused`, `Victory`, `Defeat` | none | New attempt with clean runtime state | Restart already committed |
| `ExitBattle` | Any non-exited state | none | State becomes `Exited`; committed progress preserved | Exit already committed |

Rejections never spend ATP, create an instance, grant a reward, or alter progression. They emit one `CommandRejected` event with a player-safe reason code.

## Events

Every committed event contains `battleId`, authoritative `tick`, monotonically increasing `sequence`, `type`, and payload. Consumers ignore an already-observed `(battleId, sequence)` pair.

| Event | Required payload |
|------|------------------|
| `BattleStateChanged` | `previous`, `current`, `reason` |
| `CommandRejected` | `commandId`, `reasonCode`, optional `subjectId` |
| `PlacementPreviewChanged` | `defenderConfigId`, optional `nodeId`, `valid`, optional `reasonCode` |
| `DefenderPlaced` | `commandId`, `defenderInstanceId`, `configId`, `nodeId`, `atpCost` |
| `AtpChanged` | `previous`, `current`, `delta`, `reasonId` |
| `VitalityChanged` | `previous`, `current`, `delta`, `enemyInstanceId` |
| `EnemySpawned` | `enemyInstanceId`, `configId`, `routeId`, `waveIndex` |
| `EnemyDamaged` | `enemyInstanceId`, `sourceInstanceId`, `amount`, `remainingHealth` |
| `EnemyDefeated` | `enemyInstanceId`, `sourceInstanceId`, `rewardAmount` |
| `EnemyReachedOrgan` | `enemyInstanceId`, `organDamage` |
| `WaveChanged` | `waveIndex`, `phase`, `remainingConfigured`, `activeEnemies` |
| `InfectionChanged` | `nodeId`, `previous`, `current`, `sourceId` |
| `MutationChanged` | `enemyInstanceId`, `previous`, `current`, `reason` |
| `FeverChanged` | `phase`, `charge`, `maximum`, `remainingDuration` |
| `BossPhaseChanged` | `enemyInstanceId`, `previousPhase`, `currentPhase` |
| `BattleEnded` | `result`, `mapId`, `finalVitality`, `progressCommitted` |

## Exactly-Once Invariants

1. One accepted placement command produces one defender, one ATP cost, and one `DefenderPlaced` event.
2. One enemy instance commits one terminal result: defeated or reached organ, never both.
3. One enemy death grants its configured reward at most once.
4. One enemy reaching the goal damages vitality at most once.
5. One battle attempt emits one `BattleEnded` event and commits campaign progress at most once.
6. Restart creates a new `battleId`; no pooled object, event subscription, or timer from the old attempt remains active.
7. Runtime instance IDs derive from battle-local placement/spawn sequences; random GUID generation cannot affect authoritative ordering or deterministic snapshots.

## Presentation Responsibilities

- HUD derives displayed ATP, vitality, wave, Fever, and battle result from state/events.
- Input/UI prevents `ConfirmPlacement` from being emitted while the pointer is over interactive UI; this is a PlayMode presentation invariant, not a domain rejection reason.
- Audio/VFX may coalesce repetitive noncritical feedback but may not suppress required state information.
- Missing presentation content uses an approved fallback and logs the missing presentation ID; rules continue.
- Pause-menu animation uses unscaled time. Combat, spawns, scaled timers, and command acceptance obey battle state.
