Shader "DwarfsCrypt/SpriteDissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        _NoiseScale ("Noise Scale", Float) = 12
        _EdgeWidth ("Edge Width", Range(0,0.5)) = 0.06
        [HDR] _EdgeColor ("Edge Color", Color) = (2, 0.9, 0.25, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "RenderType"       = "Transparent"
            "RenderPipeline"   = "UniversalPipeline"
            "IgnoreProjector"  = "True"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            // URP 2D Renderer transparent pass.
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 worldUV    : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _DissolveAmount;
                float  _NoiseScale;
                float  _EdgeWidth;
                float4 _EdgeColor;
            CBUFFER_END

            // Cheap hash + value noise so the effect needs no noise texture asset.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                OUT.positionCS = pos.positionCS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color      = IN.color * _Color;
                // World-space XY so all sprite parts dissolve as one cohesive mass.
                OUT.worldUV    = pos.positionWS.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float noise = valueNoise(IN.worldUV * _NoiseScale);

                // Anything below the moving threshold is cut away.
                float threshold = _DissolveAmount * (1.0 + _EdgeWidth);
                clip(tex.a - 0.001);          // respect the sprite's own transparency
                clip(noise - threshold);

                // Pixels right at the cut line glow with the edge color.
                float edge = 1.0 - smoothstep(threshold, threshold + _EdgeWidth, noise);
                half4 col = tex;
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, saturate(edge * _EdgeColor.a));

                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
