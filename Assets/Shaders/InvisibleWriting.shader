Shader "Group5/InvisibleWriting"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Writing Color", Color) = (0.25, 1, 0.85, 1)
        _RevealAngle ("Reveal Angle From Surface Normal", Range(0, 90)) = 55
        _AngleWidth ("Visible Half Width (Degrees)", Range(1, 40)) = 12
        _FadeWidth ("Fade Width (Degrees)", Range(1, 20)) = 6
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _RevealAngle, _AngleWidth, _FadeWidth;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 originWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.originWS = TransformObjectToWorld(float3(0,0,0));
                output.normalWS = TransformObjectToWorldNormal(float3(0,0,-1));
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 eye = _WorldSpaceCameraPos;
                #if defined(USING_STEREO_MATRICES)
                    eye = (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) * 0.5;
                #endif
                float cosine = dot(normalize(eye-input.originWS), normalize(input.normalWS));
                float angle = degrees(acos(clamp(cosine,-1.0,1.0)));
                float alpha = 1-smoothstep(_AngleWidth, _AngleWidth+_FadeWidth, abs(angle-_RevealAngle));
                half4 color = _Color;
                color.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
