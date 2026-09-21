using UnityEngine;

namespace ImmunWar.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        public Rect LastSafeArea { get; private set; }
        public void Apply(Rect safeArea, Vector2 screenSize)
        {
            var rect = (RectTransform)transform; var width = Mathf.Max(1f, screenSize.x); var height = Mathf.Max(1f, screenSize.y);
            rect.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height); rect.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height); rect.offsetMin = rect.offsetMax = Vector2.zero; LastSafeArea = safeArea;
        }
        private void OnEnable() => Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
    }
}
