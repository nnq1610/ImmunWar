# Production Audio Candidate Generation

Generated on 2026-09-19 by `ImmunWar.Editor.ProductionAudioGenerator` using deterministic, in-house procedural synthesis. Runtime WAV files are mirrored to the matching `AssetSource/Working` folders as raw masters. No third-party samples or external music were used.

The generator replaces the original half-second tone placeholders with two loop-oriented music beds and thirteen purpose-specific feedback sounds. Asset records remain `Review`: technical validation is complete, but a human listening pass is required before release approval.

## Technical validation

- Format: PCM WAV, mono, 48 kHz, 16-bit.
- Files checked: 15; unique SHA-256 hashes: 15.
- Header or format failures: 0.
- Clipped samples at full scale: 0.
- Unity import: all 15 files imported successfully in Unity 6000.3.24f1.
- Unity generation marker: `IMMUNEWAR_PRODUCTION_AUDIO_CANDIDATES_OK` in `Logs/production-audio.log`.

| Asset | Purpose | Duration | Peak | RMS |
|---|---|---:|---:|---:|
| AUD001 | Menu music | 24.00 s | 0.4285 | 0.1627 |
| AUD002 | Battle music | 24.00 s | 0.5773 | 0.1639 |
| AUD003 | UI click | 0.14 s | 0.8054 | 0.1728 |
| AUD004 | UI error | 0.34 s | 0.7917 | 0.1713 |
| AUD005 | T-Cell attack | 0.28 s | 0.7689 | 0.1717 |
| AUD006 | Virus death | 0.48 s | 0.8200 | 0.1782 |
| AUD007 | B-Cell antibody | 0.46 s | 0.7781 | 0.1384 |
| AUD008 | NK attack | 0.24 s | 0.8200 | 0.1786 |
| AUD009 | Platelet heal | 0.72 s | 0.8184 | 0.1733 |
| AUD010 | ATP generated | 0.52 s | 0.8200 | 0.2211 |
| AUD011 | Infection starts | 0.68 s | 0.8200 | 0.1770 |
| AUD012 | Infection cleansed | 0.62 s | 0.8200 | 0.1734 |
| AUD013 | Fever activated | 1.10 s | 0.8200 | 0.2212 |
| AUD014 | Vitality damaged | 0.42 s | 0.8200 | 0.1783 |
| AUD015 | Boss phase transition | 1.25 s | 0.8200 | 0.1772 |

## Human listening gate

Listen for semantic fit, harshness, fatigue under repetition, menu/battle loop transitions, relative loudness, and whether important combat cues remain distinguishable during music playback. Only after this pass may the 15 records change from `Review` to `Approved`.
