# Windows Release and Smoke Report

Date: 2026-09-19  
Unity: 6000.3.24f1  
Target: StandaloneWindows64 release

## Release build

- Output: `Builds/Windows/ImmuneWar.exe`
- Executable size: 667,136 bytes
- Packaged directory size: 131,382,430 bytes
- Executable SHA-256: `e1a9d9e062bbcdb91f76983fae8bb1e4fb8036d55289318ef91033f130d38ecc`
- Build result: succeeded (`IMMUNEWAR_BUILD_OK` in `Logs/BuildWindows.log`)
- Asset gate at build time: 51 approved, 0 review, `releaseReady=true`

## Packaged-player smoke

The release executable was launched headlessly with `--immunwar-smoke`. It loaded the main menu, loaded the lung battle, exercised start/pause/resume/restart, and exited itself.

- Exit code: 0
- `IMMUNEWAR_SMOKE_MENU`: present
- `IMMUNEWAR_SMOKE_BATTLE_PAUSE_RESTART_OK`: present
- `IMMUNEWAR_SMOKE_EXIT_OK`: present
- Null reference, assertion, unhandled-exception, or crash lines: 0
- Runtime log: `Logs/ExecutableSmoke.log`

The same T102 scenario subsequently passed inside the canonical EditMode suite, producing 31/31 passing tests in `TestResults/EditMode-results.xml`. The final PlayMode regression produced 9/9 passing tests in `TestResults/PlayMode-all-approved.xml`.
