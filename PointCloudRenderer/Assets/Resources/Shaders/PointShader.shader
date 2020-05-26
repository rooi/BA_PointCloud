// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/PointShader"
{
	/*
	This shader renders the given vertices as points with the given color.
	The point size is set to 30 (fixed), but unfortunately it doesn't seem to have any effect.
	*/
	Properties{
		_PointSize("Point Size", Float) = 5
	}

	SubShader
	{
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct VertexInput
			{
				float4 position : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float size : PSIZE;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _PointSize)
			UNITY_INSTANCING_BUFFER_END(Props)	

			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.position = UnityObjectToClipPos(v.position);
				o.color = v.color;
				o.size = UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize);
				return o;
			}

			float4 frag(VertexOutput o) : COLOR{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(o);
				return o.color;
			}

			ENDCG
		}
	}
}
