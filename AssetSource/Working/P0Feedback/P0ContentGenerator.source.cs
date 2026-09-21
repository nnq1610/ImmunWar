using System;
using System.IO;
using ImmunWar.Core.Config;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace ImmunWar.Editor
{
    public static class P0ContentGenerator
    {
        [MenuItem("Immune War/Setup/Generate P0 Content")]
        public static void Generate()
        {
            GenerateTextures();
            GenerateAudio();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters();
            GenerateData();
            GeneratePrefabs();
            GenerateAnimationAndAtlas();
            GenerateHudPrefab();
            ComposeBattleScene();
            AssetDatabase.SaveAssets();
            Debug.Log("IMMUNEWAR_P0_CONTENT_OK");
        }

        public static void FinalizePresentation()
        {
            GenerateAnimationAndAtlas();
            GenerateHudPrefab();
            ComposeBattleScene();
            AssetDatabase.SaveAssets();
            Debug.Log("IMMUNEWAR_P0_PRESENTATION_OK");
        }

        private static void GenerateTextures()
        {
            WriteTexture("Assets/ImmunWar/Art/Environment/Lung/ENV_Vessel_Straight.png", 512, 512, (x, y) => VesselPixel(x, y, 512, false, false));
            WriteTexture("Assets/ImmunWar/Art/Environment/Lung/ENV_Vessel_Curve.png", 512, 512, (x, y) => VesselPixel(x, y, 512, true, false));
            WriteTexture("Assets/ImmunWar/Art/Environment/Lung/ENV_Vessel_Junction3.png", 512, 512, (x, y) => VesselPixel(x, y, 512, false, true));
            WriteTexture("Assets/ImmunWar/Art/Environment/Lung/ENV_DefenseNode.png", 256, 256, (x, y) => RingPixel(x, y, 128, 78, 18, new Color32(45, 225, 235, 220)));
            WriteTexture("Assets/ImmunWar/Art/VFX/P0/VFX_TCellPulse.png", 256, 256, (x, y) => GlowPixel(x, y, 128, new Color32(40, 235, 255, 255)));
            WriteTexture("Assets/ImmunWar/Art/VFX/P0/VFX_Hit.png", 256, 256, HitPixel);
            WriteTexture("Assets/ImmunWar/Art/VFX/P0/VFX_ATP.png", 256, 256, (x, y) => GlowPixel(x, y, 128, new Color32(255, 205, 40, 255)));
            CopyIfPresent("Assets/ImmunWar/Art/Defenders/P0/DEF_Macrophage.png", "Assets/ImmunWar/Art/UI/P0/UI_Icon_Macrophage.png");
            CopyIfPresent("Assets/ImmunWar/Art/Defenders/P0/DEF_TCell.png", "Assets/ImmunWar/Art/UI/P0/UI_Icon_TCell.png");
            CopyIfPresent("Assets/ImmunWar/Art/Defenders/P0/DEF_EnergyCell.png", "Assets/ImmunWar/Art/UI/P0/UI_Icon_EnergyCell.png");
            WriteTexture("Assets/ImmunWar/Art/UI/P0/UI_ATP.png", 128, 128, (x, y) => GlowPixel(x, y, 64, new Color32(255, 210, 45, 255)));
        }

        private static Color32 VesselPixel(int x, int y, int size, bool curve, bool junction)
        {
            var nx = (x - size * .5f) / size;
            var ny = (y - size * .5f) / size;
            float distance;
            if (curve) distance = Mathf.Abs(Mathf.Sqrt((nx + .5f) * (nx + .5f) + (ny - .5f) * (ny - .5f)) - .5f);
            else if (junction) distance = Mathf.Min(Mathf.Abs(ny), Mathf.Min(Mathf.Abs(ny - nx), Mathf.Abs(ny + nx)));
            else distance = Mathf.Abs(ny);
            if (distance > .16f) return new Color32(0, 0, 0, 0);
            var edge = Mathf.InverseLerp(.16f, .09f, distance);
            var core = Mathf.InverseLerp(.1f, 0f, distance);
            return new Color(0.45f + core * .35f, 0.06f + core * .08f, 0.14f + core * .12f, edge * .92f);
        }

        private static Color32 RingPixel(int x, int y, int center, int radius, int thickness, Color32 color)
        {
            var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            var alpha = Mathf.Clamp01(1f - Mathf.Abs(distance - radius) / thickness);
            return new Color32(color.r, color.g, color.b, (byte)(color.a * alpha));
        }

        private static Color32 GlowPixel(int x, int y, int center, Color32 color)
        {
            var normalized = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
            if (normalized >= .9f) return new Color32(0, 0, 0, 0);
            var alpha = Mathf.Pow(1f - normalized / .9f, 2f);
            return new Color32(color.r, color.g, color.b, (byte)(255 * alpha));
        }

        private static Color32 HitPixel(int x, int y)
        {
            var dx = x - 128f; var dy = y - 128f; var radius = Mathf.Sqrt(dx * dx + dy * dy); var angle = Mathf.Atan2(dy, dx);
            var spokes = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 6f)), 14f);
            var alpha = Mathf.Clamp01((1f - radius / (70f + spokes * 50f)) * (0.35f + spokes));
            return new Color32(210, 250, 255, (byte)(255 * alpha));
        }

        private static void WriteTexture(string path, int width, int height, Func<int, int, Color32> pixel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Art");
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var colors = new Color32[width * height];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) colors[y * width + x] = pixel(x, y);
            texture.SetPixels32(colors); texture.Apply(false, false); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void CopyIfPresent(string source, string destination)
        {
            if (!File.Exists(source)) return; Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? "Assets/ImmunWar/Art/UI"); File.Copy(source, destination, true);
        }

        private static void ConfigureImporters()
        {
            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/ImmunWar/Art/" ) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.maxTextureSize = path.Contains("/UI/") ? 256 : path.Contains("/Defenders/") || path.Contains("/Enemies/") ? 512 : 1024;
                importer.SaveAndReimport();
            }
        }

        private static void GenerateAudio()
        {
            WriteTone("Assets/ImmunWar/Audio/SFX/P0/SFX_TCell_Attack_01.wav", 0.32f, 620f, 980f);
            WriteTone("Assets/ImmunWar/Audio/SFX/P0/SFX_Virus_Death_01.wav", 0.42f, 260f, 90f);
        }

        private static void WriteTone(string path, float seconds, float startFrequency, float endFrequency)
        {
            const int sampleRate = 48000; var count = Mathf.CeilToInt(sampleRate * seconds); var bytes = new byte[44 + count * 2];
            void WriteText(int offset, string value) { for (var i = 0; i < value.Length; i++) bytes[offset + i] = (byte)value[i]; }
            void WriteInt(int offset, int value) { var data = BitConverter.GetBytes(value); Buffer.BlockCopy(data, 0, bytes, offset, data.Length); }
            void WriteShort(int offset, short value) { var data = BitConverter.GetBytes(value); Buffer.BlockCopy(data, 0, bytes, offset, data.Length); }
            WriteText(0, "RIFF"); WriteInt(4, 36 + count * 2); WriteText(8, "WAVE"); WriteText(12, "fmt "); WriteInt(16, 16); WriteShort(20, 1); WriteShort(22, 1);
            WriteInt(24, sampleRate); WriteInt(28, sampleRate * 2); WriteShort(32, 2); WriteShort(34, 16); WriteText(36, "data"); WriteInt(40, count * 2);
            double phase = 0d;
            for (var i = 0; i < count; i++) { var t = i / (float)Math.Max(1, count - 1); var frequency = Mathf.Lerp(startFrequency, endFrequency, t); phase += Math.PI * 2d * frequency / sampleRate; var envelope = Mathf.Sin(Mathf.PI * t); var sample = (short)(Math.Sin(phase) * envelope * 12000); WriteShort(44 + i * 2, sample); }
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Audio/SFX"); File.WriteAllBytes(path, bytes);
        }

        private static void GenerateData()
        {
            var macrophage = CreateDefender("Assets/ImmunWar/Data/Defenders/P0/Macrophage.asset", "def_macrophage", DefenderRole.Blocker, 30, 240, 8, 1.1f, 1.2f);
            var tcell = CreateDefender("Assets/ImmunWar/Data/Defenders/P0/TCell.asset", "def_tcell", DefenderRole.Damage, 25, 90, 18, .7f, 3.2f);
            var energy = CreateDefender("Assets/ImmunWar/Data/Defenders/P0/EnergyCell.asset", "def_energy", DefenderRole.Economy, 35, 80, 0, 1f, 0f);
            var virus = CreateEnemy("Assets/ImmunWar/Data/Enemies/P0/BasicVirus.asset", "ene_virus", 45, .75f, 8, 6);
            var route = Create<RouteConfig>("Assets/ImmunWar/Data/Maps/LungRoute.asset", "route_lung_main"); route.waypoints = new[] { new Vector2(-8, 0), new Vector2(-4, 1.5f), Vector2.zero, new Vector2(4, -1.2f), new Vector2(8, 0) }; EditorUtility.SetDirty(route);
            var nodes = new DefenseNodeConfig[6];
            for (var i = 0; i < nodes.Length; i++) { nodes[i] = Create<DefenseNodeConfig>($"Assets/ImmunWar/Data/Maps/LungNode{i + 1}.asset", $"node_lung_{i + 1}"); nodes[i].position = new Vector2(-5f + i * 2f, i % 2 == 0 ? 2f : -2f); nodes[i].routeId = route.Id; nodes[i].routeProgress = (i + 1f) / (nodes.Length + 1f); EditorUtility.SetDirty(nodes[i]); }
            var waves = Create<WaveSet>("Assets/ImmunWar/Data/Waves/LungP0Waves.asset", "waves_lung_p0"); waves.waves = new[] { new Wave { groups = new[] { new SpawnGroup { enemy = virus, routeId = route.Id, count = 5, intervalTicks = 30 } } }, new Wave { groups = new[] { new SpawnGroup { enemy = virus, routeId = route.Id, count = 8, intervalTicks = 24 } } } }; EditorUtility.SetDirty(waves);
            var map = Create<OrganMapConfig>("Assets/ImmunWar/Data/Maps/LungMap.asset", "map_lung"); map.displayNameKey = "map.lung"; map.startingAtp = 100; map.maximumVitality = 100; map.routes = new[] { route }; map.nodes = nodes; map.waveSet = waves; map.nextMapId = "map_brain"; EditorUtility.SetDirty(map);
            var catalog = Create<GameCatalog>("Assets/ImmunWar/Resources/ImmuneWar/GameCatalog.asset", null); catalog.defenders = new[] { macrophage, tcell, energy }; catalog.enemies = new[] { virus }; catalog.maps = new[] { map }; EditorUtility.SetDirty(catalog);
        }

        private static DefenderConfig CreateDefender(string path, string id, DefenderRole role, int cost, int health, float damage, float interval, float range)
        { var asset = Create<DefenderConfig>(path, id); asset.displayNameKey = id; asset.role = role; asset.atpCost = cost; asset.maxHealth = health; asset.attackDamage = damage; asset.attackInterval = interval; asset.range = range; EditorUtility.SetDirty(asset); return asset; }
        private static EnemyConfig CreateEnemy(string path, string id, int health, float speed, int organDamage, int reward)
        { var asset = Create<EnemyConfig>(path, id); asset.displayNameKey = id; asset.maxHealth = health; asset.moveSpeed = speed; asset.organDamage = organDamage; asset.atpReward = reward; EditorUtility.SetDirty(asset); return asset; }
        private static T Create<T>(string path, string id) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Data"); var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            if (asset is GameConfig config && id != null) config.SetIdForEditor(id); return asset;
        }

        private static void GeneratePrefabs()
        {
            SaveSpritePrefab("Assets/ImmunWar/Art/Defenders/P0/DEF_Macrophage.png", "Assets/ImmunWar/Prefabs/Defenders/P0/Macrophage.prefab", "Macrophage");
            SaveSpritePrefab("Assets/ImmunWar/Art/Defenders/P0/DEF_TCell.png", "Assets/ImmunWar/Prefabs/Defenders/P0/TCell.prefab", "TCell");
            SaveSpritePrefab("Assets/ImmunWar/Art/Defenders/P0/DEF_EnergyCell.png", "Assets/ImmunWar/Prefabs/Defenders/P0/EnergyCell.prefab", "EnergyCell");
            SaveSpritePrefab("Assets/ImmunWar/Art/Enemies/P0/ENE_Virus.png", "Assets/ImmunWar/Prefabs/Enemies/P0/BasicVirus.prefab", "BasicVirus");
            SaveSpritePrefab("Assets/ImmunWar/Art/VFX/P0/VFX_TCellPulse.png", "Assets/ImmunWar/Prefabs/VFX/P0/TCellPulse.prefab", "TCellPulse");
            SaveSpritePrefab("Assets/ImmunWar/Art/VFX/P0/VFX_Hit.png", "Assets/ImmunWar/Prefabs/VFX/P0/HitBurst.prefab", "HitBurst");
        }

        private static void GenerateAnimationAndAtlas()
        {
            Directory.CreateDirectory("Assets/ImmunWar/Art/Atlases");
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/ImmunWar/Art/Atlases/P0.spriteatlas");
            if (!atlas) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, "Assets/ImmunWar/Art/Atlases/P0.spriteatlas"); }
            SpriteAtlasExtensions.Add(atlas, new UnityEngine.Object[]
            {
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ImmunWar/Art/Defenders/P0"),
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ImmunWar/Art/Enemies/P0"),
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ImmunWar/Art/Environment/Lung"),
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ImmunWar/Art/VFX/P0")
            });
            var packing = atlas.GetPackingSettings(); packing.enableRotation = false; packing.enableTightPacking = true; packing.padding = 4; atlas.SetPackingSettings(packing); EditorUtility.SetDirty(atlas);
            CreateIdleController("Assets/ImmunWar/Art/Animations/Defenders/P0Defender.controller", "Assets/ImmunWar/Art/Animations/Defenders/P0Idle.anim");
            CreateIdleController("Assets/ImmunWar/Art/Animations/Enemies/P0Enemy.controller", "Assets/ImmunWar/Art/Animations/Enemies/P0Idle.anim");
        }

        private static void CreateIdleController(string controllerPath, string clipPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(controllerPath) ?? "Assets/ImmunWar/Art/Animations");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (!clip) { clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(clipPath), frameRate = 30f }; var curve = AnimationCurve.EaseInOut(0f, .98f, 1f, 1.02f); clip.SetCurve("", typeof(Transform), "m_LocalScale.x", curve); clip.SetCurve("", typeof(Transform), "m_LocalScale.y", curve); AssetDatabase.CreateAsset(clip, clipPath); }
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var state = controller.layers[0].stateMachine.states.Length == 0 ? controller.layers[0].stateMachine.AddState("Idle") : controller.layers[0].stateMachine.states[0].state;
            state.motion = clip; controller.layers[0].stateMachine.defaultState = state; EditorUtility.SetDirty(controller);
        }

        private static void GenerateHudPrefab()
        {
            Directory.CreateDirectory("Assets/ImmunWar/Prefabs/UI");
            var root = new GameObject("P0BattleHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ImmunWar.UI.BattleHudController));
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.02f, .07f, .11f, .2f);
            foreach (var item in new[] { ("ATP", new Vector2(.08f, .94f)), ("Vitality", new Vector2(.5f, .94f)), ("Wave", new Vector2(.88f, .94f)) })
            {
                var go = new GameObject(item.Item1, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); go.transform.SetParent(root.transform, false); var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = item.Item2; r.sizeDelta = new Vector2(320, 60); var t = go.GetComponent<Text>(); t.text = item.Item1; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 28; t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            }
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/ImmunWar/Prefabs/UI/P0BattleHud.prefab"); UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ComposeBattleScene()
        {
            const string path = "Assets/ImmunWar/Scenes/Battle.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var root = GameObject.Find("BattleRuntime") ?? new GameObject("BattleRuntime");
            if (!root.GetComponent<ImmunWar.Battle.BattleSceneInstaller>()) root.AddComponent<ImmunWar.Battle.BattleSceneInstaller>();
            if (!root.GetComponent<ImmunWar.Presentation.PoolRegistry>()) root.AddComponent<ImmunWar.Presentation.PoolRegistry>();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, path);
        }

        private static void SaveSpritePrefab(string spritePath, string prefabPath, string name)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? "Assets/ImmunWar/Prefabs"); var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath); var go = new GameObject(name); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 10; go.transform.localScale = Vector3.one * .8f; PrefabUtility.SaveAsPrefabAsset(go, prefabPath); UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
