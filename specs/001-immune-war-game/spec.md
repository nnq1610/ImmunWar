# Feature Specification: Immune War Playable Game

**Feature Branch**: `001-immune-war-game` (proposed; no branch hook is configured)

**Created**: 2026-09-18

**Status**: Draft

**Input**: User description: "Đọc `09_ASSET_MANIFEST.xlsx` và đặc tả việc triển khai game Unity; chủ động điều chỉnh và tạo asset bằng các dịch vụ trực tuyến."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Defend an Organ in a Complete Battle (Priority: P1)

As a player, I can start a battle, spend ATP to place immune defenders on valid defense nodes, survive pathogen waves travelling through blood-vessel paths, and reach a clear victory or defeat state when the wave objective or organ vitality condition is met.

**Why this priority**: This is the smallest complete experience that proves the core strategy loop and gives every later system a playable foundation.

**Independent Test**: Start the first organ map with no prior progress, place defenders, complete all waves, and verify that ATP, enemy movement, attacks, organ vitality, victory, defeat, pause, and restart work without relying on later maps or advanced units.

**Acceptance Scenarios**:

1. **Given** a new battle with sufficient ATP and an empty valid node, **When** the player selects and places a defender, **Then** ATP is deducted once, the defender occupies that node, and its role and attack range are readable before placement is confirmed.
2. **Given** an active wave, **When** enemies enter and follow the vessel route, **Then** defenders acquire valid targets, enemies take damage, defeated enemies are removed, and surviving enemies that reach the organ reduce vitality by the configured amount.
3. **Given** the last enemy of the last wave is defeated while organ vitality remains above zero, **When** the battle resolves, **Then** the player sees a victory result and can replay or continue.
4. **Given** organ vitality reaches zero, **When** the battle resolves, **Then** spawning and combat stop, a defeat result explains the failed objective, and restart is available.

---

### User Story 2 - Make Tactical Immune-System Choices (Priority: P2)

As a player, I can combine immune defenders with distinct battlefield roles, respond to infection and mutation threats, and activate Fever Mode at a meaningful moment instead of relying on a single dominant unit.

**Why this priority**: Distinct counters and temporary threats create the strategy promised by the immune-system theme and make replay decisions meaningful.

**Independent Test**: Use a test battle containing a blocked lane, an infected node, a mutant enemy, and a charged Fever meter; verify that each available counter changes the battle state and communicates the result.

**Acceptance Scenarios**:

1. **Given** Macrophage, T-Cell, and Energy Cell are available, **When** each is placed under the same controlled conditions, **Then** each demonstrates a different primary role: blocking/endurance, damage, or ATP generation.
2. **Given** an infection effect is active on a node, **When** its cleanse condition is fulfilled, **Then** the infection is removed and both the danger and recovery are visibly and audibly communicated.
3. **Given** an enemy mutates, **When** mutation completes, **Then** the enemy's changed threat is identifiable and the player can respond with an effective counter.
4. **Given** Fever Mode is fully charged, **When** the player activates it, **Then** its duration, battlefield benefit, and end state are clear and it cannot be activated again until recharged.

---

### User Story 3 - Progress Through Organ Battles and a Boss (Priority: P3)

As a player, I can progress through lung, brain, and stomach battle maps, encounter escalating enemy compositions, and finish the available campaign with a multi-phase Super Pathogen boss battle.

**Why this priority**: Multiple maps and a boss turn the core loop into a short game with progression, variety, and a completion goal.

**Independent Test**: Complete the three-map sequence from a fresh profile, verify unlock order and retained completion records, then defeat the Super Pathogen after observing at least one distinct phase change.

**Acceptance Scenarios**:

1. **Given** only the first map is available, **When** the player wins it, **Then** the next map unlocks and the completed map remains replayable.
2. **Given** the player enters each organ map, **When** the battle begins, **Then** the organ landmark, vessel layout, vitality objective, and wave composition are distinct and readable.
3. **Given** the Super Pathogen reaches its phase threshold, **When** the phase changes, **Then** the boss behavior and presentation change, the transition is announced, and existing battle progress remains valid.
4. **Given** the player closes and reopens the game after a completed map, **When** the map selection is shown, **Then** earned map progress is restored.

---

### User Story 4 - Understand the Battle Through Cohesive Feedback (Priority: P4)

As a player, I can distinguish defenders, enemies, paths, valid nodes, projectiles, infection, vitality damage, ATP gain, and Fever state at a glance, with independent controls for music and sound effects.

**Why this priority**: Clear visual and audio language reduces trial-and-error and makes the tactical systems usable even before final-polish content is complete.

**Independent Test**: Run a representative battle with every core P0 event, then ask first-time playtesters to identify the event and required response without developer explanation.

**Acceptance Scenarios**:

1. **Given** a crowded battle, **When** the player inspects the field, **Then** allies, hostile units, traversable vessel paths, valid nodes, and the protected organ remain visually distinguishable.
2. **Given** ATP gain, invalid placement, an attack hit, enemy death, and vitality damage occur, **When** each event triggers, **Then** it has immediate and distinguishable feedback.
3. **Given** music or effects are distracting, **When** the player changes the corresponding setting, **Then** the chosen volume updates and remains set for later sessions.

### Edge Cases

- The player attempts to place a defender with insufficient ATP, on an occupied node, outside a node, or while the battle is paused or resolved.
- Multiple enemies reach the organ during the same resolution interval; vitality loss is applied exactly once per enemy and defeat resolves once.
- A target dies, leaves range, or becomes invalid while a defender attack is in flight; the attack resolves without duplicate rewards or a stalled defender.
- ATP reaches zero, its display limit, or receives several gains and costs at nearly the same time.
- The player activates Fever Mode on the same boundary at which the final wave, victory, defeat, pause, or restart begins.
- A node becomes infected while occupied, or its occupant is removed during cleansing.
- A mutation or boss phase transition occurs while the enemy is slowed, damaged, blocked, or being removed.
- Saved progress is absent, from an older compatible version, incomplete, or unreadable; the player can still start safely without a crash.
- A required visual or audio asset is unavailable or rejected during license review; an approved placeholder preserves gameplay and readability.
- The display uses a supported non-default aspect ratio; critical HUD information and controls remain visible and usable.

## Requirements *(mandatory)*

### Scope

The feature covers a single-player, offline, 2D lane-defense game set inside the human body. It includes one complete P0 playable slice on the lung map, followed by brain and stomach maps, advanced defenders and enemies, and a final boss for the expanded release. It also covers creation, selection, provenance, and acceptance of the audiovisual assets required by the asset manifest.

Online multiplayer, accounts, social systems, paid content, advertisements, cloud saves, cinematic story sequences, user-generated levels, and medically diagnostic or treatment claims are outside this feature.

### Functional Requirements

- **FR-001**: The game MUST provide a start flow that lets a new player enter the first playable battle without an account or network connection.
- **FR-002**: Each battle MUST define an organ objective, one or more continuous enemy routes, valid defense nodes, an initial ATP amount, ordered waves, and explicit victory and defeat conditions.
- **FR-003**: The player MUST be able to inspect a defender's name, battlefield role, ATP cost, and placement area before confirming placement.
- **FR-004**: The game MUST reject invalid placement without spending ATP and MUST explain the rejection through immediate feedback.
- **FR-005**: A confirmed placement MUST spend the stated ATP exactly once and reserve the selected node until the defender is removed or the battle ends.
- **FR-006**: Enemies MUST enter according to the active wave, follow their designated route, interact with blocking and damage effects, and reduce organ vitality only when they successfully reach the objective.
- **FR-007**: Defenders MUST select only eligible targets and apply their stated effects consistently under equivalent battle conditions.
- **FR-008**: The P0 playable slice MUST include Macrophage, T-Cell, and Energy Cell with clearly different primary roles: blocking/endurance, direct damage, and ATP generation.
- **FR-009**: The expanded roster MUST add B-Cell, NK Cell, and Platelet with support/counter roles that do not duplicate the P0 roles.
- **FR-010**: The P0 playable slice MUST include a Basic Virus enemy; the expanded game MUST add Bacteria, Mutant Virus, and Super Pathogen with distinguishable threat profiles.
- **FR-011**: Defeating enemies and generating ATP MUST award the configured resources once, even when several combat events resolve together.
- **FR-012**: Organ vitality MUST be continuously visible during battle and MUST never resolve below zero or above its configured maximum.
- **FR-013**: The game MUST support infection as a visible harmful node or battlefield state with a defined start condition, effect, and cleanse condition.
- **FR-014**: The game MUST support mutation as an identifiable enemy-state change with a defined trigger, changed threat, and at least one effective player response.
- **FR-015**: Fever Mode MUST have a visible charge state, an explicit activation action, a time-bounded benefit, and a clear cooldown or recharge rule.
- **FR-016**: A battle MUST support pause, resume, restart, victory, and defeat without duplicating spawns, rewards, costs, progress, or result screens.
- **FR-017**: The campaign MUST provide lung, brain, and stomach maps with distinct landmarks, route layouts, and wave compositions.
- **FR-018**: Winning a campaign map MUST persist its completion and unlock the next intended map; replaying an unlocked map MUST NOT erase later valid progress.
- **FR-019**: The Super Pathogen encounter MUST include at least two clearly communicated phases with different battle pressure or behavior.
- **FR-020**: The HUD MUST show current ATP, organ vitality, wave progress, available defenders, placement validity, Fever state, and pause access without covering critical combat areas.
- **FR-021**: Core events MUST have distinguishable feedback: placement accepted/rejected, defender attack, enemy hit/death, ATP gain, infection start/cleanse, Fever activation, vitality damage, boss phase, victory, and defeat.
- **FR-022**: The player MUST be able to control music and effects volume independently, and the choices MUST persist between sessions.
- **FR-023**: The game MUST preserve a playable fallback when a nonessential presentation asset is missing; missing content MUST NOT silently remove a required gameplay rule or control.
- **FR-024**: The asset catalog MUST retain the 51 manifest records as the target content baseline: 6 Defender, 4 Enemy, 9 Environment, 8 VFX, 9 UI, and 15 Audio items.
- **FR-025**: The P0 playable slice MUST supply and integrate all 17 P0 manifest items, or an explicitly approved and functionally equivalent substitute, before it is considered complete.
- **FR-026**: Every externally sourced or online-generated asset MUST record its source or generation provider, creation/download date, permitted usage, modification/attribution requirements, and in-project destination before release approval.
- **FR-027**: Online-generated art MUST be reviewed for visual consistency, clean transparency where required, legibility at gameplay scale, absence of recognizable third-party intellectual property, and compatibility with the stated purpose before acceptance.
- **FR-028**: Online-generated audio MUST be reviewed for clean looping or event boundaries, intelligibility in the battle mix, absence of recognizable protected material, and provider terms that permit the intended release.
- **FR-029**: Existing imported virus imagery MAY be used as prototype or final enemy art only after its recorded usage terms are accepted; the imported skeletal-animation pack MUST have a static visual fallback when its authored animation cannot be played.
- **FR-030**: The game MUST present the immune-system theme as stylized entertainment and MUST avoid claims that gameplay represents medical diagnosis, treatment, or scientifically exact immune behavior.

### Requirement Verification Matrix

| Requirement group | Primary verification |
|-------------------|----------------------|
| FR-001–FR-008, FR-010–FR-012, FR-016 | User Story 1 acceptance scenarios and a fresh-install lung battle |
| FR-009, FR-013–FR-015 | User Story 2 acceptance scenarios using controlled combat states |
| FR-017–FR-019 | User Story 3 acceptance scenarios and save/reload progression test |
| FR-020–FR-023 | User Story 4 acceptance scenarios plus supported-aspect-ratio review |
| FR-024–FR-029 | Asset catalog audit, in-game scale review, and license/provenance review |
| FR-030 | Content and copy review of all player-facing screens |

### Key Entities

- **Campaign Progress**: Records unlocked maps, completed maps, and the latest compatible player settings; contains no account identity.
- **Battle**: A single attempt on one organ map, including current state, ATP, vitality, Fever charge, wave position, and result.
- **Organ Map**: Defines its organ landmark, vessel routes, defense nodes, objective, wave set, and presentation identity.
- **Defense Node**: A valid placement location with occupancy and optional infection state.
- **Defender Type**: Describes a defender's role, ATP cost, targeting rules, combat effects, and presentation references.
- **Defender Instance**: A placed defender tied to a node and its current battle state.
- **Enemy Type**: Describes route behavior, durability, organ damage, rewards, special states, and presentation references.
- **Enemy Instance**: A spawned enemy with route progress, current health, applied effects, mutation state, and boss phase where relevant.
- **Wave**: An ordered set of spawn groups and timing rules associated with one battle.
- **Status Effect**: A time- or condition-bounded battle change such as infection, cleanse, slow, mutation, or Fever benefit.
- **Asset Record**: One manifest item with category, purpose, format, size or duration, priority, status, source/provenance, destination, and license decision.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of first-time playtesters can start the lung battle, place a valid defender, and begin the first wave within 3 minutes without developer assistance.
- **SC-002**: At least 80% of first-time playtesters correctly explain ATP, organ vitality, valid placement, and the basic win/lose conditions after one battle.
- **SC-003**: In controlled playtests, at least 85% of players correctly identify the primary role of Macrophage, T-Cell, and Energy Cell after using each once.
- **SC-004**: Every valid player command produces visible or audible acknowledgement within 250 milliseconds during a representative battle.
- **SC-005**: A representative P0 battle remains responsive and readable with at least 30 simultaneous enemies, 15 placed defenders, and all required HUD elements active for 30 continuous minutes without a blocking failure.
- **SC-006**: The complete lung-map session, including result and restart/continue choice, can be finished in 8–15 minutes by at least 75% of target playtesters on their second attempt.
- **SC-007**: 100% of the 17 P0 manifest items are present in the playable slice as accepted assets or approved functional substitutes, with no missing gameplay cue or control.
- **SC-008**: 100% of release-candidate third-party and online-generated assets have a completed provenance and usage-rights record; zero assets with unresolved rights are included in a distributable build.
- **SC-009**: At least 90% of playtest observations correctly distinguish ally, enemy, valid node, infection, ATP gain, vitality damage, and Fever activation without verbal explanation.
- **SC-010**: Campaign progress and audio settings are restored correctly in 100% of 20 consecutive save, close, reopen, and reload test cycles, including one safe recovery test with unreadable saved data.
- **SC-011**: All three organ maps and the final boss can be completed from a fresh start, and every intended unlock occurs exactly once in a full progression test.
- **SC-012**: No player-facing text reviewed for release makes a diagnostic, treatment, or scientific-accuracy claim about the immune system.

## Assumptions

- The initial target is a single-player offline desktop experience in landscape orientation, controlled by mouse with keyboard shortcuts as optional convenience.
- The game is a stylized 2D lane-defense experience rather than a medical simulation; clarity and tactical readability take precedence over anatomical realism.
- The first delivery milestone is the P0 lung-map slice. P1 and P2 manifest content expands the same foundation into the three-map campaign rather than blocking proof of the core loop.
- The manifest's 51 rows are the authoritative target catalog, while its current `Todo` status does not invalidate already imported prototype virus files; those files still require fit and license acceptance.
- New visual and audio content may be created through online generative services and manually adjusted. Only output whose provider terms permit the intended commercial or noncommercial distribution will be accepted.
- Generated asset dimensions and durations may be adjusted when gameplay-scale review shows a better fit, provided the asset's purpose, readability, provenance, and manifest traceability remain intact.
- Existing static virus sprites are suitable prototype fallbacks. The existing skeletal-animation source is not required for the P0 slice and may remain static until a compatible playback decision is made.
- Player progress is local to the device. No personal data, telemetry, account identity, payment, or network service is required for this feature.
- The game will use concise player-facing language and iconography. Localization beyond the initial shipping language is a separate feature.

## Dependencies

- Access to the existing imported virus assets and their current usage terms.
- Access to online image/audio generation or equivalent licensed content sources during production.
- Human review of visual consistency, audio quality, biological framing, and usage rights before any generated or third-party asset is accepted for release.
- A maintained asset provenance record that remains aligned with the manifest and the actual assets included in distributable builds.
