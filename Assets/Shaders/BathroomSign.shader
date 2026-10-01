Shader "Group5/BathroomSign" {
Properties { _MainTex ("Font", 2D) = "white" {} }
SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" } Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
V vert(A a){V o; UNITY_SETUP_INSTANCE_ID(a); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;return o;}
half4 frag(V i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return half4(i.color.rgb,i.color.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a);}
ENDHLSL
} } }

