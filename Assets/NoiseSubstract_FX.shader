// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/FX/Noise Substract"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Intensity ("Intensity", Range(0,1)) = 0.5
		_Brightness ("Brightness", Float) = 1
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			
			#include "UnityCG.cginc"

			float2 vert (float4 vertex : POSITION,
						float2 uv : TEXCOORD0,
						out float4 outpos : SV_POSITION) : TEXCOORD0
						{
				outpos = UnityObjectToClipPos(vertex);
				return uv;
			}
			
			sampler2D _MainTex;
			fixed _Intensity;
			float _Brightness;

			float noise(float3 co) {
				return frac(sin(dot(co.xyz, float3(12.9898,78.233,45.54332) )) * 43758.5453);
			}

			fixed4 frag (float2 uv : TEXCOORD0, UNITY_VPOS_TYPE sPos :VPOS) : SV_Target{
				fixed4 color = tex2D(_MainTex, uv);
				fixed noiseValue = noise(fixed3(sPos.xy, _Time.x));

				return color*_Brightness - noiseValue*_Intensity;
			}
			ENDCG
		}
	}
}
