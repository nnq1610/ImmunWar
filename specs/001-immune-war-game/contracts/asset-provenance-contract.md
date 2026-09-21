# Asset Provenance and Release Contract

This contract applies to every shippable image, animation, audio clip, font, or model considered for Immune War and to evidence-only records for reference inputs, source masters, and tool/editor packages. It is a production risk-control contract, not legal advice.

## Lifecycle

```text
Quarantined → Working → Review → Approved → Runtime (shippable only)
                         └──────→ Rejected
Approved → Revoked → Review|Rejected
```

- `Quarantined`: raw input is preserved outside runtime folders and may not be referenced by game content.
- `Working`: edited/normalized derivative; still excluded from builds.
- `Review`: technical, visual/audio, source, and usage-rights evidence is complete enough for review.
- `Approved`: evidence is accepted. Only a `ShippableAsset` approved export may enter `Assets/ImmunWar`; evidence-only records never do.
- `Rejected`: must not enter runtime folders.
- `Revoked`: previously approved but removed from the allowlist; release-critical runtime references switch to an approved fallback, while optional content is removed.

## Required Record

| Group | Required fields |
|------|-----------------|
| Identity | `recordKind`, `assetId`, optional manifest row/category/purpose, nullable runtime path, nullable fallback asset ID |
| Source | `sourceType`, author/creator, provider/service, source URL or order/job ID, acquisition/generation UTC date |
| Generation | Model/version when known, prompt, seed/settings, reference-input list and rights for each input |
| Terms evidence | Source-type-specific declaration/agreement/license/provider terms, URL, locally retained snapshot, SHA-256 snapshot hash, invoice/order reference when applicable |
| Rights decision | Tri-state `Allowed`, `Prohibited`, `Unknown`, or `N/A` for commercial use, modification, embedded redistribution, attribution, AI-input use, territory/platform limits, reviewer notes |
| Integrity | SHA-256 of exact raw bytes, working bytes, and approved-export bytes where applicable; edit history |
| Quality | Gameplay-scale review, alpha/loop review, consistency review, technical import profile |
| Approval | Status, reviewers, review dates, attribution/NOTICE text, recheck date when required |

`recordKind` is one of `ShippableAsset`, `ReferenceInput`, `SourceMaster`, or `ToolPackage`. Unknown or unresolved rights values are treated as not approved. A fallback is required only for a release-critical shippable asset; optional content may be removed from mappings and the build.

### Evidence by source type

- `in_house`: creator declaration, contributor identity, creation date, input/reference record IDs, and rights decision. A public license URL is `N/A`.
- `commissioned`: signed agreement/order evidence covering the intended use, contributor identity, and any attribution/territory limits.
- `third_party`: license/EULA snapshot applicable on the acquisition date, source URL/order, attribution, and embedded-redistribution decision.
- `ai_generated`: provider terms snapshot applicable on the generation date, provider/model/job metadata, prompt/settings, account/plan evidence where relevant, and rights for every reference input.

## Machine-readable record

- Location: `Docs/AssetProvenance/records/<assetId>.json`.
- Contract: `asset-record.schema.json`.
- Hash algorithm: SHA-256 over exact file bytes, lowercase hexadecimal.
- Evidence snapshots: `Docs/AssetProvenance/evidence/<assetId>/`; their hashes are stored in the record.
- Records are versioned. Automated validation checks schema, semantic conditions, file hashes, approval status, and recheck date before allowlisting runtime content.

## Content Rules

- The team must have permission to upload every reference input to a generation provider.
- Unity Asset Store assets must not be supplied to an AI generator or training/generation workflow without express permission from both the applicable asset provider and Unity terms governing that use.
- Generated content is rejected or escalated when it contains watermarks, logos, identifiable people/voices, recognizable protected characters, copied text, or suspicious similarity to existing intellectual property.
- Provider ownership language does not replace third-party-rights review or guarantee copyright protection.
- `NonCommercial` content cannot enter a commercial release. `NoDerivatives` content cannot be modified. ShareAlike, custom, restricted, voice/likeness, brand-critical, or disputed content requires explicit review.
- Existing imported virus assets retain their original source/license records. They are not release-approved merely because they are already in the Unity project. Legacy `.spine`, `.json`, and `.atlas` sources remain under `AssetSource/ApprovedMasters/LegacySpine` or an external archive, never a runtime folder.

## Technical Acceptance

### Images and animation

- Correct Sprite import mode, size, aspect, alpha, pivot, PPU, maximum size, and platform settings.
- No alpha halo, watermark, clipped silhouette, unreadable contrast, atlas bleeding, or missing frame.
- Master files remain lossless outside runtime folders; approved runtime export is PNG RGBA where transparency is required.
- Defender/enemy readability is reviewed at actual gameplay scale, not only at source resolution.

### Audio

- Source master is lossless, boundaries are trimmed, fades/loops are clean, and there is no clipping, speech/watermark, or recognizable protected sample.
- Channels, sample rate, load type, compression, and platform override match the clip's use.
- Music and repeated SFX are auditioned in the representative battle mix.

### Legacy Spine

- No Spine 3.7 runtime or custom parser is introduced.
- P0 may use a static export only after its own `ShippableAsset` record reaches `Approved` and its hash matches.
- Animated production use requires verified source rights and an authorized compatible editor export to ordinary PNG sequence/sprite sheet; otherwise replace the asset.

## Automated Release Gate

Release validation fails when:

1. A runtime asset has no `ShippableAsset` record or its status is not `Approved`.
2. The runtime file hash differs from the approved-export hash.
3. Terms snapshot, required rights decision, attribution, or NOTICE is missing.
4. A quarantine, raw reference, working master, or rejected file is included in the Player build.
5. An asset breaks its category import profile or has missing Unity references.
6. A revoked release-critical asset lacks an approved fallback, or an optional revoked asset is still mapped/included.
7. The 17 P0 manifest IDs/substitutes are incomplete for the P0 milestone.
8. The 51-item category totals or required notices are incomplete for full-campaign release.
9. A record's mandatory recheck date has elapsed without a renewed review.

Human visual/audio review remains mandatory; automation cannot determine artistic consistency, infringement, or loop quality reliably by itself.
