using System;
using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ImmunWar.Editor.AssetProvenance
{
    public sealed class ApprovedAssetBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            var records = Path.GetFullPath("Docs/AssetProvenance/records");
            if (!Directory.Exists(records)) throw new BuildFailedException("Asset provenance records directory is missing.");
            foreach (var path in Directory.GetFiles(records, "*.json", SearchOption.AllDirectories))
            {
                var record = JsonUtility.FromJson<AssetRecord>(File.ReadAllText(path));
                if (record == null || record.status != "Approved") throw new BuildFailedException("Unapproved asset record: " + path);
            }
        }
    }
}
