using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    public sealed class AnimatedHealthBar : MonoBehaviour
    {
        private RectTransform _fill;
        private RectTransform _trail;
        private Image _fillImage;
        private Image _frame;
        private Text _label;
        private float _width;
        private float _target;
        private float _shown;
        private float _lagged;
        private float _maximum;
        private float _flash;

        public float DisplayedFraction => _shown;
        public float TargetFraction => _target;

        public static AnimatedHealthBar Create(Transform parent, string name, Vector2 position, Vector2 size,
            float maximum, float current, bool showNumbers = false)
        {
            var root = RuntimeUi.Rect(parent, name, position, size);
            var bar = root.gameObject.AddComponent<AnimatedHealthBar>();
            bar._width = size.x - 6f;
            bar._frame = RuntimeUi.Image(root, "Frame", Vector2.zero, size + new Vector2(4f, 4f),
                new Color(0.15f, 0.77f, 0.79f, 0.9f));
            RuntimeUi.Image(root, "Track", Vector2.zero, size, new Color(0.025f, 0.075f, 0.11f, 0.94f));
            var trailImage = RuntimeUi.Image(root, "DamageTrail", new Vector2(-size.x * 0.5f + 3f, 0f),
                new Vector2(bar._width, size.y - 6f), new Color(1f, 0.77f, 0.29f, 0.9f));
            bar._trail = trailImage.rectTransform;
            var fillImage = RuntimeUi.Image(root, "HealthFill", new Vector2(-size.x * 0.5f + 3f, 0f),
                new Vector2(bar._width, size.y - 6f), Color.green);
            bar._fill = fillImage.rectTransform;
            bar._fillImage = fillImage;
            SetLeftPivot(bar._trail);
            SetLeftPivot(bar._fill);
            if (showNumbers)
                bar._label = RuntimeUi.Text(root, "HealthText", "", Vector2.zero, size, 18, Color.white);
            bar._maximum = Mathf.Max(1f, maximum);
            bar._target = bar._shown = bar._lagged = Mathf.Clamp01(current / bar._maximum);
            bar.ApplyWidths();
            bar.UpdateLabel(current);
            return bar;
        }

        private static void SetLeftPivot(RectTransform rect)
        {
            rect.pivot = new Vector2(0f, 0.5f);
        }

        public void SetValue(float current, float maximum)
        {
            _maximum = Mathf.Max(1f, maximum);
            var next = Mathf.Clamp01(current / _maximum);
            if (next < _target - 0.001f) _flash = 0.32f;
            _target = next;
            UpdateLabel(current);
        }

        private void UpdateLabel(float current)
        {
            if (_label) _label.text = Mathf.CeilToInt(Mathf.Max(0f, current)) + " / " + Mathf.CeilToInt(_maximum);
        }

        private void Update()
        {
            var delta = Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, _target, delta * 1.8f);
            _lagged = Mathf.MoveTowards(_lagged, _shown, delta * 0.55f);
            _flash = Mathf.Max(0f, _flash - delta);
            ApplyWidths();
            var healthy = new Color(0.28f, 0.93f, 0.67f);
            var wounded = new Color(1f, 0.33f, 0.42f);
            _fillImage.color = Color.Lerp(wounded, healthy, Mathf.Clamp01(_shown * 1.6f));
            _frame.color = Color.Lerp(new Color(0.15f, 0.77f, 0.79f, 0.9f),
                new Color(1f, 0.38f, 0.42f), _flash / 0.32f);
        }

        private void ApplyWidths()
        {
            _fill.sizeDelta = new Vector2(_width * _shown, _fill.sizeDelta.y);
            _trail.sizeDelta = new Vector2(_width * _lagged, _trail.sizeDelta.y);
        }
    }
}
