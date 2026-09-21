using ImmunWar.Core.Config;
using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor
{
    public static class ConfigValidationMenu
    {
        [MenuItem("Immune War/Validate/Game Catalog")]
        public static void ValidateSelectedCatalog()
        {
            var catalog = Selection.activeObject as GameCatalog;
            if (catalog == null)
            {
                var guids = AssetDatabase.FindAssets("t:GameCatalog");
                if (guids.Length > 0) catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            var result = ConfigValidator.Validate(catalog);
            if (!result.IsValid) throw new System.InvalidOperationException(string.Join("\n", result.Errors));
            Debug.Log("IMMUNEWAR_CATALOG_VALID");
        }
    }
}

