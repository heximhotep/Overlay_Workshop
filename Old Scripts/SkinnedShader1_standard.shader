// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/SkinnedShader1: Skinned Shader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SkinDegree ("Skin Degree", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "LightMode"="ForwardBase" }
		LOD 100

		Pass
		{
			Tags  {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag


#pragma multi_compile_fwdbase multi_compile_fog nolightmap nodirlightmap nodynlightmap novertexlight

			#include "AutoLight.cginc"
			#include "Lighting.cginc"
			#include "UnityCG.cginc"

			// make fog work
			

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint id : SV_VertexID;
				float4 normal : NORMAL0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				SHADOW_COORDS(1)
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _SkinDegree;
			StructuredBuffer<float4> boneIndices;
			StructuredBuffer<float4> weights;
			StructuredBuffer<float4x4> xforms;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 totalLocalPos = float4(0, 0, 0, 0);
				float4 weight = weights[v.id];
				float4 boneIdx = boneIndices[v.id];
				for (int i = 0; i < 4; i++)
				{
					float4x4 jointXform = xforms[(int)(boneIdx[i])];
					float4 posePosition = mul(jointXform, v.vertex);
					totalLocalPos += posePosition * weight[i];
				}
				float4x4 myMVP = UNITY_MATRIX_P;
				myMVP = mul(myMVP, UNITY_MATRIX_V);
				myMVP = mul(myMVP, UNITY_MATRIX_M);
				o.vertex = UnityObjectToClipPos(totalLocalPos * _SkinDegree + v.vertex * (1 - _SkinDegree));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb;
				o.ambient = ShadeSH9(half4(worldNormal, 1));
				UNITY_TRANSFER_FOG(o,o.vertex);
				TRANSFER_SHADOW(o);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);

				fixed shadow = SHADOW_ATTENUATION(i);
				// apply fog
				fixed3 lighting = i.diff * shadow + i.ambient;
				col.rgb *= lighting;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
