Shader "Hypercube/Holovid"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		 _Displacement ("Extrusion Amount", Range(-10,10)) = 0.5
		 _ForcedPerspective("Forced Perspective", Range(-1,20)) = 0
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
			float _ForcedPerspective;
			
			v2f vert (appdata_full v)
			{
				float2 depthCoord = v.texcoord.xy;
				depthCoord.x -= .5; //shove the UV to the left side of the texture, which holds the respective depth.
				float d = tex2Dlod(_MainTex, float4(depthCoord,0,0)).r * -_Displacement;
				v.vertex.xyz += v.normal * d;

				//perspective modifier to vert position
				float diffX = (.25 - depthCoord.x) ;  //.25 is the center of the lefthand image (the depth).  
				float diffY = .5 - depthCoord.y;
				v.vertex.x += diffX * _ForcedPerspective * d * 2; //the 2 compensates for the diff being only half of the relevant distance because the texture really holds 2 separate images
				v.vertex.y += diffY * _ForcedPerspective * d;

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
