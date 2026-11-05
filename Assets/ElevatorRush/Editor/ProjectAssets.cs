using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ElevatorRush.Editor
{
    /// <summary>
    /// Creates the assets that must exist on disk through the AssetDatabase API.
    /// The base materials keep the URP Lit and Unlit shaders in player builds, where Shader.Find alone fails.
    /// Runs on editor load, before every build, and from the menu.
    /// </summary>
    [InitializeOnLoad]
    public sealed class ProjectAssets : IPreprocessBuildWithReport
    {
        const string ResourcesFolder = "Assets/ElevatorRush/Resources";
        const string MaterialsFolder = ResourcesFolder + "/Materials";

        static ProjectAssets()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureAssets();
        }

        [MenuItem("Elevator Rush/Create Required Assets")]
        public static void EnsureAssets()
        {
            EnsureMaterial(MaterialLibrary.BaseMaterialName, MaterialLibrary.LitShader);
            EnsureMaterial(MaterialLibrary.BaseUnlitMaterialName, MaterialLibrary.UnlitShader);
            EnsureMaterial(MaterialLibrary.BaseCutoutMaterialName, MaterialLibrary.UnlitShader, MakeAlphaClipped);
        }

        /// <summary>Alpha clipping on a saved material keeps the clipped shader variant in builds.</summary>
        static void MakeAlphaClipped(Material material)
        {
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }

        static void EnsureMaterial(string name, string shaderName, System.Action<Material> setup = null)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                return;

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[ElevatorRush] Shader '" + shaderName + "' not found. The project needs the Universal Render Pipeline.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/ElevatorRush", "Resources");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder(ResourcesFolder, "Materials");

            var material = new Material(shader);
            setup?.Invoke(material);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[ElevatorRush] Created " + path);
        }
    }
}
