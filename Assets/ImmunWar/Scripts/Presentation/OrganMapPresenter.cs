using ImmunWar.Core.Config;
using UnityEngine;

namespace ImmunWar.Presentation
{
    public sealed class OrganMapPresenter : MonoBehaviour
    {
        public string BoundMapId { get; private set; }
        public int BoundRouteCount { get; private set; }
        public void Bind(OrganMapConfig map) { BoundMapId = map ? map.Id : null; BoundRouteCount = map?.routes?.Length ?? 0; }
    }
}
