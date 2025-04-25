Shader "PressR/HSVColorizeCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _OriginalBaseColor ("Original Base Color", Color) = (1,1,1,1)
        _SaturationBlendFactor ("Saturation Blend Factor", Range(0.0, 1.0)) = 0.5
        _BrightnessBlendFactor ("Brightness Blend Factor", Range(0.0, 1.0)) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _EffectBlendFactor ("Effect Blend Factor", Range(0.0, 1.0)) = 1.0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OriginalBaseColor;
            float _SaturationBlendFactor;
            float _BrightnessBlendFactor;
            float _Cutoff;
            float _EffectBlendFactor;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if defined(PIXELSNAP_ON)
                o.vertex = UnityPixelSnap(UnityObjectToClipPos(v.vertex));
                #else
                o.vertex = UnityObjectToClipPos(v.vertex);
                #endif

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                clip(texColor.a - _Cutoff);

                float3 pixelOrigRGB = _OriginalBaseColor.rgb * texColor.rgb * i.color.rgb;

                float3 pixelOrigHSV = rgb2hsv(pixelOrigRGB);
                float3 tintHSV = rgb2hsv(_Color.rgb);

                float finalV;
                if (_BrightnessBlendFactor == 0.0)
                {
                    finalV = pixelOrigHSV.z;
                }
                else
                {
                    float distance = abs(pixelOrigHSV.z - tintHSV.z);
                    float dynamicFactor = clamp(_BrightnessBlendFactor * distance, 0.0, 1.0);
                    finalV = lerp(pixelOrigHSV.z, tintHSV.z, dynamicFactor);
                }

                float finalS = lerp(pixelOrigHSV.y, tintHSV.y, _SaturationBlendFactor);
                
                float3 finalHSV = float3(tintHSV.x, finalS, finalV);

                float3 hsvTintedRGB = hsv2rgb(finalHSV);

                float3 finalRGB = lerp(pixelOrigRGB, hsvTintedRGB, _EffectBlendFactor);

                return fixed4(finalRGB, 1.0);
            }
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Transparent/VertexLit"
}
