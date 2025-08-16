using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PressR.Graphics
{
    [StaticConstructorOnStartup]
    public static class ShaderManager
    {
        public static readonly Shader HSVColorizeCutoutShader;
        public static readonly Shader SobelEdgeDetectShader;

        private const string HSVColorizeCutoutAssetName = "HSVColorizeCutout";
        private const string SobelEdgeDetectAssetName = "SobelEdgeDetect";

        static ShaderManager()
        {
            ModContentPack contentPack = LoadedModManager.GetMod<PressRMod>().Content;
            List<AssetBundle> loadedBundles = contentPack.assetBundles.loadedAssetBundles;

            HSVColorizeCutoutShader = LoadShaderFromBundles(
                loadedBundles,
                HSVColorizeCutoutAssetName
            );
            SobelEdgeDetectShader = LoadShaderFromBundles(loadedBundles, SobelEdgeDetectAssetName);
        }

        private static Shader LoadShaderFromBundles(List<AssetBundle> bundles, string shaderName)
        {
            Shader shader = bundles
                .Select(bundle => bundle.LoadAsset<Shader>(shaderName))
                .FirstOrDefault(s => s != null);

            if (shader == null)
            {
                Log.Error(
                    $"[Press-R] Could not load shader '{shaderName}' from any loaded asset bundle."
                );
            }
            return shader;
        }
    }
}
