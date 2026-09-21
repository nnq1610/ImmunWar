using UnityEngine;

namespace ImmunWar.Core.Config
{
    [CreateAssetMenu(menuName = "Immune War/Presentation")]
    public sealed class PresentationConfig : GameConfig
    {
        public GameObject prefab;
        public Sprite icon;
        public AudioClip primaryAudio;
        public Color accent = Color.white;
    }
}
