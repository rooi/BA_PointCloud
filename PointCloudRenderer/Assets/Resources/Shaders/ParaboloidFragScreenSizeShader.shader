﻿Shader "Custom/ParaboloidFragScreenSizeShader"
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
		[Toggle] _Cones("Cones", Int) = 0
		_OctreeSpacing("Octree Spacing", float) = 0
		_MinSize("Min Size", float) = 2.0
		_MaxSize("Max Size", float) = 50.0
		_Adaptive_Point_Size("Enable Adaptive Point Size", int) = 0
	}

		SubShader
		{
			LOD 200

			Pass
			{
				Cull off

				CGPROGRAM
				//#pragma enable_d3d11_debug_symbols
				#pragma vertex vert
				#pragma geometry geom
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				uniform int _Adaptive_Point_Size;
				uniform float _OctreeSpacing;
				uniform float _MinSize;
				uniform float _MaxSize;

				struct VertexInput
				{
					float4 position : POSITION;
					float4 color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct VertexMiddle {
					float4 position : SV_POSITION;
					float4 size : POINTSIZE;
					float4 color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				struct VertexOutput
				{
					float4 position : SV_POSITION;
					float4 viewposition: TEXCOORD1;
					float4 color : COLOR;
					float2 uv : TEXCOORD0;
					float size : PSIZE;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				}; 

				struct FragmentOutput
				{
					float4 color : COLOR;
					float depth : SV_DEPTH;
				};

				UNITY_INSTANCING_BUFFER_START(Props)
					UNITY_DEFINE_INSTANCED_PROP(float, _PointSize)
					UNITY_DEFINE_INSTANCED_PROP(int, _ScreenWidth)
					UNITY_DEFINE_INSTANCED_PROP(int, _ScreenHeight)
					UNITY_DEFINE_INSTANCED_PROP(int, _Circles)
					UNITY_DEFINE_INSTANCED_PROP(int, _Cones)
					UNITY_DEFINE_INSTANCED_PROP(float, _FOV)
					UNITY_DEFINE_INSTANCED_PROP(float4x4, _InverseProjMatrix)
				UNITY_INSTANCING_BUFFER_END(Props)

				VertexMiddle vert(VertexInput v) {
					VertexMiddle o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_OUTPUT(VertexMiddle, o)
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					
					float pointSize = UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize);
					float4 viewpos = float4(UnityObjectToViewPos(v.position), 1);
					o.position = mul(UNITY_MATRIX_P, viewpos);
					float slope = tan(UNITY_ACCESS_INSTANCED_PROP(Props, _FOV) / 2);
					o.size = -UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize) * slope * viewpos.z * 2 / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenHeight);
					//if (_Adaptive_Point_Size > 0) {
					//	o.size = o.size * _OctreeSpacing;
					//}
					
					o.color = v.color;
					return o;
				}

				[maxvertexcount(4)]
				void geom(point VertexMiddle input[1], inout TriangleStream<VertexOutput> outputStream) {
					
					float xsize = 1.0;
					float ysize = 1.0;
					//if (_Adaptive_Point_Size > 0) {
					//	xsize = input[0].size;
					//	ysize = input[0].size;
					//}
					
					xsize = xsize / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenWidth);
					ysize = ysize / UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenHeight);

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
					out1.position = out1.position / out1.position.w;
					out1.viewposition = mul(UNITY_ACCESS_INSTANCED_PROP(Props, _InverseProjMatrix), out1.position);
					out1.viewposition /= out1.viewposition.w;
					out1.size = input[0].size;

					out2.position = input[0].position;
					out2.color = input[0].color;
					out2.uv = float2(1.0f, 1.0f);
					out2.position.x += out2.position.w * xsize;
					out2.position.y += out2.position.w * ysize;
					out2.position = out2.position / out2.position.w;
					out2.viewposition = mul(UNITY_ACCESS_INSTANCED_PROP(Props, _InverseProjMatrix), out2.position);
					out2.viewposition /= out2.viewposition.w;
					out2.size = input[0].size;

					out3.position = input[0].position;
					out3.color = input[0].color;
					out3.uv = float2(1.0f, -1.0f);
					out3.position.x += out3.position.w * xsize;
					out3.position.y -= out3.position.w * ysize;
					out3.position = out3.position / out3.position.w;
					out3.viewposition = mul(UNITY_ACCESS_INSTANCED_PROP(Props, _InverseProjMatrix), out3.position);
					out3.viewposition /= out3.viewposition.w;
					out3.size = input[0].size;

					out4.position = input[0].position;
					out4.color = input[0].color;
					out4.uv = float2(-1.0f, -1.0f);
					out4.position.x -= out4.position.w * xsize;
					out4.position.y -= out4.position.w * ysize;
					out4.position = out4.position / out4.position.w;
					out4.viewposition = mul(UNITY_ACCESS_INSTANCED_PROP(Props, _InverseProjMatrix), out4.position);
					out4.viewposition /= out4.viewposition.w;
					out4.size = input[0].size;


					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out1);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out2);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out3);
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out4);

					outputStream.Append(out1);
					outputStream.Append(out2);
					outputStream.Append(out4);
					outputStream.Append(out3);
				}

				FragmentOutput frag(VertexOutput o)  {
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(o);
					FragmentOutput fragout;
					float uvlen = o.uv.x*o.uv.x + o.uv.y*o.uv.y;
					if (UNITY_ACCESS_INSTANCED_PROP(Props, _Circles) >= 0.5 && uvlen > 1) {
						discard;
					}
					if (UNITY_ACCESS_INSTANCED_PROP(Props, _Cones) < 0.5) {
						o.viewposition.z += (1 - uvlen) * o.size;
					}
					else {
						o.viewposition.z += (1 - sqrt(uvlen)) * o.size;
					}
					float4 pos = mul(UNITY_MATRIX_P, o.viewposition);
					pos /= pos.w;
					fragout.depth = pos.z;
					fragout.color = o.color;
					return fragout;
				}

			ENDCG
		}
	}
}
