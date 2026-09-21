using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace ImmunWar.Audio
{
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public void SetMusicVolume(float value) { MusicVolume = Mathf.Clamp01(value); SetMixer("MusicVolume", MusicVolume); if (musicSource) musicSource.volume = MusicVolume; }
        public void SetSfxVolume(float value) { SfxVolume = Mathf.Clamp01(value); SetMixer("SfxVolume", SfxVolume); if (sfxSource) sfxSource.volume = SfxVolume; }
        public void PlaySfx(AudioClip clip) { if (sfxSource && clip) sfxSource.PlayOneShot(clip); }
        public void PlayMusic(AudioClip clip, float fadeSeconds = .25f) { StartCoroutine(FadeMusic(clip, fadeSeconds)); }
        private IEnumerator FadeMusic(AudioClip clip, float seconds) { if (!musicSource) yield break; var start = musicSource.volume; for (var t = 0f; t < seconds; t += Time.unscaledDeltaTime) { musicSource.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(.01f, seconds)); yield return null; } musicSource.clip = clip; musicSource.loop = true; musicSource.Play(); musicSource.volume = MusicVolume; }
        private void SetMixer(string parameter, float linear) { if (mixer) mixer.SetFloat(parameter, linear <= .0001f ? -80f : Mathf.Log10(linear) * 20f); }
    }
}
