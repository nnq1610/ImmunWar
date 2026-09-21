using ImmunWar.Presentation;
using UnityEngine;

namespace ImmunWar.Presentation
{
    public sealed class BattleEntityPresenter : MonoBehaviour, IPoolResettable
    {
        [SerializeField] private SpriteRenderer sprite;
        public string InstanceId { get; private set; }
        public void Bind(string instanceId, Vector2 position, Color tint) { InstanceId = instanceId; transform.position = position; if (sprite) sprite.color = tint; }
        public void ResetForPool() { InstanceId = null; transform.localPosition = Vector3.zero; if (sprite) sprite.color = Color.white; }
    }
}

