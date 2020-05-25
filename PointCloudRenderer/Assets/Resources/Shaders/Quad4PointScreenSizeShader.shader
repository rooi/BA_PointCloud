// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Quad4PointScreenSizeShader"
{
	/*
	This shader renders the given vertices as circles with the given color. The uv-coordinates are given as offset-vectors ((-1,-1), (-1,1) etc.) which then are multiplied with the wanted point size.
	The point size is the radius of the circle given in pixel
	*/
	Properties{
		_PointSize("Point Size", Float) = 5
		_ScreenWidth("Screen Width", Int) = 0
		_ScreenHeight("Screen Height", Int) = 0
		[Toggle] _Circles("Circles", Int) = 0
	}

	SubShader
	{
		LOD 200

		Pass
		{
			Cull off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct VertexInput
			{
				float4 position : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _PointSize)
				UNITY_DEFINE_INSTANCED_PROP(int, _ScreenWidth)
				UNITY_DEFINE_INSTANCED_PROP(int, _ScreenHeight)
				UNITY_DEFINE_INSTANCED_PROP(int, _Circles)
			UNITY_INSTANCING_BUFFER_END(Props)			

			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					
				o.position = UnityObjectToClipPos(v.position);
				o.position.x += v.uv.x * o.position.w * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize) / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenWidth);
				o.position.y += v.uv.y * o.position.w * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize) / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenHeight);
				o.color = v.color;
				o.uv = v.uv;
				return o;
			}

			float4 frag(VertexOutput o) : COLOR{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(o);
				if (UNITY_ACCESS_INSTANCED_PROP(Props, _Circles) >= 0.5 && o.uv.x*o.uv.x + o.uv.y*o.uv.y > 1) {
					discard;
				}
				return o.color;
			}

			ENDCG
		}
	}
}
