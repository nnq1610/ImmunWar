using System;
using System.Collections.Generic;
using System.Linq;

namespace ImmunWar.Core.Config
{
    public sealed class ConfigValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    public static class ConfigValidator
    {
        public static ConfigValidationResult Validate(GameCatalog catalog)
        {
            var result = new ConfigValidationResult();
            if (catalog == null) { result.Errors.Add("Catalog is missing."); return result; }
            var all = catalog.AllConfigs().Where(x => x != null).ToArray();
            foreach (var config in all)
                if (string.IsNullOrWhiteSpace(config.Id)) result.Errors.Add($"{config.name}: stable ID is empty.");
            foreach (var duplicate in all.Where(x => !string.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id, StringComparer.Ordinal).Where(x => x.Count() > 1))
                result.Errors.Add($"Duplicate config ID: {duplicate.Key}");
            foreach (var map in catalog.maps ?? Array.Empty<OrganMapConfig>())
            {
                if (map == null) continue;
                if (map.routes == null || map.routes.Length == 0) result.Errors.Add($"{map.Id}: at least one route is required.");
                if (map.waveSet == null) result.Errors.Add($"{map.Id}: wave set is required.");
            }
            return result;
        }

        public static DefenderRoleMask ToMask(DefenderRole role) => role switch
        {
            DefenderRole.Blocker => DefenderRoleMask.Blocker,
            DefenderRole.Damage => DefenderRoleMask.Damage,
            DefenderRole.Support => DefenderRoleMask.Support,
            DefenderRole.Burst => DefenderRoleMask.Burst,
            DefenderRole.Repair => DefenderRoleMask.Repair,
            DefenderRole.Economy => DefenderRoleMask.Economy,
            _ => DefenderRoleMask.None
        };
    }
}

