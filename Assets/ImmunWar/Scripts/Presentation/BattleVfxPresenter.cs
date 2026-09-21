using ImmunWar.Presentation;
using UnityEngine;

namespace ImmunWar.Presentation
{
    public sealed class BattleVfxPresenter : MonoBehaviour, IPoolResettable
    {
        [SerializeField] private ParticleSystem particles;
        public void Play(Vector2 position) { transform.position = position; if (particles) particles.Play(true); }
        public void ResetForPool() { if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); transform.localPosition = Vector3.zero; }
    }
}

