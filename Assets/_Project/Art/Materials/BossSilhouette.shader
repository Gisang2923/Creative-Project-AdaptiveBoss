Shader "Custom/BossSilhouette"
{
    Properties
    {
        [PerRendererData]
        _MainTex("Sprite Texture", 2D) = "white" {}

        _Color(
            "Silhouette Color",
            Color
        ) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off

        Blend
            SrcAlpha
            OneMinusSrcAlpha

        Pass
        {
            Name "BossSilhouette"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS
                    : POSITION;

                float2 uv
                    : TEXCOORD0;

                float4 color
                    : COLOR;
            };


            struct Varyings
            {
                float4 positionHCS
                    : SV_POSITION;

                float2 uv
                    : TEXCOORD0;

                float4 color
                    : COLOR;
            };


            TEXTURE2D(_MainTex);

            SAMPLER(
                sampler_MainTex
            );


            CBUFFER_START(
                UnityPerMaterial
            )

                float4 _Color;

            CBUFFER_END


            Varyings vert(
                Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                output.uv =
                    input.uv;

                output.color =
                    input.color;

                return output;
            }


            half4 frag(
                Varyings input)
                : SV_Target
            {
                half4 sprite =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );


                half alpha =
                    sprite.a *
                    input.color.a *
                    _Color.a;


                clip(
                    alpha - 0.001
                );


                return half4(
                    _Color.rgb,
                    alpha
                );
            }

            ENDHLSL
        }
    }
}