Shader "Custom/Sprite Dice Depth Cutout"
{
Properties
{
    [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    _Color ("Tint", Color) = (1,1,1,1)
    _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
}

SubShader
{
    Tags
    {
        "RenderPipeline"="UniversalPipeline"
        "Queue"="AlphaTest"
        "RenderType"="TransparentCutout"
        "IgnoreProjector"="True"
        "CanUseSpriteAtlas"="True"
    }

    Cull Off
    Lighting Off
    ZWrite On
    ZTest LEqual
    Blend Off

    Pass
    {
        Name "SpriteCutout"
        Tags { "LightMode"="Universal2D" }
        Offset 1, 1

        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half _Cutoff;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color * _Color;
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            clip(texColor.a - _Cutoff);
            return texColor;
        }
        ENDHLSL
    }
}
}
