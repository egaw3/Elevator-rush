using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ElevatorRush
{
    /// <summary>
    /// Perspective camera turned slightly to the side. Moves back until every corner of the
    /// target bounds fits inside the viewport for the current aspect ratio.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        readonly Vector3[] corners = new Vector3[8];

        GameConfig config;
        Camera cam;
        Quaternion rotation;
        Bounds target;
        Vector3 currentCenter;
        float currentDistance;
        bool hasTarget;
        float shakeLeft;
        UniversalRenderPipelineAsset pipeline;
        float originalShadowDistance;
        float topBand;
        float bottomBand;
        float maxReserve;

        public Camera Camera => cam;

        public void Init(GameConfig config, Camera camera)
        {
            this.config = config;
            cam = camera;
            cam.orthographic = false;
            cam.fieldOfView = config.cameraFov;
            cam.nearClipPlane = config.cameraNear;
            cam.farClipPlane = config.cameraFar;
            rotation = Quaternion.Euler(config.cameraPitch, config.cameraYaw, 0f);
            transform.rotation = rotation;
            SetBands(config.cameraTopBand, config.cameraBottomBand);
        }

        /// <summary>
        /// Heights (UI reference pixels) of the screen bands at the top and bottom that the
        /// building must stay clear of. Levels and Home use different bands.
        /// </summary>
        public void SetBands(float top, float bottom)
        {
            SetBands(top, bottom, config.cameraMaxReserve);
        }

        /// <summary>As above, letting the bands take up to <paramref name="maxScreenFraction"/> of the screen height.</summary>
        public void SetBands(float top, float bottom, float maxScreenFraction)
        {
            topBand = top;
            bottomBand = bottom;
            maxReserve = maxScreenFraction;
        }

        /// <summary>Frame these bounds. Without <paramref name="instant"/> the camera glides there.</summary>
        public void Fit(Bounds bounds, bool instant)
        {
            target = bounds;
            hasTarget = true;
            if (instant)
            {
                currentCenter = target.center;
                currentDistance = RequiredDistance();
                Apply();
            }
        }

        // UI bands plus whatever a notch or home indicator hides, as fractions of the screen height.
        // Together they never take more than cameraMaxReserve, so the building always has room.
        float TopReserve => Reserve(topBand, SafeArea.TopInset);
        float BottomReserve => Reserve(bottomBand, SafeArea.BottomInset);

        float Reserve(float band, float inset)
        {
            float top = BandFraction(topBand) + SafeArea.TopInset;
            float bottom = BandFraction(bottomBand) + SafeArea.BottomInset;
            float total = top + bottom;
            float scale = total > maxReserve ? maxReserve / total : 1f;
            return (BandFraction(band) + inset) * scale;
        }

        /// <summary>A UI band height in reference pixels as a fraction of the screen height.</summary>
        float BandFraction(float referencePixels)
        {
            if (Screen.height <= 0)
                return 0f;
            // Matches CanvasFit: tall screens scale the UI by width, wider ones by height.
            float uiScale = CanvasFit.UiScale(config);
            return referencePixels * uiScale / Screen.height;
        }

        void LateUpdate()
        {
            if (!hasTarget)
                return;
            UpdateShadowDistance();

            float t = 1f - Mathf.Exp(-config.cameraSmoothing * Time.unscaledDeltaTime);
            currentCenter = Vector3.Lerp(currentCenter, target.center, t);
            currentDistance = Mathf.Lerp(currentDistance, RequiredDistance(), t);
            Apply();
        }

        /// <summary>
        /// Places the camera at the fitted distance, raised along its up axis so the bounds sit
        /// in the middle of the free band between the top and bottom reserves.
        /// </summary>
        void Apply()
        {
            float tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float bandCenter = BottomReserve - TopReserve;
            float lift = -bandCenter * currentDistance * tan;
            transform.rotation = rotation;
            transform.position = currentCenter - rotation * Vector3.forward * currentDistance + rotation * Vector3.up * lift
                + ShakeOffset();
        }

        /// <summary>
        /// Keeps the shadow distance just long enough to reach the back of the building, so tall
        /// buildings keep their shadows and short ones do not waste shadow-map resolution. The
        /// pipeline asset's own value is restored when the game stops, so the Editor asset is
        /// left unchanged.
        /// </summary>
        void UpdateShadowDistance()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp))
                return;
            if (pipeline != urp)
            {
                RestoreShadowDistance();
                pipeline = urp;
                originalShadowDistance = urp.shadowDistance;
            }
            float wanted = Mathf.Ceil(currentDistance + target.extents.magnitude + config.shadowDistanceMargin);
            if (!Mathf.Approximately(urp.shadowDistance, wanted))
                urp.shadowDistance = wanted;
        }

        void RestoreShadowDistance()
        {
            if (pipeline != null)
                pipeline.shadowDistance = originalShadowDistance;
            pipeline = null;
        }

        void OnDestroy()
        {
            RestoreShadowDistance();
        }

        void OnApplicationQuit()
        {
            RestoreShadowDistance();
        }

        /// <summary>A small decaying shake, e.g. on a perfect dock.</summary>
        public void Shake()
        {
            shakeLeft = config.cameraShakeTime;
        }

        Vector3 ShakeOffset()
        {
            if (shakeLeft <= 0f)
                return Vector3.zero;
            shakeLeft = Mathf.Max(0f, shakeLeft - Time.unscaledDeltaTime);
            float strength = config.cameraShakeAmount * (shakeLeft / config.cameraShakeTime);
            float t = Time.unscaledTime * config.cameraShakeFrequency;
            return rotation * new Vector3(Mathf.Sin(t), Mathf.Cos(t * 1.3f), 0f) * strength;
        }

        /// <summary>
        /// Smallest distance from the bounds center at which every corner projects inside the
        /// free part of the viewport. Solved per corner in camera space; the small depth of the
        /// building makes the band offset in Apply a close fit.
        /// </summary>
        float RequiredDistance()
        {
            float tan = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float tanV = tan * (1f - TopReserve - BottomReserve);
            float tanH = tan * cam.aspect * (1f - config.cameraMargin);

            Vector3 c = target.center;
            Vector3 e = target.extents;
            int n = 0;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                corners[n++] = new Vector3(c.x + e.x * x, c.y + e.y * y, c.z + e.z * z);

            var inverse = Quaternion.Inverse(rotation);
            float distance = config.cameraNear;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 p = inverse * (corners[i] - c);
                distance = Mathf.Max(distance, Mathf.Abs(p.x) / tanH - p.z);
                distance = Mathf.Max(distance, Mathf.Abs(p.y) / tanV - p.z);
            }
            return distance;
        }
    }
}
