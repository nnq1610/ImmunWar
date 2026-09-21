using ImmunWar.Audio;
using ImmunWar.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        private SettingsRepository _settings; private AudioService _audio;
        public void Initialize(SettingsRepository settings, AudioService audio) { _settings = settings; _audio = audio; Sync(); }
        public void Preview(float music, float sfx) { _settings?.Preview(music, sfx); _audio?.SetMusicVolume(music); _audio?.SetSfxVolume(sfx); }
        public void Apply() { _settings?.Apply(); Sync(); }
        public void Cancel() { _settings?.Cancel(); Sync(); }
        private void Sync() { if (_settings == null) return; if (musicSlider) musicSlider.value = _settings.PreviewMusic; if (sfxSlider) sfxSlider.value = _settings.PreviewSfx; _audio?.SetMusicVolume(_settings.PreviewMusic); _audio?.SetSfxVolume(_settings.PreviewSfx); }
    }
}
