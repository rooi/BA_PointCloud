// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/PointShader"
{
	/*
	This shader renders the given vertices as points with the given color.
	The point size is set to 30 (fixed), but unfortunately it doesn't seem to have any effect.
	*/
	Properties{
		_PointSize("Point Size", Float) = 5
		_ScreenWidth("Screen Width", Int) = 0
		_ScreenHeight("Screen Height", Int) = 0
		_BBSize("Bounding Box Size", Vector) = (0.0, 0.0, 0.0)
		_VisibleNodes("Visible Nodes", 2D) = "defaulttexture" {}
		_OctreeSpacing("Octree Spacing", float) = 0
		_MinSize("Min Size", float) = 5.0
		_MaxSize("Max Size", float) = 5.0
		_Adaptive_Point_Size("Enable Adaptive Point Size", int) = 0
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

			uniform bool _Adaptive_Point_Size;
			uniform sampler2D _VisibleNodes;
			uniform float _OctreeSpacing;
			uniform float _MinSize;
			uniform float _MaxSize;

			uniform float _vRadius;

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
				UNITY_DEFINE_INSTANCED_PROP(int, _ScreenWidth)
				UNITY_DEFINE_INSTANCED_PROP(int, _ScreenHeight)
				UNITY_DEFINE_INSTANCED_PROP(float, _FOV)
				UNITY_DEFINE_INSTANCED_PROP(float4x4, _InverseProjMatrix)
				UNITY_DEFINE_INSTANCED_PROP(float3, _BBSize)
			UNITY_INSTANCING_BUFFER_END(Props)	

				float getLOD(float3  position) {
				float3  offset = float3(0.0, 0.0, 0.0);
				float iOffset = 0.0;
				float depth = 0.0;


				float3 size = UNITY_ACCESS_INSTANCED_PROP(Props, _BBSize);
				float3 pos = position;

				for (float i = 0.0; i <= 1000.0; i++) {

					fixed4 value = tex2Dlod(_VisibleNodes, float4(iOffset / 2048.0, 0.0, 0.0, 0.0));

					int children = int(value.r * 255.0);
					float next = value.g * 255.0;
					int split = int(value.b * 255.0);

					if (next == 0.0) {
						return depth;
					}

					float3 splitv = float3(0.0, 0.0, 0.0);
					if (split == 1) {
						splitv.x = 1.0;
					}
					else if (split == 2) {
						splitv.y = 1.0;
					}
					else if (split == 4) {
						splitv.z = 1.0;
					}

					iOffset = iOffset + next;

					float factor = length(pos * splitv / size);
					if (factor < 0.5) {
						// left
						if (children == 0 || children == 2) {
							return depth;
						}
					}
					else {
						// right
						pos = pos - size * splitv * 0.5;
						if (children == 0 || children == 1) {
							return depth;
						}
						if (children == 3) {
							iOffset = iOffset + 1.0;
						}
					}
					size = size * ((1.0 - (splitv + 1.0) / 2.0) + 0.5);

					depth++;
				}


				return depth;
			}

			float getPointSizeAttenuation(float3 position) {
				return 0.5 * pow(1.3, getLOD(position));
			}

			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				o.position = UnityObjectToClipPos(v.position);
				o.color = v.color;

				float pointSize = UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize);
				float4 viewpos = float4(UnityObjectToViewPos(v.position), 1);
				o.position = mul(UNITY_MATRIX_P, viewpos);
				float slope = tan(UNITY_ACCESS_INSTANCED_PROP(Props, _FOV) / 2);
				if (_Adaptive_Point_Size > 0) {
					float projFactor = -0.5 * UNITY_ACCESS_INSTANCED_PROP(Props, _ScreenHeight) / (slope * viewpos.z);
					float r = _OctreeSpacing * 1.7;
					_vRadius = r;
					//#if defined fixed_point_size
					//					pointSize = size;
					//#elif defined attenuated_point_size
					//					if (uUseOrthographicCamera) {
					//						pointSize = size;
					//					}
					//					else {
					//						pointSize = size * spacing * projFactor;
					//						//pointSize = pointSize * projFactor;
					//					}
					//#elif defined adaptive_point_size
										//if (uUseOrthographicCamera) {
										//	float worldSpaceSize = 1.0 * size * r / getPointSizeAttenuation();
										//	pointSize = (worldSpaceSize / uOrthoWidth) * uScreenWidth;
										//}
										//else {

											//if (uIsLeafNode && false) {
											//	pointSize = size * spacing * projFactor;
											//}
											//else {
					float worldSpaceSize = 1.0 * UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize) * r / getPointSizeAttenuation(v.position);
					pointSize = worldSpaceSize * projFactor;
					//}
				//}
				//#endif
					pointSize = max(_MinSize, pointSize);
					pointSize = min(_MaxSize, pointSize);

					_vRadius = pointSize / projFactor;

					o.size = pointSize;
				}
				else {
					o.size = UNITY_ACCESS_INSTANCED_PROP(Props, _PointSize);
				}

				

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
