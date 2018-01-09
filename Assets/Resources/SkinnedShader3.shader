// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/SkinnedShader3"
{
	Properties
	{
		_MainTex ("Diffuse Map", 2D) = "white" {}
		_NormalMap("Normal Map", 2D) = "bump" {}
		_SpecMap("Specular Map", 2D) = "black" {}
		_SkinDegree ("Skin Degree", Range(0, 1)) = 1
		_BumpDepth ("Normal Degree", Range(-2, 2)) = 1
	}
	SubShader
	{
			Tags{ "RenderType" = "Opaque" }

		LOD 100

		Pass
		{
			Tags{"LightMode" = "ForwardBase"}
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
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 uv : TEXCOORD0;
				uint id : SV_VertexID;
				uint instanceId : SV_InstanceID;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalWorld : TEXCOORD2;
				float3 tangentWorld : TEXCOORD3;
				float3 binormalWorld : TEXCOORD4;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			int numJoints;

			sampler2D _MainTex;
			sampler2D _NormalMap;
			sampler2D _SpecMap;
			float4 _MainTex_ST;
			float4 _NormalMap_ST;
			float4 _SpecMap_ST;

			float _SkinDegree;
			float _BumpDepth;
			

			uniform StructuredBuffer<float4> boneIndices;
			uniform StructuredBuffer<float4> weights;
			uniform StructuredBuffer<float4x4> xforms;
			uniform StructuredBuffer<float4x4> instanceXforms;
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
				float4 weightedLocalPos = totalLocalPos * _SkinDegree + v.vertex * (1 - _SkinDegree);

				o.posWorld = mul(unity_ObjectToWorld, weightedLocalPos);
				o.vertex = UnityObjectToClipPos(weightedLocalPos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.normalWorld = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);
				o.tangentWorld = normalize(mul(unity_ObjectToWorld, v.tangent).xyz);
				o.binormalWorld = normalize(cross(o.normalWorld, o.tangentWorld));

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
				float3 lightDirection;
				float atten;

				if (_WorldSpaceLightPos0.w == 0)
				{
					atten = 1;
					lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				}
				else
				{
					float3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
					float distance = length(fragmentToLightSource);
					float atten = 1 / distance;
					lightDirection = normalize(fragmentToLightSource);
				}

				

				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
				fixed4 nor = tex2D(_NormalMap, i.uv * _NormalMap_ST.xy + _NormalMap_ST.zw);
				fixed4 specCol = tex2D(_SpecMap, i.uv * _SpecMap_ST.xy + _SpecMap_ST.zw);

				float3 localCoords = float3(2.0 * nor.ag - float2(1, 1), _BumpDepth);

				float3x3 local2WorldTranspose = float3x3(i.tangentWorld, i.binormalWorld, i.normalWorld);

				float3 normalDirection = normalize(mul(localCoords, local2WorldTranspose));

				float3 diffuseReflection = atten * _LightColor0.rgb * saturate(dot(normalDirection, lightDirection));
				float3 specularReflection = diffuseReflection * specCol.rgb * pow(saturate(dot(reflect(-lightDirection, normalDirection), viewDirection)), 1);

				float3 lightFinal = diffuseReflection + specularReflection + UNITY_LIGHTMODEL_AMBIENT.rgb;

				return float4(col.xyz * lightFinal, 1);

				// apply fog
				//return col;
			}
			ENDCG
		}
	}
}
