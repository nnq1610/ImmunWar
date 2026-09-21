using System;
using System.Collections.Generic;

namespace ImmunWar.Persistence
{
    public static class SaveDataValidator
    {
        public static SaveData Sanitize(SaveData source, IReadOnlyList<string> orderedMapIds)
        {
            source ??= SaveData.CreateDefault();
            var allowed = new HashSet<string>(orderedMapIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            allowed.Add("map_lung");
            source.schemaVersion = SaveData.CurrentSchemaVersion;
            source.saveSequence = Math.Max(0, source.saveSequence);
            if (!DateTime.TryParse(source.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out _))
                source.savedAtUtc = DateTime.UtcNow.ToString("O");
            source.unlockedMapIds = UniqueAllowed(source.unlockedMapIds, allowed);
            if (!source.unlockedMapIds.Contains("map_lung")) source.unlockedMapIds.Insert(0, "map_lung");
            source.completedMapIds = UniqueAllowed(source.completedMapIds, new HashSet<string>(source.unlockedMapIds, StringComparer.Ordinal));
            if (string.IsNullOrEmpty(source.lastSelectedMapId) || !source.unlockedMapIds.Contains(source.lastSelectedMapId))
                source.lastSelectedMapId = "map_lung";
            source.musicVolume = Clamp01(source.musicVolume);
            source.sfxVolume = Clamp01(source.sfxVolume);
            return source;
        }

        public static bool IsSemanticallyValid(SaveData data, IReadOnlyList<string> mapIds)
        {
            if (data == null || data.schemaVersion != SaveData.CurrentSchemaVersion || data.saveSequence < 0) return false;
            var copy = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(data));
            var sanitized = Sanitize(copy, mapIds);
            return UnityEngine.JsonUtility.ToJson(data) == UnityEngine.JsonUtility.ToJson(sanitized);
        }

        private static List<string> UniqueAllowed(IEnumerable<string> values, HashSet<string> allowed)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (values == null) return result;
            foreach (var value in values)
                if (!string.IsNullOrWhiteSpace(value) && allowed.Contains(value) && seen.Add(value)) result.Add(value);
            return result;
        }

        private static float Clamp01(float value) => float.IsNaN(value) ? 1f : Math.Max(0f, Math.Min(1f, value));
    }
}

