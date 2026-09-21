using System.Collections.Generic;
using UnityEngine;

namespace ImmunWar.Audio
{
    public sealed class SfxService : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        public void Register(string id, AudioClip clip) { if (!string.IsNullOrEmpty(id) && clip) _clips[id] = clip; }
        public void Play(string id) { if (source && _clips.TryGetValue(id, out var clip)) source.PlayOneShot(clip); }
    }
}

