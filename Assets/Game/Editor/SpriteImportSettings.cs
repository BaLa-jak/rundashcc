using System.IO;
using UnityEditor;
using UnityEngine;

namespace RunDashC.EditorTools
{
    /// <summary>Configura automáticamente las imágenes de Assets/Game/Resources/Art como sprites.</summary>
    class SpriteImportSettings : AssetPostprocessor
    {
        const string ArtDir = "Assets/Game/Resources/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtDir)) return;
            var name = Path.GetFileNameWithoutExtension(assetPath);
            bool isBackground = name == "fondo" || name == "duende";
            bool isSmooth = isBackground || name == "sierra" || name == "obstaculo" || name.StartsWith("portal_") || name == "estrella"; // glow suave: Point parpadea al girar

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = !isBackground;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = isSmooth ? FilterMode.Bilinear : FilterMode.Point;
            importer.wrapMode = isBackground ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        }
    }
}
