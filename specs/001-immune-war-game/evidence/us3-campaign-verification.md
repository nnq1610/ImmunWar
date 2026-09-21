# US3 campaign verification

Date: 2026-09-18

## Automated results

- Campaign EditMode suite: 4 passed, 0 failed (`TestResults/US3-EditMode-results.xml`).
- Campaign PlayMode journey plus US1/US2 regression: 5 passed, 0 failed (`TestResults/US3-PlayMode-regression-results.xml`).
- Tests cover ordered/idempotent unlocks, campaign completion, save/reload and corrupt-primary recovery, organ config invariants, boss thresholds/invulnerability/abilities/death, and the map-select-to-result-to-reload journey.

## Integrated content

- Brain and Stomach maps each have stable route, node, wave, and catalog entries.
- Super Pathogen has a prefab, enemy config, three ordered phase configs, phase abilities, transition invulnerability, and presentation binding.
- Map Select prefab exposes lung/brain/stomach cards; Main Menu and battle result controllers provide campaign transitions.

## Open release gate

Brain/Stomach and Super Pathogen visuals are functional generated candidates. T073 and T074 remain open pending raw-source/provenance intake and explicit human visual, audio, similarity, and release approval.
