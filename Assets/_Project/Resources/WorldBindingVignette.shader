Shader "UI/WorldBindingVignette"
{
    Properties
    {
        _VignetteColor ("Vignette Color", Color) = (0, 0, 0, 1)
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1.777778
        _Radius ("Clear Radius", Range(0, 2)) = 0.5
        _Softness ("Edge Softness", Range(0.001, 2)) = 0.5
        _Strength ("Strength", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest Always
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

            float4 _VignetteColor;
            float4 _Center;
            float _Aspect;
            float _Radius;
            float _Softness;
            float _Strength;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // Height-normalized distance keeps the circular falloff
                // circular on wide and narrow displays alike.
                float2 offset = (input.uv - _Center.xy) * float2(_Aspect, 1);
                float distanceFromCenter = length(offset);
                float edge = smoothstep(_Radius,
                    _Radius + max(_Softness, 0.001), distanceFromCenter);
                return fixed4(_VignetteColor.rgb,
                    saturate(edge * _Strength * _VignetteColor.a));
            }
            ENDCG
        }
    }
}
