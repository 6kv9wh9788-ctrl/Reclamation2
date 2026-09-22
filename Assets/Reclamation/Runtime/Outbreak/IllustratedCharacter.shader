Shader "Reclamation/IllustratedCharacter"
{
    Properties { [MainColor] _BaseColor("Palette", Color) = (0.3,0.5,0.4,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(world); return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                Light light = GetMainLight();
                half shade = dot(normalize(input.normalWS), light.direction);
                half band = shade > .45 ? 1 : shade > -.15 ? .74 : .46;
                half rim = abs(dot(normalize(input.normalWS), normalize(input.viewWS)));
                half ink = rim < .12 ? .35 : 1;
                return half4(_BaseColor.rgb * band * ink, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
