using System;

namespace ImmunWar.Editor.AssetProvenance
{
    [Serializable]
    public sealed class RightsRecord
    {
        public string commercialUse = "Allowed";
        public string modification = "Allowed";
        public string embeddedRedistribution = "Allowed";
        public string attribution = "N/A";
        public string aiInputUse = "N/A";
        public string territoryOrPlatformLimits;
    }

    [Serializable]
    public sealed class EvidenceRecord
    {
        public string evidenceType = "CreatorDeclaration";
        public string nameAndVersion = "Immune War in-house asset declaration v1";
        public string urlOrOrderId;
        public string snapshotPath = "Docs/AssetProvenance/evidence/in-house-declaration.txt";
        public string snapshotSha256 = new string('0', 64);
        public string attributionText;
    }

    [Serializable]
    public sealed class ReviewRecord
    {
        public string reviewer = "Project owner";
        public string reviewedAtUtc = "2026-09-18T00:00:00Z";
        public string notes = "In-house generated implementation asset.";
    }

    [Serializable]
    public sealed class AssetRecord
    {
        public int schemaVersion = 1;
        public string recordKind = "ShippableAsset";
        public string assetId;
        public string manifestCategory;
        public string purpose;
        public string runtimePath;
        public string fallbackAssetId;
        public bool releaseCritical;
        public string sourceType = "in_house";
        public string creatorOrProvider = "Immune War project";
        public string sourceUrlOrJobId;
        public string acquiredOrGeneratedAtUtc = "2026-09-18T00:00:00Z";
        public string status = "Approved";
        public RightsRecord rights = new RightsRecord();
        public EvidenceRecord evidence = new EvidenceRecord();
        public string rawSha256;
        public string approvedExportSha256;
        public string recheckAtUtc;
        public ReviewRecord review = new ReviewRecord();

        public static AssetRecord CreateInHouse(string id, string path, string hash) => new AssetRecord
        {
            assetId = id,
            runtimePath = path,
            rawSha256 = hash,
            approvedExportSha256 = hash,
            fallbackAssetId = id + "_FALLBACK"
        };
    }
}

