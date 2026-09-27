using UnityEngine;
using ImmunWar.Battle.Placement;

namespace ImmunWar.Battle.Audio
{
    public enum PlacementResultType
    {
        Success,
        InvalidPosition,
        InsufficientATP
    }

    public struct PlacementResultFeedback
    {
        public PlacementResultType Type;
    }

    /// <summary>
    /// Handles audio feedback for placement operations.
    /// Implements Task 11.1
    /// </summary>
    public class PlacementAudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        
        [Header("Primary Sounds")]
        [SerializeField] private AudioClip _successSound;
        [SerializeField] private AudioClip _errorSound;
        [SerializeField] private AudioClip _atpWarningSound;

        [Header("Fallback Sounds")]
        [SerializeField] private AudioClip _genericPositiveSound;
        [SerializeField] private AudioClip _genericNegativeSound;
        [SerializeField] private AudioClip _genericWarningSound;

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void PlayPlacementFeedback(PlacementResultFeedback result)
        {
            switch (result.Type)
            {
                case PlacementResultType.Success:
                    PlayWithFallback(_successSound, _genericPositiveSound);
                    break;
                case PlacementResultType.InvalidPosition:
                    PlayWithFallback(_errorSound, _genericNegativeSound);
                    break;
                case PlacementResultType.InsufficientATP:
                    PlayWithFallback(_atpWarningSound, _genericWarningSound);
                    break;
            }
        }
        
        private void PlayWithFallback(AudioClip primary, AudioClip fallback)
        {
            if (_audioSource == null || !_audioSource.enabled) return;

            if (primary != null)
                _audioSource.PlayOneShot(primary);
            else if (fallback != null)
                _audioSource.PlayOneShot(fallback);
        }
    }
}
