Shader "VoodooStore/UI Alpha Aura Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0,0,1)
        _GlowIntensity ("Glow Intensity", Range(0,8)) = 2
        _GlowSpread ("Glow Spread", Range(0,24)) = 6

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowSpread;
            float4 _MainTex_TexelSize;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 offset = _MainTex_TexelSize.xy * _GlowSpread;
                float centerAlpha = tex2D(_MainTex, IN.texcoord).a;

                float glowAlpha = 0;
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + float2(offset.x, 0)).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + float2(0, offset.y)).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + offset).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord - offset).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + float2(offset.x, -offset.y)).a);
                glowAlpha = max(glowAlpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, offset.y)).a);

                float outsideAlpha = saturate((glowAlpha - centerAlpha * 0.7) * _GlowIntensity);
                fixed4 color = _GlowColor;
                color.a *= outsideAlpha * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
