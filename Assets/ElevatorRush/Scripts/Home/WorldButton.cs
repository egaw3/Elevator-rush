using System;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorRush
{
    /// <summary>
    /// A UI button that follows something in the 3D world (home plan 3.2): either a fixed-size
    /// marker over a world point, or an invisible hit area covering a world box on screen.
    /// Positioned every frame with WorldToScreenPoint, so no physics colliders are needed.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldButton : MonoBehaviour
    {
        readonly Vector3[] corners = new Vector3[8];

        Camera cam;
        Canvas canvas;
        RectTransform rect;
        Bounds box;
        Vector3 point;
        bool followsBox;

        public Button Button { get; private set; }
        public Image Image { get; private set; }
        /// <summary>The icon inside a marker, or null for a hit area.</summary>
        public Image Icon { get; private set; }

        /// <summary>A round marker of fixed size in reference pixels, with an icon.</summary>
        public static WorldButton Marker(Transform parent, string name, Camera camera, Canvas canvas, Sprite bubble, Sprite icon,
            Color bubbleColor, Color iconColor, float size, Action onClick)
        {
            var image = UiBuilder.Image(parent, name, bubble, bubbleColor, false);
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = Vector2.one * size;
            var wb = Setup(image, camera, canvas, onClick);
            image.gameObject.AddComponent<PressScale>();
            wb.Icon = UiBuilder.Image(image.transform, "Icon", icon, iconColor, false);
            UiBuilder.Place(wb.Icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.6f);
            return wb;
        }

        /// <summary>An invisible button over a world box.</summary>
        public static WorldButton HitArea(Transform parent, string name, Camera camera, Canvas canvas, Action onClick)
        {
            var image = UiBuilder.Image(parent, name, null, Color.clear, false);
            image.raycastTarget = true;
            return Setup(image, camera, canvas, onClick);
        }

        static WorldButton Setup(Image image, Camera camera, Canvas canvas, Action onClick)
        {
            var wb = image.gameObject.AddComponent<WorldButton>();
            wb.cam = camera;
            wb.canvas = canvas;
            wb.rect = image.rectTransform;
            wb.rect.anchorMin = wb.rect.anchorMax = wb.rect.pivot = new Vector2(0.5f, 0.5f);
            wb.Image = image;
            wb.Button = image.gameObject.AddComponent<Button>();
            wb.Button.targetGraphic = image;
            wb.Button.onClick.AddListener(() => onClick());
            return wb;
        }

        public void Follow(Vector3 worldPoint)
        {
            point = worldPoint;
            followsBox = false;
            Place();
        }

        public void Cover(Bounds worldBox)
        {
            box = worldBox;
            followsBox = true;
            Place();
        }

        void LateUpdate()
        {
            Place();
        }

        void Place()
        {
            if (cam == null)
                return;
            if (!followsBox)
            {
                var s = cam.WorldToScreenPoint(point);
                Image.enabled = s.z > 0f;
                rect.position = new Vector3(s.x, s.y, 0f);
                return;
            }

            var c = box.center;
            var e = box.extents;
            int n = 0;
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        corners[n++] = cam.WorldToScreenPoint(new Vector3(c.x + e.x * x, c.y + e.y * y, c.z + e.z * z));
            var min = corners[0];
            var max = corners[0];
            for (int i = 1; i < corners.Length; i++)
            {
                min = Vector3.Min(min, corners[i]);
                max = Vector3.Max(max, corners[i]);
            }
            float scale = canvas.scaleFactor;
            rect.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            rect.sizeDelta = new Vector2(max.x - min.x, max.y - min.y) / scale;
        }
    }
}
