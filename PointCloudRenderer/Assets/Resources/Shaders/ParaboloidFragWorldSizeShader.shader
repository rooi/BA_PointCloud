
Shader "Custom/ParaboloidFragWorldSizeShader"
{
	/*
	This shader renders the given vertices as circles with the given color.
	The point size is the radius of the circle given in WORLD COORDINATES
	Implemented using geometry shader.
	Interpolation is done by creating screen facing paraboloids in the fragment shader!
	*/
	Properties{
		_PointSize("Point Size", Float) = 5
		[Toggle] _Circles("Circles", Int) = 0
		[Toggle] _Cones("Cones", Int) = 0
	}

	SubShader
	{
		LOD 200

		Pass
		{
			Cull off

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			struct VertexInput
			{
				float4 position : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexMiddle {
				float4 position : SV_POSITION;
				float4 color : COLOR;
				float4 R : NORMAL0;
				float4 U : NORMAL1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct VertexOutput
			{
				float4 position : SV_POSITION;
				float4 viewposition : TEXCOORD1;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct FragmentOutput {
				float4 color : SV_TARGET;
				float depth : SV_DEPTH;
			};
			
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _PointSize)
				UNITY_DEFINE_INSTANCED_PROP(int, _Circles)
				UNITY_DEFINE_INSTANCED_PROP(int, _Cones)
			UNITY_INSTANCING_BUFFER_END(Props)

			VertexMiddle vert(VertexInput v) {
				VertexMiddle o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(VertexMiddle, o)
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					
				o.position = v.position;
				o.color = v.color;
				float3 view = normalize(UNITY_MATRIX_IT_MV[2].xyz);
				float3 upvec = normalize(UNITY_MATRIX_IT_MV[1].xyz);
				float3 R = normalize(cross(view, upvec));
				o.U = float4(upvec * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize), 0);
				o.R = -float4(R * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize), 0);
				return o;
			}

			[maxvertexcount(4)]
			void geom(point VertexMiddle input[1], inout TriangleStream<VertexOutput> outputStream) {
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
				out1.position += (-input[0].R + input[0].U);
				out1.viewposition = mul(UNITY_MATRIX_MV, out1.position);
				out1.position = UnityObjectToClipPos(out1.position);
				
				out2.position = input[0].position;
				out2.color = input[0].color;
				out2.uv = float2(1.0f, 1.0f);
				out2.position += (input[0].R + input[0].U);
				out2.viewposition = mul(UNITY_MATRIX_MV, out2.position);
				out2.position = UnityObjectToClipPos(out2.position);
				
				out3.position = input[0].position;
				out3.color = input[0].color;
				out3.uv = float2(1.0f, -1.0f);
				out3.position += (input[0].R - input[0].U);
				out3.viewposition = mul(UNITY_MATRIX_MV, out3.position);
				out3.position = UnityObjectToClipPos(out3.position);
				
				out4.position = input[0].position;
				out4.color = input[0].color;
				out4.uv = float2(-1.0f, -1.0f);
				out4.position += (-input[0].R - input[0].U);
				out4.viewposition = mul(UNITY_MATRIX_MV, out4.position);
				out4.position = UnityObjectToClipPos(out4.position);
				
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out1);
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out2);
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out3);
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input[0], out4);
					
				outputStream.Append(out1);
				outputStream.Append(out2);
				outputStream.Append(out4);
				outputStream.Append(out3);
			}

			FragmentOutput frag(VertexOutput o) {
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(o);
				FragmentOutput fragout;
				float uvlen = o.uv.x*o.uv.x + o.uv.y*o.uv.y;
				if (UNITY_ACCESS_INSTANCED_PROP(Props, _Circles) >= 0.5 && uvlen > 1) {
					discard;
				}
				if (UNITY_ACCESS_INSTANCED_PROP(Props, _Cones) < 0.5) {
					o.viewposition.z += (1 - uvlen) * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize);
				}
				else {
					o.viewposition.z += (1 - sqrt(uvlen)) * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize);
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
