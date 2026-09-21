using UnityEngine;

namespace ImmunWar.Core.Config
{
    public abstract class GameConfig : ScriptableObject
    {
        [SerializeField] private string id;
        public string Id => id;
        public void SetIdForEditor(string value) => id = value?.Trim();
    }
}

