Shader "Sprites/Act2AshWorld"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Tint ("Tint", Color) =
            (0.75, 0.78, 0.77, 1)

        _MinBrightness (
            "Minimum Brightness",
            Range(0, 1)
        ) = 0.45

        _MaxBrightness (
            "Maximum Brightness",
            Range(0, 1.5)
        ) = 1.0

        _Contrast (
            "Contrast",
            Range(0.1, 2)
        ) = 1.0
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

        Cull Off
        Lighting Off
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;

            fixed4 _Tint;

            float _MinBrightness;
            float _MaxBrightness;
            float _Contrast;


            v2f vert(appdata v)
            {
                v2f o;

                o.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );

                o.uv =
                    v.uv;

                return o;
            }


            fixed4 frag(v2f i)
                : SV_Target
            {
                fixed4 tex =
                    tex2D(
                        _MainTex,
                        i.uv
                    );


                float luminance =
                    dot(
                        tex.rgb,
                        float3(
                            0.299,
                            0.587,
                            0.114
                        )
                    );


                luminance =
                    (luminance - 0.5)
                    * _Contrast
                    + 0.5;


                luminance =
                    saturate(
                        luminance
                    );


                float brightness =
                    lerp(
                        _MinBrightness,
                        _MaxBrightness,
                        luminance
                    );


                fixed3 finalColor =
                    _Tint.rgb *
                    brightness;


                return fixed4(
                    finalColor,
                    tex.a *
                    _Tint.a
                );
            }

            ENDCG
        }
    }
}