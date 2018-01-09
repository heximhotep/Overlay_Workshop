// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/SkinnedShader3"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SkinDegree ("Skin Degree", Range(0, 1)) = 1
	}
	SubShader
	{
			Tags{ "RenderType" = "Opaque" }

		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


			#pragma target 5.0


			#pragma multi_compile_instancing

			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#include "UnityCG.cginc"

			// make fog work
			

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint id : SV_VertexID;
				uint instanceId : SV_InstanceID;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;
			int numJoints;
			float4 _MainTex_ST;
			float _SkinDegree;
			StructuredBuffer<float4> boneIndices;
			StructuredBuffer<float4> weights;
			StructuredBuffer<float4x4> xforms;
			StructuredBuffer<float4x4> instanceXforms;
			v2f vert (appdata v)
			{
				v2f o;
				float4 totalLocalPos = float4(0, 0, 0, 0);
				float4 weight = weights[v.id];
				float4 boneIdx = boneIndices[v.id];
				int instanceOffset = v.instanceId * numJoints;
				for (int i = 0; i < 4; i++)
				{
					float4x4 jointXform = xforms[instanceOffset + (int)(boneIdx[i])];
					float4 posePosition = mul(jointXform, v.vertex);
					totalLocalPos += posePosition * weight[i];
				}
				totalLocalPos = mul(instanceXforms[v.instanceId], totalLocalPos);
				float4x4 myMVP = UNITY_MATRIX_P;
				myMVP = mul(myMVP, UNITY_MATRIX_V);
				myMVP = mul(myMVP, UNITY_MATRIX_M);
				o.vertex = UnityObjectToClipPos(totalLocalPos * _SkinDegree + v.vertex * (1 - _SkinDegree));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				// apply fog
				return col;
			}
			ENDCG
		}
	}
}
