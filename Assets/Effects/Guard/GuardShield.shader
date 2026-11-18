// Transparent energy shield. Back faces draw first (dim), then front faces with a fresnel rim and a view-space sheen.
// Output is premultiplied alpha.
Shader "LoneWolf/GuardShield"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.06, 0.16, 0.62, 1)
        _CenterAlpha ("Center Alpha", Range(0, 1)) = 0.04
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.9
        _RimBrightness ("Rim Brightness", Float) = 2.2
        _RimPower ("Rim Power", Float) = 3
        _BackFaceStrength ("Back Face Strength", Range(0, 1)) = 0.3
        [HDR] _SheenColor ("Sheen Color", Color) = (0.75, 0.9, 1, 1)
        _SheenStrength ("Sheen Strength", Float) = 0.55
        _SheenPower ("Sheen Power", Float) = 24
        _SheenDirection ("Sheen Direction (View Space)", Vector) = (-0.45, 0.5, 0.75, 0)
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _CenterAlpha;
            half _RimAlpha;
            half _RimBrightness;
            half _RimPower;
            half _BackFaceStrength;
            half4 _SheenColor;
            half _SheenStrength;
            half _SheenPower;
            float4 _SheenDirection;
            half _Fade;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS : TEXCOORD0;
            float3 viewDirWS : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
            return output;
        }

        // Returns premultiplied color. faceSign is 1 for front faces and -1 for back faces.
        half4 Shade(Varyings input, half faceSign, half strength, half sheen)
        {
            float3 normalWS = normalize(input.normalWS) * faceSign;
            float3 viewDirWS = normalize(input.viewDirWS);

            half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower);
            half alpha = lerp(_CenterAlpha, _RimAlpha, fresnel);
            half3 color = _Color.rgb * lerp(1.0h, _RimBrightness, fresnel) * alpha;

            float3 normalVS = normalize(TransformWorldToViewDir(normalWS));
            half highlight = pow(saturate(dot(normalVS, normalize(_SheenDirection.xyz))), _SheenPower) * _SheenStrength * sheen;
            color += _SheenColor.rgb * highlight;
            alpha = saturate(alpha + highlight);

            half fade = _Fade * strength;
            return half4(color * fade, alpha * fade);
        }
        ENDHLSL

        Pass
        {
            Name "BackFaces"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            half4 Frag(Varyings input) : SV_Target
            {
                return Shade(input, -1.0h, _BackFaceStrength, 0.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "FrontFaces"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            half4 Frag(Varyings input) : SV_Target
            {
                return Shade(input, 1.0h, 1.0h, 1.0h);
            }
            ENDHLSL
        }
    }
}
