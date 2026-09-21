using System.IO;
using ImmunWar.Core.Config;
using ImmunWar.UI;
using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor
{
    public static class PlayableContentLinker
    {
        [MenuItem("Immune War/Setup/Link Playable Art")]
        public static void LinkPlayableArt()
        {
            Link("Macrophage", "Assets/ImmunWar/Data/Defenders/P0/Macrophage.asset", "Assets/ImmunWar/Prefabs/Defenders/P0/Macrophage.prefab");
            Link("TCell", "Assets/ImmunWar/Data/Defenders/P0/TCell.asset", "Assets/ImmunWar/Prefabs/Defenders/P0/TCell.prefab");
            Link("EnergyCell", "Assets/ImmunWar/Data/Defenders/P0/EnergyCell.asset", "Assets/ImmunWar/Prefabs/Defenders/P0/EnergyCell.prefab");
            Link("BasicVirus", "Assets/ImmunWar/Data/Enemies/P0/BasicVirus.asset", "Assets/ImmunWar/Prefabs/Enemies/P0/BasicVirus.prefab");
            const string artPath = "Assets/ImmunWar/Resources/ImmuneWar/PlayableArtCatalog.asset";
            var art = AssetDatabase.LoadAssetAtPath<PlayableArtCatalog>(artPath);
            if (!art)
            {
                art = ScriptableObject.CreateInstance<PlayableArtCatalog>();
                AssetDatabase.CreateAsset(art, artPath);
            }
            art.hit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ImmunWar/Art/VFX/P0/VFX_Hit.png");
            art.pulse = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ImmunWar/Art/VFX/P0/VFX_TCellPulse.png");
            art.atp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ImmunWar/Art/VFX/P0/VFX_ATP.png");
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
            Debug.Log("IMMUNEWAR_PLAYABLE_ART_LINKED");
        }

        private static void Link(string name, string configPath, string prefabPath)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(configPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!config || !prefab) throw new FileNotFoundException("Missing playable config or prefab: " + name);
            var sprite = prefab.GetComponent<SpriteRenderer>()?.sprite;
            if (!sprite) throw new InvalidDataException("Playable prefab has no sprite: " + prefabPath);
            var presentationPath = "Assets/ImmunWar/Data/Presentation/Playable" + name + ".asset";
            var presentation = AssetDatabase.LoadAssetAtPath<PresentationConfig>(presentationPath);
            if (!presentation)
            {
                presentation = ScriptableObject.CreateInstance<PresentationConfig>();
                presentation.SetIdForEditor("presentation_" + name.ToLowerInvariant());
                AssetDatabase.CreateAsset(presentation, presentationPath);
            }
            presentation.prefab = prefab;
            presentation.icon = sprite;
            EditorUtility.SetDirty(presentation);
            if (config is DefenderConfig defender) defender.presentation = presentation;
            else if (config is EnemyConfig enemy) enemy.presentation = presentation;
            EditorUtility.SetDirty(config);
        }
    }
}
