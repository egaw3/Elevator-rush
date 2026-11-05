using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace ElevatorRush.Editor
{
    /// <summary>
    /// "Elevator Rush/Apply Mobile Settings": portrait-only, IL2CPP + ARM64 on Android, a placeholder
    /// bundle identifier, the product name, and soft shadows on every URP quality asset
    /// (the template's mobile asset ships with them off). All through PlayerSettings and
    /// SerializedObject, never by editing YAML.
    /// </summary>
    public static class MobileSetup
    {
        const string ProductName = "Elevator Rush";
        const string CompanyName = "Elevator Rush";
        // Placeholder; replace with your own reverse-domain id before publishing.
        const string BundleIdentifier = "com.example.elevatorrush";

        [MenuItem("Elevator Rush/Apply Mobile Settings")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;

            // Portrait only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Android: IL2CPP, 64-bit ARM.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleIdentifier);

            // iOS: IL2CPP is the only backend; same identifier.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);

            int softened = EnableSoftShadows();

            AssetDatabase.SaveAssets();
            Debug.Log("[ElevatorRush] Mobile settings applied: portrait only, IL2CPP + ARM64 (Android), id "
                + BundleIdentifier + ", soft shadows newly enabled on " + softened + " URP asset(s).");
        }

        /// <summary>Turns on soft shadows for every render pipeline asset used by a quality level.</summary>
        static int EnableSoftShadows()
        {
            int changed = 0;
            for (int level = 0; level < QualitySettings.count; level++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(level);
                if (asset == null)
                    asset = GraphicsSettings.defaultRenderPipeline;
                if (asset == null)
                    continue;

                var so = new SerializedObject(asset);
                var soft = so.FindProperty("m_SoftShadowsSupported");
                if (soft == null || soft.boolValue)
                    continue;
                soft.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                changed++;
            }
            return changed;
        }
    }
}
