using PressR.Graphics.Interfaces;
using UnityEngine;
using Verse;

namespace PressR.Graphics.Shaders
{
    public static class MpbConfigurators
    {
        public struct Payload
        {
            public float? Alpha { get; set; }
            public Color? TargetColor { get; set; }
            public Color? OriginalBaseColor { get; set; }
            public float? SaturationBlendFactor { get; set; }
            public float? BrightnessBlendFactor { get; set; }
            public float? Cutoff { get; set; }
            public float? EffectBlendFactor { get; set; }

            public Color? FillColor { get; set; }
            public Color? OutlineColor { get; set; }
            public float? EdgeSensitivity { get; set; }
        }

        public class GhostEffectConfigurator : IMpbConfigurator
        {
            public void Configure(MaterialPropertyBlock mpb, Payload payload)
            {
                Color finalColor = payload.TargetColor ?? Color.white;
                float alpha = payload.Alpha ?? 1.0f;
                finalColor.a *= alpha;

                mpb.SetColor(ShaderPropertyIDs.Color, finalColor);
            }
        }

        public class HSVColorizeCutoutConfigurator : IMpbConfigurator
        {
            private static readonly int ColorProp = Shader.PropertyToID("_Color");
            private static readonly int OriginalBaseColorProp = Shader.PropertyToID(
                "_OriginalBaseColor"
            );
            private static readonly int SaturationBlendFactorProp = Shader.PropertyToID(
                "_SaturationBlendFactor"
            );
            private static readonly int BrightnessBlendFactorProp = Shader.PropertyToID(
                "_BrightnessBlendFactor"
            );
            private static readonly int CutoffProp = Shader.PropertyToID("_Cutoff");
            private static readonly int EffectBlendFactorProp = Shader.PropertyToID(
                "_EffectBlendFactor"
            );

            public void Configure(MaterialPropertyBlock mpb, Payload payload)
            {
                Color targetColor = payload.TargetColor ?? Color.white;
                mpb.SetColor(ColorProp, targetColor);

                Color originalBaseColor = payload.OriginalBaseColor ?? Color.white;
                mpb.SetColor(OriginalBaseColorProp, originalBaseColor);

                float saturationBlendFactor = payload.SaturationBlendFactor ?? 0.5f;
                mpb.SetFloat(SaturationBlendFactorProp, saturationBlendFactor);

                float brightnessBlendFactor = payload.BrightnessBlendFactor ?? 0.0f;
                mpb.SetFloat(BrightnessBlendFactorProp, brightnessBlendFactor);

                float cutoff = payload.Cutoff ?? 0.5f;
                mpb.SetFloat(CutoffProp, cutoff);

                float effectBlendFactor = payload.EffectBlendFactor ?? 1.0f;
                mpb.SetFloat(EffectBlendFactorProp, effectBlendFactor);
            }
        }

        public class SobelEdgeDetectConfigurator : IMpbConfigurator
        {
            private static readonly int FillColorProp = Shader.PropertyToID("_FillColor");
            private static readonly int OutlineColorProp = Shader.PropertyToID("_OutlineColor");
            private static readonly int CutoffProp = Shader.PropertyToID("_Cutoff");
            private static readonly int EffectAlphaProp = Shader.PropertyToID("_EffectAlpha");
            private static readonly int EdgeSensitivityProp = Shader.PropertyToID(
                "_EdgeSensitivity"
            );

            public void Configure(MaterialPropertyBlock mpb, Payload payload)
            {
                Color fillColor = payload.FillColor ?? Color.clear;
                mpb.SetColor(FillColorProp, fillColor);

                Color outlineColor = payload.OutlineColor ?? Color.white;
                mpb.SetColor(OutlineColorProp, outlineColor);

                float cutoff = payload.Cutoff ?? 0.5f;
                mpb.SetFloat(CutoffProp, cutoff);

                float effectAlpha = payload.Alpha ?? 1.0f;
                mpb.SetFloat(EffectAlphaProp, effectAlpha);

                float edgeSensitivity = payload.EdgeSensitivity ?? 1.5f;
                mpb.SetFloat(EdgeSensitivityProp, edgeSensitivity);
            }
        }
    }
}
