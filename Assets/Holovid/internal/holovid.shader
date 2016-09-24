Shader "Hypercube/Holovid"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		 _Displacement ("Extrusion Amount", Range(-10,10)) = 0.5
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
		ZWrite On

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Displacement;
			
			v2f vert (appdata_full v)
			{
				float2 depthCoord = v.texcoord.xy;
				depthCoord.x -= .5;
				float d = tex2Dlod(_MainTex, float4(depthCoord,0,0)).r * -_Displacement;
				v.vertex.xyz += v.normal * d;

				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
