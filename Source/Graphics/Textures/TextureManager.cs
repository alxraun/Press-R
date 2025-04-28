using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Textures
{
    [StaticConstructorOnStartup]
    public static class TextureManager
    {
        private static readonly Dictionary<string, Texture2D> _cachedTextures =
            new Dictionary<string, Texture2D>();
        private static readonly AssetBundle _textureBundle;
        private const string BundleName = "alx_pressr_textures";

        static TextureManager()
        {
            ModContentPack contentPack = LoadedModManager.GetMod<PressRMod>()?.Content;
            if (contentPack == null)
            {
                return;
            }

            _textureBundle = contentPack.assetBundles?.loadedAssetBundles?.FirstOrDefault(b =>
                b != null && b.name == BundleName
            );

            if (_textureBundle == null)
            {
                Log.Error(
                    $"[Press-R TextureManager] AssetBundle '{BundleName}' not found or not loaded for mod '{contentPack.Name}'. Texture loading will fail."
                );
            }
        }

        public static Texture2D Get(string fullAssetPath, bool reportFailure = true)
        {
            if (_cachedTextures.TryGetValue(fullAssetPath, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            if (_textureBundle == null)
            {
                _cachedTextures[fullAssetPath] = BaseContent.BadTex;
                return BaseContent.BadTex;
            }

            Texture2D loadedTexture = _textureBundle.LoadAsset<Texture2D>(fullAssetPath);

            if (loadedTexture != null)
            {
                _cachedTextures[fullAssetPath] = loadedTexture;
                return loadedTexture;
            }
            else
            {
                _cachedTextures[fullAssetPath] = BaseContent.BadTex;
                return BaseContent.BadTex;
            }
        }
    }
}
