# Tactical Units v2 — AI Generation Evidence

Generated on 2026-09-18 with the built-in OpenAI image generation tool. The existing P0 Macrophage, T-Cell, Energy Cell, and Virus images were supplied only as visual style references. All outputs remain working candidates and are not release-approved.

## Shared constraints

- Unity 2D character sprite for Immune War.
- Polished glossy, cute biological character with soft 3D-painted rendering.
- One centered full-body subject on a genuinely transparent square canvas.
- Readable at approximately 96 px.
- No text, logo, frame, ground shadow, or watermark.
- Clean silhouette and clear friendly/enemy faction language.

## Candidate inventory

| Asset ID | Working candidate | SHA-256 | Review recommendation |
|---|---|---|---|
| DEF003 | `DEF003_BCell_candidate_v2.png` | `b3c5944f0bd9b1db1a0df1a81ac34842bfdb54ea79059a0ce107edbee6b5f56d` | Strong approve candidate; antibody motif and support role are immediately readable. |
| DEF004 | `DEF004_NKCell_candidate_v2.png` | `0336782cea0dec39584894c374b52d880b9c682f01aaee1e8f337cfe2094b67a` | Strong approve candidate; combat silhouette and purple palette distinguish it from T-Cell. |
| DEF005 | `DEF005_Platelet_candidate_v2.png` | `e0583238a8464ea8c5d6605de01acf933a8e6132c3a1b1266a909f9ef6f6cbee` | Conditional candidate; repair shield reads well, but reduce hostile-red association during final review if needed. |
| ENE002 | `ENE002_Bacteria_candidate_v2.png` | `02085f9f6bb83a4022b24a9fd74ea5be2370c50a2112908aa9be4482ecca43e7` | Strong approve candidate; elongated toxic silhouette is distinct from friendly units. |
| ENE003 | `ENE003_MutantVirus_candidate_v2.png` | `01f002411a9f381c2290483b3429c18cb76424bbaacc371938f359174d6e42e2` | Strong approve candidate; clearly communicates a dangerous mutation of ENE001. |

## Platelet v3 color revision

`DEF005_Platelet_candidate_v3.png` was created as a targeted edit of the v2 candidate. Only the dominant body palette was changed from coral/red to friendly golden-peach and amber with teal accents; the character identity, silhouette, pose, face, repair shield, transparent background, and rendering style were requested unchanged.

- SHA-256: `c3cdffafcabe3441aa9d2b0a2dd883cd9819603c2a17ea6743b3aadcb49cdc58`
- Technical format: 1254×1254 PNG color type 6 (RGBA with alpha)
- Generation job: `exec-d8ea5d33-eb34-452d-b1dd-f541a69aed71`
- Status: working candidate; separate human approval is still required before runtime export.

All candidate files are 1254×1254 PNG color type 6 (RGBA with alpha).

## Subject-specific prompts

### DEF003 — B-Cell

Create one friendly B lymphocyte character matching the P0 references: rounded antibody-support immune cell, clear Y-shaped antibody motif, helpful and clever expression, turquoise/emerald body with white antibody accents, visually distinct from Macrophage and T-Cell.

### DEF004 — NK Cell

Create one friendly natural killer cell matching the P0 references: agile burst-damage immune cell, assertive expression, compact athletic silhouette, purple cytotoxic granules and short energy-blade motif, deep violet/indigo with cyan highlights, visually distinct from T-Cell.

### DEF005 — Platelet

Create one friendly platelet repair/control character matching the P0 references: small flatter disk-like cell carrying a biological repair patch/shield, supportive determined expression, coral/peach body with teal-friendly accents and a silhouette distinct from the other cells.

### ENE002 — Bacteria

Create one hostile bacteria enemy matching the P0 rendering: elongated asymmetrical bacterium with small flagella, toxic pustules and hostile expression, toxic lime/olive/acid-yellow body with dark purple accents, immediately distinct from round friendly immune cells.

### ENE003 — Mutant Virus

Create one hostile mutated virus evolved from ENE001: irregular spiked body, asymmetrical mutation growths, glowing mutation core and aggressive expression, magenta/violet body with sickly green glow, unmistakably stronger than the basic virus.

## Required approval follow-up

Before any candidate is copied into `Assets/ImmunWar`, a human reviewer must inspect it at actual gameplay scale, accept its artistic and rights profile, update the corresponding `ShippableAsset` record, and verify the approved export hash.
