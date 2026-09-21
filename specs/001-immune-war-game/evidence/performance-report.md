# Performance and memory report

Date: 2026-09-18

## Environment

- Windows 11 Professional, x64, 16 GB physical memory.
- Unity 6000.3.24f1, batchmode, Null graphics device.
- Scenario: 30-enemy/15-defender capacity target, 300-frame (10 simulated seconds at 30 Hz) warmup, then 7,200 measured frames.

## Results

- Result: 2 passed, 0 failed (`TestResults/Performance-results.xml`).
- Main-thread frame time: p50 0.114 ms, p95 0.161 ms, p99 0.212 ms.
- Last recorded GC allocation: 330 bytes/frame (test-runner/profiler-inclusive).
- p95 threshold: under 33.34 ms — passed.
- Memory soak: 50 repeated battle restart cycles; retained managed growth remained below the 8 MB bound — passed.
- Pool capacities: enemy 40, projectile 96, VFX 64, with explicit prewarm support.

These batchmode figures validate deterministic simulation overhead and regression bounds. They are not a substitute for GPU/render profiling on every target device.
