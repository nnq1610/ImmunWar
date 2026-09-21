using System;
using System.Collections.Generic;
using System.IO;
using ImmunWar.Editor.AssetProvenance;
using NUnit.Framework;
using UnityEngine;

namespace ImmunWar.Tests.EditMode
{
    [Serializable] internal sealed class CoverageManifest { public CoverageEntry[] assets; }
    [Serializable] internal sealed class CoverageEntry { public string assetId; public string runtimePath; }
    public sealed class AssetManifestCoverageTests
    {
        [Test]
        public void All51ManifestAssetsHaveApprovedRuntimeMappingAndValidRecord()
        {
            var map=JsonUtility.FromJson<CoverageManifest>(File.ReadAllText("Docs/AssetProvenance/manifest-path-map.json"));Assert.That(map.assets,Has.Length.EqualTo(51));var records=new Dictionary<string,AssetRecord>();foreach(var path in Directory.GetFiles("Docs/AssetProvenance/records","*.json",SearchOption.AllDirectories)){var record=JsonUtility.FromJson<AssetRecord>(File.ReadAllText(path));if(record?.assetId!=null)records[record.assetId]=record;}
            foreach(var entry in map.assets){Assert.That(File.Exists(entry.runtimePath),Is.True,$"Missing runtime asset {entry.assetId}");Assert.That(records.TryGetValue(entry.assetId,out var record),Is.True,$"Missing record {entry.assetId}");var hash=AssetRecordValidator.ComputeSha256(entry.runtimePath);var result=AssetRecordValidator.Validate(record,hash,DateTime.UtcNow);Assert.That(result.IsValid,Is.True,$"{entry.assetId}: {string.Join("; ",result.Errors)}");}
        }
    }
}
