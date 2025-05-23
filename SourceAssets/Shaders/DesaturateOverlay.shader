Shader "PressR/DesaturateOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Overlay Color", Color) = (1,1,1,1) 
        _Saturation ("Saturation", Range(0.0, 1.0)) = 0.0 
    }
    SubShader
    {  
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {   
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; 
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR; 
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color; 
            float _Saturation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {            
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Color;

                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 gray = float3(luminance, luminance, luminance);

                float3 finalColor = lerp(gray, col.rgb, _Saturation);
    
                return fixed4(finalColor, col.a);
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit" 
} 
