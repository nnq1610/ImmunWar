using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImmunWar.Editor.AssetProvenance;
using ImmunWar.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.Editor
{
    [Serializable] public sealed class BatchArtEntry
    {
        public string assetId;
        public string source;
        public string destination;
        public string kind;
        public string catalogSlot;
        public int columns = 1;
        public int rows = 1;
        public int fps = 8;
        public string[] spriteNames;
    }

    [Serializable] public sealed class BatchArtManifest
    {
        public int schemaVersion = 1;
        public string batchId;
        public BatchArtEntry[] entries;
    }

    public static class BatchArtImporter
    {
        private const string DefaultManifest = "AssetSource/Incoming/Batches/2026-09-19-p0-visuals/batch.json";
        private const string CatalogPath = "Assets/ImmunWar/Resources/ImmuneWar/PlayableArtCatalog.asset";
        private const string AnimDir = "Assets/ImmunWar/Art/Animations/Batch";

        [MenuItem("Immune War/Assets/Import Approved Batch")]
        public static void ImportFromMenu() => Import(DefaultManifest);

        public static void ImportFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-batchManifest");
            Import(index >= 0 && index + 1 < args.Length ? args[index + 1] : DefaultManifest);
        }

        public static void Import(string manifestPath)
        {
            var projectRoot = Path.GetFullPath(".");
            var manifestFull = Path.GetFullPath(manifestPath);
            var incomingRoot = Path.GetFullPath("AssetSource/Incoming") + Path.DirectorySeparatorChar;
            if (!manifestFull.StartsWith(incomingRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Batch manifest must be inside AssetSource/Incoming.");
            var manifest = JsonUtility.FromJson<BatchArtManifest>(File.ReadAllText(manifestFull));
            if (manifest == null || manifest.schemaVersion != 1 || manifest.entries == null || manifest.entries.Length == 0)
                throw new InvalidDataException("Batch manifest is empty or unsupported.");

            // Complete validation before copying any asset.
            var destinationSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in manifest.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.assetId) ||
                    string.IsNullOrWhiteSpace(entry.source) || string.IsNullOrWhiteSpace(entry.destination))
                    throw new InvalidDataException("Batch entry has missing fields.");
                var source = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestFull) ?? "", entry.source));
                if (!source.StartsWith(incomingRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(source))
                    throw new InvalidDataException("Missing or unsafe batch source: " + entry.source);
                var dest = entry.destination.Replace('\\', '/');
                if (!dest.StartsWith("Assets/ImmunWar/Art/Batch/", StringComparison.Ordinal) ||
                    !dest.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    dest.Contains("..") || !destinationSet.Add(dest))
                    throw new InvalidDataException("Invalid or duplicate batch destination: " + dest);
                var recordPath = Path.Combine(projectRoot, "Docs/AssetProvenance/records", entry.assetId + ".json");
                if (!File.Exists(recordPath)) throw new InvalidDataException("Missing approved record: " + entry.assetId);
                var record = JsonUtility.FromJson<AssetRecord>(File.ReadAllText(recordPath));
                var hash = AssetRecordValidator.ComputeSha256(source);
                var check = AssetRecordValidator.Validate(record, hash, DateTime.UtcNow);
                if (!check.IsValid || record.runtimePath.Replace('\\', '/') != dest || record.rawSha256 != hash)
                    throw new InvalidDataException("Unapproved or mismatched asset " + entry.assetId + ": " + string.Join("; ", check.Errors));
                var png = File.ReadAllBytes(source);
                if (png.Length < 24 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
                    throw new InvalidDataException("Source is not PNG: " + entry.source);
                var width = ReadBigEndian(png, 16);
                var height = ReadBigEndian(png, 20);
                if (width < 128 || height < 128 || width > 8192 || height > 8192)
                    throw new InvalidDataException("Unexpected PNG size: " + entry.source);
                if (entry.columns < 1 || entry.rows < 1 || entry.columns * entry.rows > 64 ||
                    entry.spriteNames == null || entry.spriteNames.Length != entry.columns * entry.rows)
                    throw new InvalidDataException("Invalid sprite grid or names: " + entry.source);
                if (entry.kind != "map" && entry.kind != "character" && entry.kind != "ui")
                    throw new InvalidDataException("Unknown batch kind: " + entry.kind);
                if (string.IsNullOrWhiteSpace(entry.catalogSlot))
                    throw new InvalidDataException("Missing catalog slot: " + entry.assetId);
            }

            foreach (var entry in manifest.entries)
            {
                var source = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestFull) ?? "", entry.source));
                var destination = entry.destination.Replace('\\', '/');
                var absoluteDestination = Path.GetFullPath(destination);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteDestination) ?? projectRoot);
                if (File.Exists(absoluteDestination) &&
                    AssetRecordValidator.ComputeSha256(absoluteDestination) != AssetRecordValidator.ComputeSha256(source))
                    throw new IOException("Destination already contains different artwork: " + destination);
                File.Copy(source, absoluteDestination, true);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var entry in manifest.entries) ConfigureSprites(entry);
            LinkCatalog(manifest);
            AssetDatabase.SaveAssets();
            Debug.Log("IMMUNEWAR_BATCH_ART_IMPORTED " + manifest.batchId + " entries=" + manifest.entries.Length);
        }

        private static int ReadBigEndian(byte[] png, int offset) =>
            (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];

        private static void ConfigureSprites(BatchArtEntry entry)
        {
            var path = entry.destination.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidDataException("Texture import failed: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spriteImportMode = entry.columns * entry.rows == 1 ? SpriteImportMode.Single : SpriteImportMode.Multiple;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.isReadable = entry.kind == "ui";
            importer.SaveAndReimport();
            if (entry.columns * entry.rows == 1) return;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previousIds = provider.GetSpriteRects().ToDictionary(x => x.name, x => x.spriteID);
            var rects = new SpriteRect[entry.spriteNames.Length];
            var fileIds = new List<SpriteNameFileIdPair>();
            for (var row = 0; row < entry.rows; row++)
            for (var column = 0; column < entry.columns; column++)
            {
                var index = row * entry.columns + column;
                var id = previousIds.TryGetValue(entry.spriteNames[index], out var previousId) ? previousId : GUID.Generate();
                var x0 = Mathf.RoundToInt(column * texture.width / (float)entry.columns);
                var x1 = Mathf.RoundToInt((column + 1) * texture.width / (float)entry.columns);
                var y0 = Mathf.RoundToInt((entry.rows - row - 1) * texture.height / (float)entry.rows);
                var y1 = Mathf.RoundToInt((entry.rows - row) * texture.height / (float)entry.rows);
                if (entry.kind == "ui")
                {
                    var left = x1;
                    var right = x0;
                    var bottom = y1;
                    var top = y0;
                    for (var y = y0; y < y1; y++)
                    for (var x = x0; x < x1; x++)
                    {
                        if (texture.GetPixel(x, y).a < 0.08f) continue;
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y);
                        top = Mathf.Max(top, y);
                    }
                    if (right < left) throw new InvalidDataException("Empty UI sprite cell: " + entry.spriteNames[index]);
                    x0 = Mathf.Max(x0, left - 2);
                    x1 = Mathf.Min(x1, right + 3);
                    y0 = Mathf.Max(y0, bottom - 2);
                    y1 = Mathf.Min(y1, top + 3);
                }
                rects[index] = new SpriteRect
                {
                    name = entry.spriteNames[index],
                    spriteID = id,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    rect = new Rect(x0, y0, x1 - x0, y1 - y0)
                };
                fileIds.Add(new SpriteNameFileIdPair(entry.spriteNames[index], id));
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(fileIds);
            provider.Apply();
            importer.SaveAndReimport();
            var actual = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            foreach (var name in entry.spriteNames)
                if (!actual.Any(sprite => sprite.name == name))
                    throw new InvalidDataException("Sliced sprite missing: " + path + "/" + name);
        }

        private static Sprite Sprite(BatchArtEntry entry, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(entry.destination.Replace('\\', '/')).OfType<Sprite>().FirstOrDefault(x => x.name == name);

        private static AnimationClip Clip(string name, Sprite[] frames, int fps, bool loop)
        {
            if (frames.Any(x => !x)) throw new InvalidDataException("Animation frame is missing: " + name);
            Directory.CreateDirectory(AnimDir);
            var path = AnimDir + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = fps;
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (var i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / (float)fps, value = frames[loop ? 0 : frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip,
                new EditorCurveBinding { path = "", type = typeof(Image), propertyName = "m_Sprite" }, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static RuntimeAnimatorController Controller(string name, AnimationClip idle, AnimationClip attack = null)
        {
            var path = AnimDir + "/" + name + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.states.Select(x => x.state).FirstOrDefault(x => x.name == "Idle") ?? machine.AddState("Idle");
            idleState.motion = idle;
            machine.defaultState = idleState;
            if (attack)
            {
                var attackState = machine.states.Select(x => x.state).FirstOrDefault(x => x.name == "Attack") ?? machine.AddState("Attack");
                attackState.motion = attack;
                var back = attackState.transitions.FirstOrDefault(x => x.destinationState == idleState) ?? attackState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 1f;
                back.duration = 0f;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void LinkCatalog(BatchArtManifest manifest)
        {
            var art = AssetDatabase.LoadAssetAtPath<PlayableArtCatalog>(CatalogPath);
            if (!art) throw new FileNotFoundException("Playable art catalog missing: " + CatalogPath);
            foreach (var entry in manifest.entries)
            {
                if (entry.catalogSlot == "lungMap")
                    art.lungMap = AssetDatabase.LoadAssetAtPath<Sprite>(entry.destination);
                else if (entry.catalogSlot == "brainMap")
                    art.brainMap = AssetDatabase.LoadAssetAtPath<Sprite>(entry.destination);
                else if (entry.catalogSlot == "stomachMap")
                    art.stomachMap = AssetDatabase.LoadAssetAtPath<Sprite>(entry.destination);
                else if (entry.catalogSlot == "uiKit")
                {
                    art.primaryButton = Sprite(entry, "button_primary");
                    art.secondaryButton = Sprite(entry, "button_secondary");
                    art.dangerButton = Sprite(entry, "button_danger");
                    art.panel = Sprite(entry, "panel");
                    art.organCard = Sprite(entry, "organ_card");
                }
                else if (entry.catalogSlot == "macrophage")
                {
                    var frames = entry.spriteNames.Select(x => Sprite(entry, x)).ToArray();
                    art.macrophageIdle = frames.Take(4).ToArray();
                    art.macrophageAttack = frames.Skip(4).Take(4).ToArray();
                    var idle = Clip("Macrophage_Idle", art.macrophageIdle, entry.fps, true);
                    var attack = Clip("Macrophage_Attack", art.macrophageAttack, entry.fps, false);
                    art.macrophageController = Controller("Macrophage_UI", idle, attack);
                }
                else if (entry.catalogSlot == "virus")
                {
                    art.virusMove = entry.spriteNames.Select(x => Sprite(entry, x)).ToArray();
                    var move = Clip("Virus_Move", art.virusMove, entry.fps, true);
                    art.virusController = Controller("Virus_UI", move);
                }
                else if (entry.catalogSlot.StartsWith("def_", StringComparison.Ordinal))
                {
                    if (entry.spriteNames.Length != 8)
                        throw new InvalidDataException("Defender sheet requires four idle and four action frames: " + entry.assetId);
                    var frames = entry.spriteNames.Select(x => Sprite(entry, x)).ToArray();
                    var label = entry.catalogSlot.Substring(4);
                    var idle = Clip(label + "_Idle", frames.Take(4).ToArray(), entry.fps, true);
                    var action = Clip(label + "_Action", frames.Skip(4).ToArray(), entry.fps, false);
                    var item = new UnitAnimationArt
                    {
                        unitId = entry.catalogSlot,
                        idleOrMove = frames.Take(4).ToArray(),
                        action = frames.Skip(4).ToArray(),
                        controller = Controller(label + "_UI", idle, action)
                    };
                    art.defenderAnimations = Upsert(art.defenderAnimations, item);
                }
                else if (entry.catalogSlot.StartsWith("ene_", StringComparison.Ordinal))
                {
                    if (entry.spriteNames.Length != 8)
                        throw new InvalidDataException("Enemy sheet requires eight move frames: " + entry.assetId);
                    var frames = entry.spriteNames.Select(x => Sprite(entry, x)).ToArray();
                    var label = entry.catalogSlot.Substring(4);
                    var move = Clip(label + "_Move", frames, entry.fps, true);
                    art.enemyAnimations = Upsert(art.enemyAnimations, new UnitAnimationArt
                    {
                        unitId = entry.catalogSlot,
                        idleOrMove = frames,
                        controller = Controller(label + "_UI", move)
                    });
                }
                else throw new InvalidDataException("Unsupported catalog slot: " + entry.catalogSlot);
            }
            EditorUtility.SetDirty(art);
        }

        private static UnitAnimationArt[] Upsert(UnitAnimationArt[] existing, UnitAnimationArt replacement)
        {
            var list = existing == null ? new List<UnitAnimationArt>() : existing.Where(x => x != null && x.unitId != replacement.unitId).ToList();
            list.Add(replacement);
            return list.ToArray();
        }
    }
}
