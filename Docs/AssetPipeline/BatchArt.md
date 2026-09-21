# Batch visual asset workflow

The first visual batch lives at `AssetSource/Incoming/Batches/2026-09-19-p0-visuals`: Macrophage and Virus animation sheets, lung map, and UI kit. The complete roster/map batch lives at `AssetSource/Incoming/Batches/2026-09-20-full-visuals`: five defender animation sheets, three enemy animation sheets, and brain and stomach map plates. Originals stay outside Unity runtime folders. Each batch is imported from one manifest with SHA-256 provenance records.

## Generate a new batch

1. Plan the set together: character actions and frame order, map composition, buttons and panels, reference art, dimensions, and alpha requirements. Keep related pieces in the same visual style.
2. Ask Codex image generation for the whole set in one request. It can generate distinct images concurrently; a sprite sheet must specify its grid, frame order, fixed camera, transparent background, and consistent character scale. Inspect every frame and the alpha channel before approving.
3. Copy the generated originals to a new folder under `AssetSource/Incoming/Batches`. Save one `batch.json` there with one entry per generated PNG. Each entry defines `assetId`, `source`, `destination`, `kind`, `catalogSlot`, `columns`, `rows`, `fps`, and the ordered `spriteNames`. Destinations must be unique paths under `Assets/ImmunWar/Art/Batch`.
4. Review the whole batch at game scale. Check similarity and input rights, visual consistency, frame boundaries, transparency, and UI readability. Create provenance records and evidence in `Docs/AssetProvenance` for each accepted PNG. Keep rejected images in Incoming only.
5. In Unity, run **Immune War > Assets > Import Approved Batch** for the current P0 batch. For another batch, call the editor method below with its manifest. The importer checks all records and PNG dimensions before copying any file, then slices sheets and creates clips and controllers.

```powershell
& "D:/Unity/Editors/6000.3.24f1/Editor/Unity.exe" -batchmode -nographics -projectPath "D:/assetUnity/ImmunWar_Unity_Project_Imported" -batchManifest "AssetSource/Incoming/Batches/<batch-id>/batch.json" -executeMethod ImmunWar.Editor.BatchArtImporter.ImportFromCommandLine -logFile "Logs/batch-art-import.log" -quit
```

The catalog supports `macrophage`, `virus`, `lungMap`, `brainMap`, `stomachMap`, `uiKit`, all `def_*` roster IDs, and all `ene_*` roster IDs. Defender sheets use four idle frames followed by four action frames. Enemy sheets use eight move frames. The importer creates clips/controllers and links them into `PlayableArtCatalog`. The manifest can use new asset IDs to replace any slot.

The generated sheet is source art. The Unity `.anim` clips and `.controller` assets are the playable animation, and the PlayMode journey plus executable visual smoke are the acceptance checks.
