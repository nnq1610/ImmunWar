using System;
using System.Collections.Generic;

namespace ImmunWar.Persistence
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 1;
        public int schemaVersion = CurrentSchemaVersion;
        public long saveSequence;
        public string savedAtUtc;
        public List<string> unlockedMapIds = new List<string>();
        public List<string> completedMapIds = new List<string>();
        public string lastSelectedMapId;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;

        public static SaveData CreateDefault() => new SaveData
        {
            schemaVersion = CurrentSchemaVersion,
            saveSequence = 0,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            unlockedMapIds = new List<string> { "map_lung" },
            completedMapIds = new List<string>(),
            lastSelectedMapId = "map_lung",
            musicVolume = 1f,
            sfxVolume = 1f
        };
    }
}

