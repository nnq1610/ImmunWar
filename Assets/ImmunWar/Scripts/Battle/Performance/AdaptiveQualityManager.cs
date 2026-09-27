using UnityEngine;

namespace ImmunWar.Battle.Performance
{
    public enum QualityLevelState
    {
        High,
        Medium,
        Low
    }

    /// <summary>
    /// Monitors frame rate and dynamically adjusts quality settings to maintain 60 FPS.
    /// Implements Task 9.3
    /// </summary>
    public class AdaptiveQualityManager : MonoBehaviour
    {
        [SerializeField] private float _targetFPS = 60f;
        [SerializeField] private float _checkInterval = 2f;
        
        private float _deltaTime = 0f;
        private float _timeSinceLastCheck = 0f;
        private QualityLevelState _currentQuality = QualityLevelState.High;

        public QualityLevelState CurrentQuality => _currentQuality;

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _timeSinceLastCheck += Time.unscaledDeltaTime;

            if (_timeSinceLastCheck >= _checkInterval)
            {
                EvaluatePerformance();
                _timeSinceLastCheck = 0f;
            }
        }

        private void EvaluatePerformance()
        {
            float fps = 1.0f / _deltaTime;

            if (fps < _targetFPS - 10f && _currentQuality != QualityLevelState.Low)
            {
                // Degrade quality
                if (_currentQuality == QualityLevelState.High)
                    SetQuality(QualityLevelState.Medium);
                else if (_currentQuality == QualityLevelState.Medium)
                    SetQuality(QualityLevelState.Low);
            }
            else if (fps >= _targetFPS && _currentQuality != QualityLevelState.High)
            {
                // Upgrade quality slightly if performance permits
                if (_currentQuality == QualityLevelState.Low)
                    SetQuality(QualityLevelState.Medium);
                else if (_currentQuality == QualityLevelState.Medium)
                    SetQuality(QualityLevelState.High);
            }
        }

        private void SetQuality(QualityLevelState state)
        {
            _currentQuality = state;
            
            switch (state)
            {
                case QualityLevelState.Low:
                    QualitySettings.SetQualityLevel(0, true);
                    break;
                case QualityLevelState.Medium:
                    QualitySettings.SetQualityLevel(2, true);
                    break;
                case QualityLevelState.High:
                    QualitySettings.SetQualityLevel(5, true);
                    break;
            }
        }
    }
}
