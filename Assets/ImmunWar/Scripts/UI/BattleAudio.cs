using System.Collections.Generic;
using ImmunWar.Persistence;
using UnityEngine;

namespace ImmunWar.UI
{
    /// <summary>
    /// Plays looping music and one-shot effects for the runtime-built menu and battle views.
    /// Volumes come from the save file; each clip is rate-limited so rapid combat events do not stack into noise.
    /// </summary>
    public sealed class BattleAudio
    {
        private const float MusicLevel = 0.45f;
        private readonly AudioSource _music;
        private readonly AudioSource _sfx;
        private readonly float _musicVolume;
        private readonly float _sfxVolume;
        private readonly Dictionary<AudioClip, float> _lastPlayed = new Dictionary<AudioClip, float>();
        private bool _isMuted;

        public bool IsMuted => _isMuted;

        public bool ToggleMute()
        {
            _isMuted = !_isMuted;
            ApplyMute();
            return _isMuted;
        }

        public void SetMute(bool muted)
        {
            _isMuted = muted;
            ApplyMute();
        }

        private void ApplyMute()
        {
            if (_music) _music.mute = _isMuted;
            if (_sfx) _sfx.mute = _isMuted;
        }

        private BattleAudio(AudioSource music, AudioSource sfx, float musicVolume, float sfxVolume)
        {
            _music = music;
            _sfx = sfx;
            _musicVolume = Mathf.Clamp01(musicVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
        }

        public static BattleAudio Create(GameObject host, PlayableArtCatalog art, SaveData progress)
        {
            var music = host.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            var sfx = host.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            return new BattleAudio(music, sfx, progress != null ? progress.musicVolume : 1f, progress != null ? progress.sfxVolume : 1f);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (!clip || !_music) return;
            _music.clip = clip;
            _music.volume = MusicLevel * _musicVolume;
            _music.mute = _isMuted;
            _music.Play();
        }

        public void StopMusic()
        {
            if (_music) _music.Stop();
        }

        public void Play(AudioClip clip, float volume = 1f, float minInterval = 0.05f)
        {
            if (_isMuted || !clip || !_sfx || _sfxVolume <= 0f) return;
            var now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(clip, out var last) && now - last < minInterval) return;
            _lastPlayed[clip] = now;
            _sfx.PlayOneShot(clip, volume * _sfxVolume);
        }
    }
}
