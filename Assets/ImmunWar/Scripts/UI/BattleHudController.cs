using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public readonly struct BattleHudSnapshot
    {
        public int Atp { get; } public int Vitality { get; } public int MaximumVitality { get; }
        public int WaveIndex { get; } public int WaveCount { get; } public string WavePhase { get; } public string Result { get; }
        public BattleHudSnapshot(int atp, int vitality, int maximumVitality, int waveIndex, int waveCount, string wavePhase, string result)
        { Atp = atp; Vitality = vitality; MaximumVitality = maximumVitality; WaveIndex = waveIndex; WaveCount = waveCount; WavePhase = wavePhase; Result = result; }
    }

    public sealed class BattleHudController : MonoBehaviour
    {
        [SerializeField] private Text atpLabel;
        [SerializeField] private Text vitalityLabel;
        [SerializeField] private Text waveLabel;
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private Text resultLabel;
        public BattleHudSnapshot LastSnapshot { get; private set; }
        public void ApplySnapshot(BattleHudSnapshot snapshot)
        {
            LastSnapshot = snapshot;
            if (atpLabel) atpLabel.text = $"ATP {snapshot.Atp}";
            if (vitalityLabel) vitalityLabel.text = $"Vitality {snapshot.Vitality}/{snapshot.MaximumVitality}";
            if (waveLabel) waveLabel.text = $"Wave {snapshot.WaveIndex}/{snapshot.WaveCount} — {snapshot.WavePhase}";
            if (resultRoot) resultRoot.SetActive(!string.IsNullOrEmpty(snapshot.Result));
            if (resultLabel) resultLabel.text = snapshot.Result;
        }
    }
}

