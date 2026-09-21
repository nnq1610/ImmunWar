using System;
using System.IO;
using NUnit.Framework;
using ImmunWar.Editor.AssetProvenance;

namespace ImmunWar.Tests.EditMode
{
    public sealed class AssetProvenanceTests
    {
        [Test]
        public void ApprovedRuntimeAsset_RequiresAllowlistedPathAndMatchingHash()
        {
            var file = Path.GetTempFileName();
            try
            {
                File.WriteAllText(file, "approved export");
                var hash = AssetRecordValidator.ComputeSha256(file);
                var record = AssetRecord.CreateInHouse("DEF001", "Assets/ImmunWar/Art/Defenders/P0/DEF_Macrophage.png", hash);
                var result = AssetRecordValidator.Validate(record, hash, DateTime.UtcNow);
                Assert.That(result.IsValid, Is.True, string.Join("; ", result.Errors));
            }
            finally { File.Delete(file); }
        }

        [Test]
        public void UnknownRightsOrPathOutsideRuntime_IsRejected()
        {
            var record = AssetRecord.CreateInHouse("DEF001", "Assets/_Game/bad.png", new string('a', 64));
            record.rights.commercialUse = "Unknown";
            var result = AssetRecordValidator.Validate(record, record.approvedExportSha256, DateTime.UtcNow);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }
    }
}
