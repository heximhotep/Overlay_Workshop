Shader "Instanced/InstancedShader_VertFrag0"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma target 5.0
			#pragma multi_compile_instancing


		
			#include "UnityCG.cginc"

	

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				uint id : SV_InstanceID;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			StructuredBuffer<float4x4> xforms;
			StructuredBuffer<float> times;

			v2f vert (appdata v)
			{
				float4x4 myM = xforms[v.id];
				v2f o;
				//o.vertex = mul(o.vertex, unity_ObjectToWorld);
				float4x4 myMVP = UNITY_MATRIX_P;
				myMVP = mul(myMVP, UNITY_MATRIX_V);
				myMVP = mul(myMVP, myM);
				o.vertex = mul(myMVP, v.vertex);
				
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
