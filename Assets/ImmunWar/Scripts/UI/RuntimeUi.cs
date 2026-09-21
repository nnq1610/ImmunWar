using System;
using UnityEngine;
using UnityEngine.UI;

namespace ImmunWar.UI
{
    internal enum UiButtonRole { Primary, Secondary, Danger, Card, Locked }

    internal static class RuntimeUi
    {
        private static Font _font;
        private static Sprite _circle;
        public static readonly Color Background = new Color(0.035f, 0.07f, 0.12f);
        public static readonly Color Panel = new Color(0.075f, 0.15f, 0.22f);
        public static readonly Color Accent = new Color(0.32f, 0.9f, 0.78f);

        public static RectTransform Root(string name)
        {
            var existing = GameObject.Find("SceneLabel");
            if (existing) existing.SetActive(false);
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (!canvas)
            {
                var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            return root;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Image(Transform parent, string name, Vector2 position, Vector2 size, Color color, bool circle = false)
        {
            var rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = circle ? Circle : null;
            image.raycastTarget = false;
            return image;
        }

        public static Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rect = Rect(parent, name, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string name, string caption, Vector2 position, Vector2 size, Color color, Action action, int fontSize = 30)
        {
            var rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            button.colors = colors;
            Text(rect, "Label", caption, Vector2.zero, size - new Vector2(12, 8), fontSize, Color.white);
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        public static void Skin(Button button, PlayableArtCatalog art, UiButtonRole role, bool selected = false)
        {
            if (!button) return;
            var image = button.GetComponent<Image>();
            if (!image) return;
            Sprite sprite = null;
            if (art)
                sprite = role switch
                {
                    UiButtonRole.Primary => art.primaryButton,
                    UiButtonRole.Danger => art.dangerButton,
                    UiButtonRole.Card => art.panel,
                    _ => art.secondaryButton
                };
            if (sprite) image.sprite = sprite;
            var baseColor = role switch
            {
                UiButtonRole.Primary => new Color(0.84f, 1f, 0.97f),
                UiButtonRole.Danger => new Color(1f, 0.86f, 0.88f),
                UiButtonRole.Locked => new Color(0.42f, 0.49f, 0.56f),
                UiButtonRole.Card => new Color(0.78f, 0.92f, 0.98f),
                _ => Color.white
            };
            if (selected) baseColor = new Color(0.48f, 1f, 0.85f);
            image.color = baseColor;
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = Color.Lerp(baseColor, new Color(0.18f, 0.36f, 0.43f), 0.35f);
            colors.disabledColor = new Color(0.32f, 0.4f, 0.48f, 0.75f);
            colors.fadeDuration = 0.18f;
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>();
            if (label) label.color = role == UiButtonRole.Locked ? new Color(0.76f, 0.82f, 0.84f) : Color.white;
        }

        public static Vector2 Point(Vector2 world) => new Vector2(world.x * 76f, world.y * 76f);

        public static void Line(Transform parent, string name, Vector2 from, Vector2 to, float width, Color color)
        {
            var center = (from + to) * 0.5f;
            var delta = to - from;
            var image = Image(parent, name, center, new Vector2(delta.magnitude, width), color);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Sprite Circle
        {
            get
            {
                if (_circle) return _circle;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                    {
                        var distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                        texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(31f - distance)));
                    }
                texture.Apply();
                _circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _circle;
            }
        }
    }
}
