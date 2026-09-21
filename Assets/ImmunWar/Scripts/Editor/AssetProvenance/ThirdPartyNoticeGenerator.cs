using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor.AssetProvenance
{
    public static class ThirdPartyNoticeGenerator
    {
        [MenuItem("Immune War/Assets/Generate Third-Party Notices")]
        public static void Generate()
        {
            const string records = "Docs/AssetProvenance/records"; const string output = "Assets/ImmunWar/StreamingAssets/THIRD_PARTY_NOTICES.txt"; var builder = new StringBuilder("IMMUNE WAR - THIRD-PARTY ASSET NOTICES\nGenerated from approved provenance records.\n\n");
            if (Directory.Exists(records)) foreach (var path in Directory.GetFiles(records, "*.json", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal)) { AssetRecord record; try { record = JsonUtility.FromJson<AssetRecord>(File.ReadAllText(path)); } catch { continue; } if (record == null || record.status != "Approved" || record.sourceType == "in_house") continue; builder.AppendLine($"{record.assetId} - {record.purpose}"); builder.AppendLine($"Provider: {record.creatorOrProvider}"); if (!string.IsNullOrWhiteSpace(record.rights?.attribution) && record.rights.attribution != "N/A") builder.AppendLine($"Attribution: {record.rights.attribution}"); if (!string.IsNullOrWhiteSpace(record.evidence?.urlOrOrderId)) builder.AppendLine($"Source/terms: {record.evidence.urlOrOrderId}"); builder.AppendLine(); }
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, builder.ToString()); AssetDatabase.ImportAsset(output); Debug.Log("IMMUNEWAR_NOTICES_OK");
        }
    }
}
