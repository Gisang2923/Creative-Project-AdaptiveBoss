Shader "UI/DeathSilhouette"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) =
            (1,1,1,1)

        _StencilComp
            ("Stencil Comparison", Float) = 8

        _Stencil
            ("Stencil ID", Float) = 0

        _StencilOp
            ("Stencil Operation", Float) = 0

        _StencilWriteMask
            ("Stencil Write Mask", Float) = 255

        _StencilReadMask
            ("Stencil Read Mask", Float) = 255

        _ColorMask
            ("Color Mask", Float) = 15
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

        Blend SrcAlpha OneMinusSrcAlpha

        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex :
                    POSITION;

                float2 texcoord :
                    TEXCOORD0;

                fixed4 color :
                    COLOR;
            };

            struct v2f
            {
                float4 vertex :
                    SV_POSITION;

                float2 texcoord :
                    TEXCOORD0;

                fixed4 color :
                    COLOR;
            };

            sampler2D _MainTex;

            v2f vert(
                appdata_t input)
            {
                v2f output;

                output.vertex =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.texcoord =
                    input.texcoord;

                output.color =
                    input.color;

                return output;
            }

            fixed4 frag(
                v2f input)
                : SV_Target
            {
                fixed4 textureColor =
                    tex2D(
                        _MainTex,
                        input.texcoord
                    );

                return fixed4(
                    1.0,
                    1.0,
                    1.0,
                    textureColor.a *
                    input.color.a
                );
            }

            ENDCG
        }
    }
}