using System.Collections.Generic;
using UnityEngine;

namespace ElevatorRush
{
    /// <summary>
    /// One shared URP material per color (Lit for the world, Unlit for icons and rings),
    /// created once and cached. Each is a copy of a base material on disk, which keeps the
    /// shaders in player builds.
    /// </summary>
    public sealed class MaterialLibrary
    {
        public const string BaseMaterialName = "BaseLit";
        public const string BaseUnlitMaterialName = "BaseUnlit";
        public const string BaseCutoutMaterialName = "BaseUnlitCutout";
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string UnlitShader = "Universal Render Pipeline/Unlit";
        const string MaterialFolder = "Materials/";
        const float Smoothness = 0.15f;
        const float ShinySmoothness = 0.75f;
        const float ShinyMetallic = 0.85f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        public const string BaseMapProperty = "_BaseMap";
        static readonly int BaseMapId = Shader.PropertyToID(BaseMapProperty);
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        readonly Dictionary<Color32, Material> lit = new Dictionary<Color32, Material>();
        readonly Dictionary<Color32, Material> unlit = new Dictionary<Color32, Material>();
        readonly Dictionary<(Texture, Color32), Material> cutout = new Dictionary<(Texture, Color32), Material>();
        readonly Dictionary<(Texture, Vector2), Material> patterned = new Dictionary<(Texture, Vector2), Material>();
        readonly Dictionary<Color32, Material> shiny = new Dictionary<Color32, Material>();
        readonly Dictionary<Color32, Material> glossy = new Dictionary<Color32, Material>();
        readonly Material baseLit;
        readonly Material baseUnlit;
        readonly Material baseCutout;

        public MaterialLibrary()
        {
            baseLit = LoadBase(BaseMaterialName, LitShader);
            baseUnlit = LoadBase(BaseUnlitMaterialName, UnlitShader);
            baseCutout = LoadBase(BaseCutoutMaterialName, UnlitShader);
        }

        public Material Get(Color color) => Get(lit, baseLit, color, "ER_");

        public Material GetUnlit(Color color) => Get(unlit, baseUnlit, color, "ER_Unlit_");

        /// <summary>Unlit alpha-clipped material showing a texture tinted with a color. One per pair.</summary>
        public Material GetCutout(Texture texture, Color color)
        {
            var key = (texture, (Color32)color);
            if (cutout.TryGetValue(key, out var material))
                return material;

            material = new Material(baseCutout) { name = "ER_Cutout_" + texture.name + "_" + ColorUtility.ToHtmlStringRGB(color) };
            material.SetColor(BaseColorId, color);
            material.SetTexture(BaseMapId, texture);
            cutout.Add(key, material);
            return material;
        }

        /// <summary>Lit material showing a repeating texture, tiled this many times across a face. One per pair.</summary>
        public Material GetPattern(Texture texture, Vector2 tiling)
        {
            tiling = new Vector2(Mathf.Round(tiling.x * 100f) / 100f, Mathf.Round(tiling.y * 100f) / 100f);
            var key = (texture, tiling);
            if (patterned.TryGetValue(key, out var material))
                return material;

            material = new Material(baseLit) { name = "ER_Pattern_" + texture.name };
            material.SetColor(BaseColorId, Color.white);
            material.SetFloat(SmoothnessId, Smoothness);
            material.SetTexture(BaseMapId, texture);
            material.SetTextureScale(BaseMapId, tiling);
            patterned.Add(key, material);
            return material;
        }

        /// <summary>Polished metal (gold trim, chrome), lit with a strong highlight. One per color.</summary>
        public Material GetShiny(Color color)
        {
            Color32 key = color;
            if (shiny.TryGetValue(key, out var material))
                return material;
            material = new Material(baseLit) { name = "ER_Shiny_" + ColorUtility.ToHtmlStringRGB(color) };
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, ShinySmoothness);
            material.SetFloat(MetallicId, ShinyMetallic);
            shiny.Add(key, material);
            return material;
        }

        /// <summary>Smooth, non-metallic (glass-like panels). One per color.</summary>
        public Material GetGlossy(Color color)
        {
            Color32 key = color;
            if (glossy.TryGetValue(key, out var material))
                return material;
            material = new Material(baseLit) { name = "ER_Glossy_" + ColorUtility.ToHtmlStringRGB(color) };
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, ShinySmoothness);
            glossy.Add(key, material);
            return material;
        }

        static Material Get(Dictionary<Color32, Material> cache, Material source, Color color, string prefix)
        {
            Color32 key = color;
            if (cache.TryGetValue(key, out var material))
                return material;

            material = new Material(source) { name = prefix + ColorUtility.ToHtmlStringRGB(color) };
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, Smoothness);
            cache.Add(key, material);
            return material;
        }

        static Material LoadBase(string name, string shaderName)
        {
            var material = Resources.Load<Material>(MaterialFolder + name);
            if (material != null)
                return material;

            // Works in the editor; in a player only if something else keeps the shader.
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("[ElevatorRush] No " + name + " material. Run 'Elevator Rush/Create Required Assets'.");
                shader = Shader.Find("Hidden/InternalErrorShader");
            }
            return new Material(shader);
        }
    }
}
