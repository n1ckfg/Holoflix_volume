// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Hypercube/HolovidParticle"
{
	Properties 
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Displacement ("Extrusion Amount", Range(-10,10)) = 0.5
		_ParticleTex ("Particle Texture", 2D) = "white" {}
		_ParticleSize ("Particle Size", Range(0.001, 0.25)) = 0.025
		[MaterialEnum(Single,0,Quad,1)] _ParticleUV ("Particle UV", Int) = 1
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		[Toggle(ENABLE_SOFTSLICING)] _softSlicingToggle ("Soft Sliced", Float) = 1
		[HideInInspector]_Dims ("UV Projection Scale", Vector) = (1,1,1,1)
	}

	SubShader 
	{
		Pass
		{
			Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
			Alphatest Greater [_Cutoff]
			AlphaToMask True
			LOD 200
			Blend SrcAlpha OneMinusSrcAlpha
		
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex VS_Main
			#pragma fragment FS_Main
			#pragma geometry GS_Main
			#include "UnityCG.cginc"
			
			#pragma shader_feature ENABLE_SOFTSLICING
			#pragma multi_compile __ SOFT_SLICING 
			
			struct GS_INPUT
			{
				float4	vertex		: POSITION;
				float3	up			: TEXCOORD0;
				float3	right		: TEXCOORD1;
				float2  uv0			: TEXCOORD2;
				float2  uv1			: TEXCOORD3;
				float	projPosZ	: TEXCOORD4; //Screen Z position
			};

			struct FS_INPUT
			{
				float4	vertex		: POSITION;
				float2  uv0			: TEXCOORD0;
				float2  uv1			: TEXCOORD1;
				float	projPosZ	: TEXCOORD2;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Displacement;
			
			float _ParticleSize;
			int _ParticleUV;
			float4x4 _VP;
			Texture2D _ParticleTex;
			SamplerState sampler_ParticleTex;
			float _softPercent;
			half4 _blackPoint;
			
			float4 _Dims;
			
			uniform fixed _Cutoff;

			GS_INPUT VS_Main(appdata_base v)
			{
				GS_INPUT o = (GS_INPUT)0;

				float2 depthCoord = v.texcoord.xy;
				depthCoord.x -= .5;
				float d = tex2Dlod(_MainTex, float4(depthCoord,0,0)).r * -_Displacement;
				v.vertex.xyz += v.normal * d;
				
				o.vertex =  mul(unity_ObjectToWorld, v.vertex);
				
				float halfS = _ParticleSize;
				o.up = mul(unity_ObjectToWorld, UNITY_MATRIX_IT_MV[0].xyz) * halfS;
				o.right = mul(unity_ObjectToWorld, UNITY_MATRIX_IT_MV[1].xyz) * halfS;
				
				o.uv0 = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
				o.uv1 = float2(0, 0);
				
				o.projPosZ = mul(UNITY_MATRIX_MVP, v.vertex).z;

				return o;
			}
			
			[maxvertexcount(4)]
			void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
			{		
				float4 v[4];
				v[0] = float4(p[0].vertex + p[0].right - p[0].up, 1.0f);
				v[1] = float4(p[0].vertex + p[0].right + p[0].up, 1.0f);
				v[2] = float4(p[0].vertex - p[0].right - p[0].up, 1.0f);
				v[3] = float4(p[0].vertex - p[0].right + p[0].up, 1.0f);
				
				//Single style uvs
				float2 scaleUV = float2(0, 0);
				
				if (_ParticleUV > 0)
				{
					//Quad style uvs
					scaleUV = float2(unity_WorldToObject[0][0], unity_WorldToObject[1][1]) * _ParticleSize * _Dims.xy;
				}
				
				float4x4 vp = mul(UNITY_MATRIX_MVP, unity_WorldToObject);
				FS_INPUT pIn;
				pIn.vertex = mul(vp, v[0]);
				pIn.uv0 = p[0].uv0 + scaleUV * float2(1, 1);
				pIn.uv1 = float2(0, 1);
				pIn.projPosZ = p[0].projPosZ;
				triStream.Append(pIn);

				pIn.vertex =  mul(vp, v[1]);
				pIn.uv0 = p[0].uv0 + scaleUV * float2(-1, 1);
				pIn.uv1 = float2(1, 1);
				pIn.projPosZ = p[0].projPosZ;
				triStream.Append(pIn);

				pIn.vertex =  mul(vp, v[2]);
				pIn.uv0 = p[0].uv0 + scaleUV * float2(1, -1);
				pIn.uv1 = float2(0, 0);
				pIn.projPosZ = p[0].projPosZ;
				triStream.Append(pIn);

				pIn.vertex =  mul(vp, v[3]);
				pIn.uv0 = p[0].uv0 + scaleUV * float2(-1, -1);
				pIn.uv1 = float2(1, 0);
				pIn.projPosZ = p[0].projPosZ;
				triStream.Append(pIn);
			}
			
			float4 FS_Main(FS_INPUT i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.uv0) * _ParticleTex.Sample(sampler_ParticleTex, i.uv1);
				clip( col.a - _Cutoff );
				col.xyz += _blackPoint.xyz;
			
				#if defined(SOFT_SLICING) && defined(ENABLE_SOFTSLICING)
					float d = i.projPosZ;
					//return d; //uncomment this to show the raw depth

					//note: if _softPercent == 0  that is the same as hard slice.

					float mask = 1;	
										
					if (d < _softPercent)
						mask *= d / _softPercent; //this is the darkening of the slice near 0 (near)
					else if (d > 1 - _softPercent)
						mask *= 1 - ((d - (1-_softPercent))/_softPercent); //this is the darkening of the slice near 1 (far)
					
					//return mask;
					return col * mask;  //multiply mask after everything because _blackPoint must be included in there or we will get 'hardness' from non-black blackpoints		
				#endif
				return col;
			}
			ENDCG
		}
	}
	Fallback "Legacy Shaders/Transparent/Cutout/VertexLit"
}
