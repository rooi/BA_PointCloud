// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/QuadGeoScreenSizeShader"
{
	/*
	This shader renders the given vertices as circles with the given color.
	The point size is the radius of the circle given in pixel
	Implemented using geometry shader
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
				#pragma require geometry
				#pragma vertex vert
				#pragma geometry geom
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"
				
				// Use Shader model 3.0 target
				#pragma target 3.0

				struct VertexInput
				{
					float4 position : POSITION;
					float4 color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct VertexMiddle {
					float4 position : SV_POSITION;
					float4 color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
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

				VertexMiddle vert(VertexInput v) {
					VertexMiddle o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_OUTPUT(VertexMiddle, o)
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					o.position = UnityObjectToClipPos(v.position);
					o.color = v.color;
					return o;
				}

				[maxvertexcount(4)]
				void geom(point VertexMiddle input[1], inout TriangleStream<VertexOutput> outputStream) {
					float xsize = UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize) / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenWidth);
					float ysize = UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize) / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenHeight);
					UNITY_SETUP_INSTANCE_ID(input[0]);
					
					VertexOutput out1;
					VertexOutput out2;
					VertexOutput out3;
					VertexOutput out4;
					
					UNITY_INITIALIZE_OUTPUT(VertexOutput, out1)
					UNITY_INITIALIZE_OUTPUT(VertexOutput, out2)
					UNITY_INITIALIZE_OUTPUT(VertexOutput, out3)
					UNITY_INITIALIZE_OUTPUT(VertexOutput, out4)
					
					out1.position = input[0].position;
					out1.color = input[0].color;
					out1.uv = float2(-1.0f, 1.0f);
					out1.position.x -= out1.position.w * xsize;
					out1.position.y += out1.position.w * ysize;
					
					out2.position = input[0].position;
					out2.color = input[0].color;
					out2.uv = float2(1.0f, 1.0f);
					out2.position.x += out2.position.w * xsize;
					out2.position.y += out2.position.w * ysize;
					
					out3.position = input[0].position;
					out3.color = input[0].color;
					out3.uv = float2(1.0f, -1.0f);
					out3.position.x += out3.position.w * xsize;
					out3.position.y -= out3.position.w * ysize;
					
					out4.position = input[0].position;
					out4.color = input[0].color;
					out4.uv = float2(-1.0f, -1.0f);
					out4.position.x -= out4.position.w * xsize;
					out4.position.y -= out4.position.w * ysize;
					
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out1);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out2);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out3);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out4);
					
					outputStream.Append(out1);
					outputStream.Append(out2);
					outputStream.Append(out4);
					outputStream.Append(out3);
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
