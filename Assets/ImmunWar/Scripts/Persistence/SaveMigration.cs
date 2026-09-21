using System;
using UnityEngine;

namespace ImmunWar.Persistence
{
    [Serializable]
    internal sealed class SaveVersionEnvelope { public int schemaVersion; }

    public static class SaveMigration
    {
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("Save JSON is empty.");
            var envelope = JsonUtility.FromJson<SaveVersionEnvelope>(json);
            if (envelope == null) throw new FormatException("Save envelope is invalid.");
            if (envelope.schemaVersion > SaveData.CurrentSchemaVersion)
                throw new NotSupportedException("Save schema is newer than this build.");

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) throw new FormatException("Save data is invalid.");
            if (data.schemaVersion <= 0)
            {
                data.schemaVersion = SaveData.CurrentSchemaVersion;
                data.musicVolume = 1f;
                data.sfxVolume = 1f;
                if (string.IsNullOrEmpty(data.savedAtUtc)) data.savedAtUtc = DateTime.UtcNow.ToString("O");
            }
            return data;
        }
    }
}

