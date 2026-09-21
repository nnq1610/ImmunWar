using System;
using System.Collections;
using System.IO;
using ImmunWar.Audio;
using ImmunWar.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ImmunWar.Tests.PlayMode
{
    public sealed class AudioSettingsTests
    {
        [UnityTest] public IEnumerator MusicAndSfxPreviewApplyCancelAreIndependentAndPersisted() { var directory = Path.Combine(Path.GetTempPath(), "ImmunWarAudio-" + Guid.NewGuid().ToString("N")); var saves = new SaveRepository(directory, new[] { "map_lung" }); var settings = new SettingsRepository(saves); var go = new GameObject("Audio", typeof(AudioService)); var audio = go.GetComponent<AudioService>(); settings.Preview(.25f, .8f); audio.SetMusicVolume(settings.PreviewMusic); audio.SetSfxVolume(settings.PreviewSfx); settings.Apply(); Assert.That(audio.MusicVolume, Is.EqualTo(.25f)); Assert.That(audio.SfxVolume, Is.EqualTo(.8f)); settings.Preview(.9f, .1f); settings.Cancel(); Assert.That(settings.PreviewMusic, Is.EqualTo(.25f)); Assert.That(settings.PreviewSfx, Is.EqualTo(.8f)); UnityEngine.Object.Destroy(go); yield return null; Directory.Delete(directory, true); }
    }
}
