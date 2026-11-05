using UnityEditor;
using UnityEngine;

namespace ElevatorRush.Editor
{
    /// <summary>
    /// Import settings for downloaded art under Resources (plan section 6): consistent scale,
    /// no cameras or lights, compressed meshes with read/write off, legacy animation for the
    /// character, crisp palette textures, and compressed mono audio for effects.
    /// </summary>
    public sealed class AssetImportRules : AssetPostprocessor
    {
        const string ModelsRoot = "Assets/ElevatorRush/Resources/Models/";
        const string CharacterRoot = ModelsRoot + "character/";
        const string AudioRoot = "Assets/ElevatorRush/Resources/Audio/";
        const string MusicPrefix = "music_";
        const float VorbisQuality = 0.5f;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsRoot))
                return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importBlendShapes = false;
            importer.addCollider = false;

            if (assetPath.StartsWith(CharacterRoot))
            {
                // Legacy clips play through the Animation component set up from code.
                importer.animationType = ModelImporterAnimationType.Legacy;
                importer.importAnimation = true;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ModelsRoot))
                return;
            // Kenney models use small color-palette textures: no mipmaps so cells never blend.
            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioRoot))
                return;
            var importer = (AudioImporter)assetImporter;
            bool music = System.IO.Path.GetFileName(assetPath).StartsWith(MusicPrefix);
            importer.forceToMono = !music;
            importer.loadInBackground = music;
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            importer.defaultSampleSettings = settings;
        }
    }
}
