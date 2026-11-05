using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// Small helpers for building uGUI from code. Nothing is a raycast target unless it is a
    /// button or a blocking panel, so holding on the HUD still drives the elevator.
    /// </summary>
    public static class UiBuilder
    {
        /// <summary>Style for every label; Bootstrap picks it once for the font in use.</summary>
        public static FontStyle FontStyle = FontStyle.Bold;

        public static Canvas CreateCanvas(string name, Transform parent, GameConfig config, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = config.uiReferenceResolution;
            scaler.matchWidthOrHeight = config.uiMatchWidthOrHeight;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchors at one point of the parent (0..1), with the given pivot, offset and size.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Text Label(Transform parent, string name, Font font, string text, int size, Color color,
            TextAnchor alignment, Color shadow)
        {
            var rt = Rect(name, parent);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyle;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            if (shadow.a > 0f)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = shadow;
                s.effectDistance = new Vector2(0f, -size * 0.06f);
            }
            return label;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool sliced)
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.raycastTarget = false;
            return image;
        }

        public static Button Button(Transform parent, string name, Font font, Sprite sprite, string label,
            int fontSize, Color color, Color textColor, Color shadow, UnityAction onClick)
        {
            var image = Image(parent, name, sprite, color, true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            image.gameObject.AddComponent<PressScale>();
            var text = Label(image.transform, "Label", font, label, fontSize, textColor, TextAnchor.MiddleCenter, shadow);
            Stretch(text.rectTransform);
            return button;
        }
    }
}
