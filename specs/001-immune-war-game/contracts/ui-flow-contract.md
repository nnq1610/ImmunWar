# UI Flow Contract

## Screen Flow

```text
Bootstrap
  └─> Main Menu
       ├─> Map Select ─> Battle
       │                  ├─> Pause Overlay ─> Battle|Main Menu
       │                  └─> Result Overlay ─> Retry|Map Select|Next Map
       ├─> Settings Overlay
       └─> Quit
```

Bootstrap must recover or initialize save data before interactive navigation. A failure to load progress presents a nonblocking recovery notice and continues with safe defaults.

## Main Menu

- `Continue`: available when at least one valid progression record exists; opens Map Select with the last unlocked selection.
- `New Game`: asks for confirmation only when it would replace valid progress; initializes lung unlocked.
- `Settings`: opens music and SFX controls without requiring a battle.
- `Quit`: exits the desktop application.

## Map Select

- Shows lung, brain, and stomach with `Locked`, `Unlocked`, or `Completed` state.
- A locked map explains its unlock requirement and cannot launch.
- An unlocked/completed map exposes `Start`/`Replay`.
- Victory unlocks only the configured next map; replay never removes progress.

## Battle HUD

Always visible during `Preparing`, `Running`, and `Paused`:

- ATP current value.
- Organ vitality current/maximum.
- Current wave/total waves and wave phase.
- Available defender cards with name/icon, role, cost, availability, and hotkey when assigned.
- Fever charge/state and activation control.
- Pause control.

### Placement interaction

1. Selecting a defender opens a preview with range/placement indication.
2. Hovering/pointing at a node shows valid or invalid state and a reason for invalidity.
3. Confirming a valid node sends `ConfirmPlacement`; UI waits for committed event before treating placement as spent.
4. Confirming an invalid node gives immediate rejection feedback and keeps or closes preview according to reason without spending ATP.
5. Cancel, right-click, Escape, selecting another card, pause, or terminal state closes/replaces the preview safely.

World placement input is ignored while the pointer is over interactive UI.

## Pause Overlay

- Stops simulation, spawning, combat, placement, and other battle commands while still accepting pause-menu controls such as Resume, Restart, Settings, Exit, and CancelPlacement.
- Provides `Resume`, `Restart`, `Settings`, and `Exit to Main Menu`.
- Resume returns to the same attempt without duplicate events.
- Restart/exit require one confirmation when current attempt progress would be lost.

## Result Overlay

- Appears exactly once after `Victory` or `Defeat`.
- States result and the relevant objective outcome.
- Victory offers `Next Map` only when a next map is unlocked, plus `Replay` and `Map Select`.
- Defeat offers `Retry` and `Map Select`.
- Combat input is disabled behind the overlay.

## Settings

- Music and SFX controls are independent, range from muted to full, and preview changes immediately. Only `Apply` emits a committed settings event and writes persistence.
- Controls remain usable from Main Menu and Pause.
- Cancel restores values from before the overlay opened and writes nothing; Apply commits once.

## Layout and Feedback

- All critical controls and values remain visible and non-overlapping at 1920×1080 and 1280×720 landscape.
- Color is not the only indicator for valid/invalid, infection/cleanse, vitality damage, or Fever ready/active.
- Every valid command receives visible or audible acknowledgement within 250 ms under the representative performance scenario.
- Required player-facing errors use stable reason codes mapped to concise copy; raw exceptions and internal IDs are not displayed.
