using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ImmunWar.Editor.AssetProvenance
{
    public sealed class AssetRecordValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    public static class AssetRecordValidator
    {
        private static readonly Regex IdPattern = new Regex("^[A-Z]{2,5}[0-9]{3}$", RegexOptions.Compiled);
        private static readonly Regex HashPattern = new Regex("^[a-f0-9]{64}$", RegexOptions.Compiled);

        public static AssetRecordValidationResult Validate(AssetRecord record, string actualHash, DateTime nowUtc)
        {
            var result = new AssetRecordValidationResult();
            if (record == null) { result.Errors.Add("Record is missing."); return result; }
            if (!IdPattern.IsMatch(record.assetId ?? string.Empty)) result.Errors.Add("Asset ID is invalid.");
            if (!string.Equals(record.status, "Approved", StringComparison.Ordinal)) result.Errors.Add("Asset is not approved.");
            if (string.IsNullOrEmpty(record.runtimePath) || !record.runtimePath.Replace('\\', '/').StartsWith("Assets/ImmunWar/", StringComparison.Ordinal)) result.Errors.Add("Runtime path is outside Assets/ImmunWar.");
            if (!HashPattern.IsMatch(record.rawSha256 ?? string.Empty) || !HashPattern.IsMatch(record.approvedExportSha256 ?? string.Empty)) result.Errors.Add("SHA-256 value is invalid.");
            if (!string.Equals(record.approvedExportSha256, actualHash, StringComparison.OrdinalIgnoreCase)) result.Errors.Add("Approved export hash does not match.");
            if (record.rights == null || record.rights.commercialUse != "Allowed" || record.rights.modification != "Allowed" || record.rights.embeddedRedistribution != "Allowed") result.Errors.Add("Required rights are unresolved.");
            if (record.evidence == null || string.IsNullOrWhiteSpace(record.evidence.snapshotPath)) result.Errors.Add("Evidence is missing.");
            if (record.review == null || string.IsNullOrWhiteSpace(record.review.reviewer)) result.Errors.Add("Review is missing.");
            if (DateTime.TryParse(record.recheckAtUtc, out var recheck) && recheck.ToUniversalTime() < nowUtc.ToUniversalTime()) result.Errors.Add("Rights recheck is overdue.");
            return result;
        }

        public static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}

