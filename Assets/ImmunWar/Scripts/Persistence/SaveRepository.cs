using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ImmunWar.Persistence
{
    public sealed class SaveRepository
    {
        private readonly IReadOnlyList<string> _mapIds;
        public string PrimaryPath { get; }
        public string BackupPath { get; }
        public string TemporaryPath { get; }

        public SaveRepository(string directory, IReadOnlyList<string> mapIds)
        {
            Directory.CreateDirectory(directory);
            _mapIds = mapIds ?? new[] { "map_lung" };
            PrimaryPath = Path.Combine(directory, "save.json");
            BackupPath = Path.Combine(directory, "save.backup.json");
            TemporaryPath = Path.Combine(directory, "save.tmp.json");
        }

        public SaveData Load()
        {
            var primary = TryRead(PrimaryPath);
            var backup = TryRead(BackupPath);
            if (primary == null && backup == null) return SaveData.CreateDefault();
            var selected = backup == null || primary != null && primary.saveSequence >= backup.saveSequence ? primary : backup;
            return SaveDataValidator.Sanitize(selected, _mapIds);
        }

        public SaveData Save(SaveData data)
        {
            data = SaveDataValidator.Sanitize(data, _mapIds);
            data.saveSequence = Math.Max(data.saveSequence, Load().saveSequence) + 1;
            data.savedAtUtc = DateTime.UtcNow.ToString("O");
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(TemporaryPath, json);
            var verified = TryRead(TemporaryPath);
            if (verified == null) throw new IOException("Temporary save verification failed.");
            if (File.Exists(PrimaryPath)) File.Copy(PrimaryPath, BackupPath, true);
            File.Copy(TemporaryPath, PrimaryPath, true);
            File.Delete(TemporaryPath);
            return data;
        }

        private SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var data = SaveMigration.FromJson(File.ReadAllText(path));
                return SaveDataValidator.IsSemanticallyValid(data, _mapIds) ? data : null;
            }
            catch { return null; }
        }
    }
}

