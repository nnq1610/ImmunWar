using System;
using System.IO;
using System.Linq;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImmunWar.Editor
{
    public static class CampaignContentGenerator
    {
        [MenuItem("Immune War/Setup/Generate Campaign Content")]
        public static void Generate()
        {
            WriteOrganTexture("Assets/ImmunWar/Art/Environment/Brain/ENV_Brain_Path.png", new Color32(180, 95, 210, 255));
            WriteOrganTexture("Assets/ImmunWar/Art/Environment/Stomach/ENV_Stomach_Path.png", new Color32(225, 110, 95, 255));
            WriteOrganTexture("Assets/ImmunWar/Art/Enemies/Boss/ENE_SuperPathogen.png", new Color32(145, 35, 190, 255));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); ConfigureTextures();

            var existingCatalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/ImmunWar/Resources/ImmuneWar/GameCatalog.asset");
            var baseEnemy = existingCatalog && existingCatalog.enemies != null ? existingCatalog.enemies.FirstOrDefault() : null;
            var brain = CreateMap("brain", "map_stomach", baseEnemy, new[] { new Vector2(-8, -2), new Vector2(-3, 2), new Vector2(2, -1), new Vector2(8, 1) });
            var stomach = CreateMap("stomach", null, baseEnemy, new[] { new Vector2(-8, 1), new Vector2(-4, -2), Vector2.zero, new Vector2(5, 2), new Vector2(8, -1) });

            var spray = CreateAbility("Assets/ImmunWar/Data/Abilities/BossSporeSpray.asset", "ability_boss_spore_spray", "SporeSpray", 1.25f);
            var surge = CreateAbility("Assets/ImmunWar/Data/Abilities/BossMutationSurge.asset", "ability_boss_mutation_surge", "MutationSurge", 1.6f);
            var phase1 = CreatePhase("Assets/ImmunWar/Data/Bosses/SuperPathogenPhases.asset", "boss_phase_1", 1f, 0, spray);
            var phase2 = CreatePhase("Assets/ImmunWar/Data/Bosses/SuperPathogenPhase2.asset", "boss_phase_2", .65f, 20, spray, surge);
            var phase3 = CreatePhase("Assets/ImmunWar/Data/Bosses/SuperPathogenPhase3.asset", "boss_phase_3", .3f, 30, surge);
            var boss = Create<EnemyConfig>("Assets/ImmunWar/Data/Enemies/SuperPathogen.asset", "ene_super_pathogen"); boss.displayNameKey = "enemy.super_pathogen"; boss.maxHealth = 1200; boss.moveSpeed = .32f; boss.organDamage = 100; boss.atpReward = 100; boss.bossPhases = new[] { phase1, phase2, phase3 }; EditorUtility.SetDirty(boss);
            var bossPrefab = SaveSpritePrefab("Assets/ImmunWar/Art/Enemies/Boss/ENE_SuperPathogen.png", "Assets/ImmunWar/Prefabs/Enemies/Boss/SuperPathogen.prefab", "SuperPathogen");
            var presentation = Create<PresentationConfig>("Assets/ImmunWar/Data/Presentation/SuperPathogen.asset", "presentation_super_pathogen"); presentation.prefab = bossPrefab; presentation.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ImmunWar/Art/Enemies/Boss/ENE_SuperPathogen.png"); presentation.accent = new Color(.7f, .15f, .9f); boss.presentation = presentation;

            if (existingCatalog) { existingCatalog.maps = Append(existingCatalog.maps, brain, stomach); existingCatalog.enemies = Append(existingCatalog.enemies, boss); EditorUtility.SetDirty(existingCatalog); }
            GenerateMapSelectPrefab(); ComposeMainMenu(); AssetDatabase.SaveAssets(); Debug.Log("IMMUNEWAR_CAMPAIGN_CONTENT_OK");
        }

        private static OrganMapConfig CreateMap(string organ, string nextMapId, EnemyConfig enemy, Vector2[] points)
        {
            var title = char.ToUpperInvariant(organ[0]) + organ.Substring(1); var route = Create<RouteConfig>($"Assets/ImmunWar/Data/Maps/{title}Route.asset", $"route_{organ}_main"); route.waypoints = points;
            var nodes = new DefenseNodeConfig[5]; for (var i = 0; i < nodes.Length; i++) { nodes[i] = Create<DefenseNodeConfig>($"Assets/ImmunWar/Data/Maps/{title}Node{i + 1}.asset", $"node_{organ}_{i + 1}"); nodes[i].routeId = route.Id; nodes[i].routeProgress = (i + 1f) / 6f; nodes[i].position = Vector2.Lerp(points[0], points[points.Length - 1], nodes[i].routeProgress) + Vector2.up * (i % 2 == 0 ? 1.5f : -1.5f); }
            var waves = Create<WaveSet>($"Assets/ImmunWar/Data/Waves/{title}Waves.asset", $"waves_{organ}"); waves.waves = new[] { new Wave { groups = enemy ? new[] { new SpawnGroup { enemy = enemy, routeId = route.Id, count = organ == "brain" ? 9 : 12, intervalTicks = 24 } } : Array.Empty<SpawnGroup>() } };
            var map = Create<OrganMapConfig>($"Assets/ImmunWar/Data/Maps/{title}Map.asset", $"map_{organ}"); map.displayNameKey = $"map.{organ}"; map.nextMapId = nextMapId; map.startingAtp = 120; map.maximumVitality = 100; map.routes = new[] { route }; map.nodes = nodes; map.waveSet = waves; EditorUtility.SetDirty(route); EditorUtility.SetDirty(waves); EditorUtility.SetDirty(map); return map;
        }

        private static AbilityConfig CreateAbility(string path, string id, string type, float magnitude) { var value = Create<AbilityConfig>(path, id); value.abilityType = type; value.magnitude = magnitude; value.cooldownTicks = 90; return value; }
        private static BossPhaseConfig CreatePhase(string path, string id, float threshold, int invulnerability, params AbilityConfig[] abilities) { var value = Create<BossPhaseConfig>(path, id); value.healthThreshold = threshold; value.transitionInvulnerabilityTicks = invulnerability; value.abilities = abilities; return value; }
        private static T Create<T>(string path, string id) where T : ScriptableObject { Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Data"); var value = AssetDatabase.LoadAssetAtPath<T>(path); if (!value) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); } if (value is GameConfig config) config.SetIdForEditor(id); EditorUtility.SetDirty(value); return value; }
        private static T[] Append<T>(T[] source, params T[] values) where T : GameConfig => (source ?? Array.Empty<T>()).Concat(values).Where(x => x).GroupBy(x => x.Id).Select(x => x.Last()).ToArray();

        private static void WriteOrganTexture(string path, Color32 color)
        {
            if (File.Exists(path)) return; Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Art"); const int size = 512; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) { var dx = x - 256f; var dy = y - 256f; var radius = 170f + 25f * Mathf.Sin(Mathf.Atan2(dy, dx) * 7f); pixels[y * size + x] = dx * dx + dy * dy < radius * radius ? color : new Color32(0, 0, 0, 0); }
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
        private static void ConfigureTextures() { foreach (var path in AssetDatabase.GetAllAssetPaths().Where(x => x.Contains("/Environment/Brain/") || x.Contains("/Environment/Stomach/") || x.Contains("/Enemies/Boss/"))) if (AssetImporter.GetAtPath(path) is TextureImporter importer) { importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport(); } }
        private static GameObject SaveSpritePrefab(string spritePath, string prefabPath, string name) { Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? "Assets/ImmunWar/Prefabs"); var go = new GameObject(name); go.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath); var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath); UnityEngine.Object.DestroyImmediate(go); return prefab; }
        private static void GenerateMapSelectPrefab() { Directory.CreateDirectory("Assets/ImmunWar/Prefabs/UI"); var root = new GameObject("MapSelectScreen", typeof(RectTransform), typeof(MapSelectController)); for (var i = 0; i < 3; i++) { var card = new GameObject(new[] { "Lung", "Brain", "Stomach" }[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); card.transform.SetParent(root.transform, false); ((RectTransform)card.transform).anchoredPosition = new Vector2((i - 1) * 300, 0); card.GetComponent<Image>().color = new Color(.12f, .25f + i * .08f, .35f, .9f); } PrefabUtility.SaveAsPrefabAsset(root, "Assets/ImmunWar/Prefabs/UI/MapSelectScreen.prefab"); UnityEngine.Object.DestroyImmediate(root); }
        private static void ComposeMainMenu() { const string path = "Assets/ImmunWar/Scenes/MainMenu.unity"; var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single); var root = GameObject.Find("MainMenuRuntime") ?? new GameObject("MainMenuRuntime"); if (!root.GetComponent<MainMenuController>()) root.AddComponent<MainMenuController>(); EditorSceneManager.SaveScene(scene, path); }
    }
}
