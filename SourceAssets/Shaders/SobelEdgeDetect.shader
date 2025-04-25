Shader "PressR/SobelEdgeDetect"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillColor ("Fill Color", Color) = (1, 1, 1, 0.5)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _EffectAlpha ("Effect Alpha", Range(0.0, 1.0)) = 1.0
        _EdgeSensitivity ("Edge Sensitivity", Range(0.1, 5.0)) = 1.5
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [_ZTest]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _FillColor;
            fixed4 _OutlineColor;
            float _Cutoff;
            float _EffectAlpha;
            float _EdgeSensitivity;

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
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float luminance(float2 uv, float cutoff_threshold)
            {
                fixed4 sampleColor = tex2D(_MainTex, uv);
                if (sampleColor.a < cutoff_threshold) return 0.0;
                return dot(sampleColor.rgb, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                float cutoffThreshold = _Cutoff * 0.1;
                if (texColor.a < cutoffThreshold)
                {
                    return fixed4(0, 0, 0, 0);
                }

                fixed4 baseFillColor = _FillColor * i.color;
                baseFillColor.a *= _EffectAlpha;

                float dx = _MainTex_TexelSize.x;
                float dy = _MainTex_TexelSize.y;

                float l00 = luminance(i.texcoord + float2(- dx, dy), cutoffThreshold); float l10 = luminance(i.texcoord + float2(0, dy), cutoffThreshold); float l20 = luminance(i.texcoord + float2(dx, dy), cutoffThreshold);
                float l01 = luminance(i.texcoord + float2(- dx, 0), cutoffThreshold); /* l11 = luminance(i.texcoord) */ float l21 = luminance(i.texcoord + float2(dx, 0), cutoffThreshold);
                float l02 = luminance(i.texcoord + float2(- dx, - dy), cutoffThreshold); float l12 = luminance(i.texcoord + float2(0, - dy), cutoffThreshold); float l22 = luminance(i.texcoord + float2(dx, - dy), cutoffThreshold);

                float Gx = (l20 + 2.0 * l21 + l22) - (l00 + 2.0 * l01 + l02);
                float Gy = (l00 + 2.0 * l10 + l20) - (l02 + 2.0 * l12 + l22);

                float edgeStrength = saturate((abs(Gx) + abs(Gy)) * _EdgeSensitivity);

                fixed4 baseOutlineColor = _OutlineColor * i.color;
                baseOutlineColor.a *= _EffectAlpha;

                fixed4 finalColor = lerp(baseFillColor, baseOutlineColor, edgeStrength);

                return finalColor;
            }
            ENDCG
        }
    }

    Fallback "Legacy Shaders/Transparent/VertexLit"
}
