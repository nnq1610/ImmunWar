using System;
using System.IO;
using System.Linq;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.Editor
{
    public static class TacticalContentGenerator
    {
        [MenuItem("Immune War/Setup/Generate Tactical Content")]
        public static void Generate()
        {
            GenerateCandidateTextures();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTacticalTextures();

            var infection = Create<StatusEffectConfig>("Assets/ImmunWar/Data/StatusEffects/Infection.asset", "status_infection");
            infection.durationTicks = 180;
            infection.maximumStacks = 3;
            infection.stackPolicy = StatusStackPolicy.Stack;
            infection.damageMultiplier = 1.15f;
            infection.speedMultiplier = .9f;

            var cleanse = Create<StatusEffectConfig>("Assets/ImmunWar/Data/StatusEffects/Cleanse.asset", "status_cleanse");
            cleanse.durationTicks = 1;
            cleanse.maximumStacks = 1;
            cleanse.stackPolicy = StatusStackPolicy.Replace;

            var fever = Create<FeverConfig>("Assets/ImmunWar/Data/Fever/FeverMode.asset", "fever_mode");
            fever.maximumCharge = 100;
            fever.durationTicks = 300;
            fever.damageMultiplier = 1.5f;
            fever.speedMultiplier = 1.25f;

            var resilient = Create<MutationDefinition>("Assets/ImmunWar/Data/Mutations/Resilient.asset", "mutation_resilient");
            resilient.weight = 1f;
            resilient.healthMultiplier = 1.6f;
            resilient.speedMultiplier = .9f;
            resilient.immunityStatusIds = new[] { "status_slow" };

            var swift = Create<MutationDefinition>("Assets/ImmunWar/Data/Mutations/Swift.asset", "mutation_swift");
            swift.weight = 1f;
            swift.healthMultiplier = .85f;
            swift.speedMultiplier = 1.5f;
            swift.immunityStatusIds = Array.Empty<string>();

            var bCellAbility = CreateAbility("Assets/ImmunWar/Data/Abilities/BCellCleanse.asset", "ability_bcell_cleanse", "Cleanse", 1f, 150, cleanse);
            var nkAbility = CreateAbility("Assets/ImmunWar/Data/Abilities/NkBurst.asset", "ability_nk_burst", "MutantBurst", 2f, 120, null);
            var plateletAbility = CreateAbility("Assets/ImmunWar/Data/Abilities/PlateletRepair.asset", "ability_platelet_repair", "RepairAndSlow", 15f, 180, null);

            var bCell = CreateDefender("Assets/ImmunWar/Data/Defenders/P1/BCell.asset", "def_bcell", DefenderRole.Support, 30, 95, 6, 1.1f, 3f, bCellAbility);
            var nk = CreateDefender("Assets/ImmunWar/Data/Defenders/P1/NkCell.asset", "def_nk", DefenderRole.Burst, 45, 105, 24, 1.3f, 2.5f, nkAbility);
            var platelet = CreateDefender("Assets/ImmunWar/Data/Defenders/P1/Platelet.asset", "def_platelet", DefenderRole.Repair, 35, 130, 4, 1.4f, 2.2f, plateletAbility);
            var bacteria = CreateEnemy("Assets/ImmunWar/Data/Enemies/P1/Bacteria.asset", "ene_bacteria", 90, .58f, 12, 9, Array.Empty<MutationDefinition>());
            var mutant = CreateEnemy("Assets/ImmunWar/Data/Enemies/P1/Mutant.asset", "ene_mutant", 120, .7f, 16, 13, new[] { resilient, swift });

            var bCellPrefab = SaveSpritePrefab("Assets/ImmunWar/Art/Defenders/P1/DEF_BCell.png", "Assets/ImmunWar/Prefabs/Defenders/P1/BCell.prefab", "BCell");
            var nkPrefab = SaveSpritePrefab("Assets/ImmunWar/Art/Defenders/P1/DEF_NkCell.png", "Assets/ImmunWar/Prefabs/Defenders/P1/NkCell.prefab", "NkCell");
            var plateletPrefab = SaveSpritePrefab("Assets/ImmunWar/Art/Defenders/P1/DEF_Platelet.png", "Assets/ImmunWar/Prefabs/Defenders/P1/Platelet.prefab", "Platelet");
            var bacteriaPrefab = SaveSpritePrefab("Assets/ImmunWar/Art/Enemies/P1/ENE_Bacteria.png", "Assets/ImmunWar/Prefabs/Enemies/P1/Bacteria.prefab", "Bacteria");
            var mutantPrefab = SaveSpritePrefab("Assets/ImmunWar/Art/Enemies/P1/ENE_Mutant.png", "Assets/ImmunWar/Prefabs/Enemies/P1/Mutant.prefab", "Mutant");

            bCell.presentation = CreatePresentation("Assets/ImmunWar/Data/Presentation/BCell.asset", "presentation_bcell", bCellPrefab, "Assets/ImmunWar/Art/UI/Tactical/UI_Icon_BCell.png", new Color(.35f, .85f, 1f));
            nk.presentation = CreatePresentation("Assets/ImmunWar/Data/Presentation/NkCell.asset", "presentation_nk", nkPrefab, "Assets/ImmunWar/Art/UI/Tactical/UI_Icon_NkCell.png", new Color(.75f, .35f, 1f));
            platelet.presentation = CreatePresentation("Assets/ImmunWar/Data/Presentation/Platelet.asset", "presentation_platelet", plateletPrefab, "Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Platelet.png", new Color(1f, .45f, .55f));
            bacteria.presentation = CreatePresentation("Assets/ImmunWar/Data/Presentation/Bacteria.asset", "presentation_bacteria", bacteriaPrefab, "Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Infection.png", new Color(.55f, 1f, .35f));
            mutant.presentation = CreatePresentation("Assets/ImmunWar/Data/Presentation/Mutant.asset", "presentation_mutant", mutantPrefab, "Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Mutation.png", new Color(1f, .45f, .2f));

            var tacticalPresentation = Create<PresentationConfig>("Assets/ImmunWar/Data/Presentation/TacticalPresentation.asset", "presentation_tactical");
            tacticalPresentation.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Fever.png");
            tacticalPresentation.accent = new Color(1f, .35f, .12f);

            GenerateHudPrefab();
            UpdateCatalog(bCell, nk, platelet, bacteria, mutant, infection, cleanse, resilient, swift, fever);
            AssetDatabase.SaveAssets();
            Debug.Log("IMMUNEWAR_TACTICAL_CONTENT_OK");
        }

        private static void GenerateCandidateTextures()
        {
            WriteDisc("Assets/ImmunWar/Art/Defenders/P1/DEF_BCell.png", new Color32(55, 185, 235, 255), 3);
            WriteDisc("Assets/ImmunWar/Art/Defenders/P1/DEF_NkCell.png", new Color32(160, 75, 235, 255), 7);
            WriteDisc("Assets/ImmunWar/Art/Defenders/P1/DEF_Platelet.png", new Color32(245, 95, 115, 255), 5);
            WriteDisc("Assets/ImmunWar/Art/Enemies/P1/ENE_Bacteria.png", new Color32(115, 210, 65, 255), 4);
            WriteDisc("Assets/ImmunWar/Art/Enemies/P1/ENE_Mutant.png", new Color32(245, 95, 35, 255), 9);
            CopyIcon("DEF_BCell", "UI_Icon_BCell");
            CopyIcon("DEF_NkCell", "UI_Icon_NkCell");
            CopyIcon("DEF_Platelet", "UI_Icon_Platelet");
            WriteDisc("Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Infection.png", new Color32(125, 220, 55, 255), 6, 128);
            WriteDisc("Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Mutation.png", new Color32(245, 105, 35, 255), 8, 128);
            WriteDisc("Assets/ImmunWar/Art/UI/Tactical/UI_Icon_Fever.png", new Color32(255, 55, 20, 255), 12, 128);
            WriteDisc("Assets/ImmunWar/Art/VFX/Tactical/VFX_Infection.png", new Color32(120, 245, 55, 255), 10);
            WriteDisc("Assets/ImmunWar/Art/VFX/Tactical/VFX_Fever.png", new Color32(255, 65, 15, 255), 14);
        }

        private static void WriteDisc(string path, Color32 color, int lobes, int size = 512)
        {
            if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Art");
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var center = size * .5f;
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var dx = x - center; var dy = y - center; var angle = Mathf.Atan2(dy, dx);
                var radius = center * (.58f + .08f * Mathf.Sin(angle * lobes)); var distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > radius) pixels[y * size + x] = new Color32(0, 0, 0, 0);
                else { var light = (byte)Mathf.Clamp(color.r + (1f - distance / radius) * 30f, 0f, 255f); pixels[y * size + x] = new Color32(light, color.g, color.b, color.a); }
            }
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }

        private static void CopyIcon(string sourceName, string iconName)
        {
            var source = $"Assets/ImmunWar/Art/Defenders/P1/{sourceName}.png";
            var destination = $"Assets/ImmunWar/Art/UI/Tactical/{iconName}.png";
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? "Assets/ImmunWar/Art/UI/Tactical");
            if (!File.Exists(destination) && File.Exists(source)) File.Copy(source, destination);
        }

        private static void ConfigureTacticalTextures()
        {
            foreach (var path in AssetDatabase.GetAllAssetPaths().Where(path => path.Contains("/P1/") || path.Contains("/Tactical/")))
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = path.Contains("/UI/") ? 256 : 512; importer.SaveAndReimport();
            }
        }

        private static AbilityConfig CreateAbility(string path, string id, string type, float magnitude, int cooldown, StatusEffectConfig status)
        {
            var asset = Create<AbilityConfig>(path, id); asset.abilityType = type; asset.magnitude = magnitude; asset.cooldownTicks = cooldown; asset.appliedStatus = status; EditorUtility.SetDirty(asset); return asset;
        }

        private static DefenderConfig CreateDefender(string path, string id, DefenderRole role, int cost, int health, float damage, float interval, float range, AbilityConfig ability)
        {
            var asset = Create<DefenderConfig>(path, id); asset.displayNameKey = id; asset.role = role; asset.atpCost = cost; asset.maxHealth = health; asset.attackDamage = damage; asset.attackInterval = interval; asset.range = range; asset.ability = ability; EditorUtility.SetDirty(asset); return asset;
        }

        private static EnemyConfig CreateEnemy(string path, string id, int health, float speed, int damage, int reward, MutationDefinition[] mutations)
        {
            var asset = Create<EnemyConfig>(path, id); asset.displayNameKey = id; asset.maxHealth = health; asset.moveSpeed = speed; asset.organDamage = damage; asset.atpReward = reward; asset.mutations = mutations; EditorUtility.SetDirty(asset); return asset;
        }

        private static PresentationConfig CreatePresentation(string path, string id, GameObject prefab, string iconPath, Color accent)
        {
            var asset = Create<PresentationConfig>(path, id); asset.prefab = prefab; asset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath); asset.accent = accent; EditorUtility.SetDirty(asset); return asset;
        }

        private static T Create<T>(string path, string id) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Data");
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            if (asset is GameConfig config) config.SetIdForEditor(id);
            EditorUtility.SetDirty(asset); return asset;
        }

        private static GameObject SaveSpritePrefab(string spritePath, string prefabPath, string objectName)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath) ?? "Assets/ImmunWar/Prefabs");
            var go = new GameObject(objectName); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath); renderer.sortingOrder = 10;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath); UnityEngine.Object.DestroyImmediate(go); return prefab;
        }

        private static void GenerateHudPrefab()
        {
            Directory.CreateDirectory("Assets/ImmunWar/Prefabs/UI");
            var root = new GameObject("TacticalHudExtension", typeof(RectTransform), typeof(TacticalHudController));
            var meter = CreateUi<Slider>(root.transform, "FeverMeter", new Vector2(.5f, .08f), new Vector2(360, 36)); meter.minValue = 0; meter.maxValue = 1;
            var button = CreateUi<Button>(root.transform, "FeverButton", new Vector2(.72f, .08f), new Vector2(160, 48)); button.gameObject.AddComponent<Image>().color = new Color(1f, .22f, .08f, .9f);
            var label = CreateUi<Text>(root.transform, "TacticalStatus", new Vector2(.5f, .14f), new Vector2(480, 48)); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.text = "Tactical status";
            var serialized = new SerializedObject(root.GetComponent<TacticalHudController>()); serialized.FindProperty("feverMeter").objectReferenceValue = meter; serialized.FindProperty("feverButton").objectReferenceValue = button; serialized.FindProperty("statusLabel").objectReferenceValue = label; serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/ImmunWar/Prefabs/UI/TacticalHudExtension.prefab"); UnityEngine.Object.DestroyImmediate(root);
        }

        private static T CreateUi<T>(Transform parent, string name, Vector2 anchor, Vector2 size) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; return go.AddComponent<T>();
        }

        private static void UpdateCatalog(DefenderConfig bCell, DefenderConfig nk, DefenderConfig platelet, EnemyConfig bacteria, EnemyConfig mutant, StatusEffectConfig infection, StatusEffectConfig cleanse, MutationDefinition resilient, MutationDefinition swift, FeverConfig fever)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/ImmunWar/Resources/ImmuneWar/GameCatalog.asset");
            if (!catalog) catalog = Create<GameCatalog>("Assets/ImmunWar/Resources/ImmuneWar/GameCatalog.asset", null);
            catalog.defenders = AppendUnique(catalog.defenders, bCell, nk, platelet); catalog.enemies = AppendUnique(catalog.enemies, bacteria, mutant); catalog.statusEffects = AppendUnique(catalog.statusEffects, infection, cleanse); catalog.mutations = AppendUnique(catalog.mutations, resilient, swift); catalog.fever = fever; EditorUtility.SetDirty(catalog);
        }

        private static T[] AppendUnique<T>(T[] existing, params T[] values) where T : GameConfig
        {
            return (existing ?? Array.Empty<T>()).Concat(values).Where(value => value).GroupBy(value => value.Id).Select(group => group.Last()).ToArray();
        }
    }
}
