Shader "Unlit/SpecialFX/Cool Hologram"
{
    Properties
    {
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (1,1,1,1)
        _Transparency("Transparency", Range(0.0,0.5)) = 0.25
        _CutoutThresh("Cutout Threshold", Range(0.0,1.0)) = 0.2
        _Distance("Distance", Float) = 1
        _Amplitude("Amplitude", Float) = 1
        _Speed ("Speed", Float) = 1
        _Amount("Amount", Range(0.0,1.0)) = 1
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
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
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TintColor;
            float _Transparency;
            float _CutoutThresh;
            float _Distance;
            float _Amplitude;
            float _Speed;
            float _Amount;
            
            
            v2f vert (appdata v)
            {
                v2f o;
                float halfV = 50.0f;
                float normedY = (v.vertex.y + halfV) / (halfV * 2.0f);
                //float seamEdgeWidth = 0.1f;
                //float start = smoothstep(0.0f, seamEdgeWidth, normedY);
                //float end = 1.0f - smoothstep(1.0f - seamEdgeWidth, 1.0f, normedY);
                //float effect = 1.0f - (start * end);
                const float pi = 3.14159265359;
                const float pi2 = 2.0f * pi;
                float c = normedY * pi2;
                v.vertex.z += sin(c - _Time.y/*-_Time.y * _Speed + normedY * _Amplitude*/) * _Distance * _Amount;// * effect;
                float4 n = v.normal;
                o.normal = float4(n.x + sin(_Time.y), n.y, n.z, n.w);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) + _TintColor;
                col.a = _Transparency;
                clip(col.r - _CutoutThresh);
                return col;
            }
            ENDCG
        }
    }
}