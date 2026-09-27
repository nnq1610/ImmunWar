using System;
using System.Collections.Generic;
using UnityEngine;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Core.Data
{
    [Serializable]
    public class PlacementSaveData
    {
        public int Version = 1;
        
        // Placement Preferences
        public KeyCode[] HotkeyAssignments = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };
        public bool HighContrastMode = false;
        public float FontScale = 1.0f;
        
        // Grid State Serialization
        public List<GridNodeState> SavedNodes = new List<GridNodeState>();
        
        // Battle Statistics
        public Dictionary<string, float> PlacementEfficiencyMetrics = new Dictionary<string, float>();
    }

    [Serializable]
    public struct GridNodeState
    {
        public GridPosition Position;
        public string OccupantId;
        public int LastUpdatedTick;
    }

    /// <summary>
    /// Handles migrating old save data versions and corruption recovery.
    /// Implements Task 10.2
    /// </summary>
    public static class SaveDataMigrationManager
    {
        public const int CurrentVersion = 2;

        public static PlacementSaveData Migrate(PlacementSaveData data)
        {
            if (data == null)
            {
                // Corruption recovery: graceful fallback to defaults
                return CreateDefault();
            }

            if (data.Version < 2)
            {
                // Migrate Version 1 -> 2
                // Example: Add newly tracked metrics or update grid layouts
                if (data.PlacementEfficiencyMetrics == null)
                {
                    data.PlacementEfficiencyMetrics = new Dictionary<string, float>();
                }
                data.Version = 2;
            }

            return data;
        }

        private static PlacementSaveData CreateDefault()
        {
            return new PlacementSaveData
            {
                Version = CurrentVersion,
                HotkeyAssignments = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 },
                HighContrastMode = false,
                FontScale = 1.0f,
                SavedNodes = new List<GridNodeState>(),
                PlacementEfficiencyMetrics = new Dictionary<string, float>()
            };
        }
    }
}
