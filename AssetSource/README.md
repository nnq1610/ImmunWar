# Immune War asset intake

All external or generated files enter `Incoming` first. Work only from copies in `Working`; retain the original SHA-256 and source/job identifier. Nothing under `Incoming`, `Working`, `ApprovedMasters`, or `RejectedArchive` is a Unity runtime asset.

An export may be copied to `Assets/ImmunWar` only after its JSON record under `Docs/AssetProvenance/records` has status `Approved`, rights decisions are resolved, evidence is present, and the approved export hash matches. Rejected and revoked material stays outside `Assets`.

`ApprovedMasters/LegacySpine` is source/archive storage. The game ships static or baked Unity-compatible exports only and does not use a Spine runtime or custom parser.

