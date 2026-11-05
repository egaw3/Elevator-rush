using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// Renders decor thumbnails for the Build sheet (home plan 5.3): builds the option far below
    /// the world, frames it with a private camera, renders once into a texture and removes it.
    /// Textures are cached per item and the oldest are released past a small limit.
    /// </summary>
    public sealed class ThumbnailRenderer
    {
        static readonly Vector3 StagePosition = new Vector3(0f, -400f, 0f);
        const float FieldOfView = 30f;
        const float Margin = 1.25f;
        const float Pitch = 12f;
        const float Yaw = -20f;

        readonly GameConfig config;
        readonly RoomDecorator decorator;
        readonly Camera cam;
        readonly Transform stage;
        readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        readonly List<string> order = new List<string>();

        public ThumbnailRenderer(GameConfig config, RoomDecorator decorator, Transform parent)
        {
            this.config = config;
            this.decorator = decorator;
            stage = new GameObject("ThumbnailStage").transform;
            stage.SetParent(parent, false);
            stage.position = StagePosition;

            var camGo = new GameObject("ThumbnailCamera");
            camGo.transform.SetParent(stage, false);
            cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = config.thumbnailBackground;
            cam.fieldOfView = FieldOfView;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
        }

        public Texture Get(int floor, int slot, int option)
        {
            string id = Catalog.DecorId(floor, slot, option);
            if (cache.TryGetValue(id, out var texture) && texture != null && texture.IsCreated())
                return texture;

            texture = new RenderTexture(config.thumbnailSize, config.thumbnailSize, 24) { name = "Thumb_" + id, antiAliasing = 2 };
            var preview = decorator.BuildPreview(stage, floor, slot, option);
            Frame(preview);
            cam.targetTexture = texture;
            cam.Render();
            cam.targetTexture = null;
            preview.SetActive(false);
            Object.Destroy(preview);

            cache[id] = texture;
            order.Remove(id);
            order.Add(id);
            while (order.Count > config.thumbnailCacheSize)
            {
                var oldest = order[0];
                order.RemoveAt(0);
                if (cache.TryGetValue(oldest, out var old) && old != null)
                    old.Release();
                cache.Remove(oldest);
            }
            return texture;
        }

        /// <summary>Points the camera at the preview from the front, slightly above and to the side.</summary>
        void Frame(GameObject preview)
        {
            var renderers = preview.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            float radius = bounds.extents.magnitude * Margin;
            float distance = radius / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
        }
    }
}
