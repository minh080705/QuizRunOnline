Shader "Unlit/UnlitBlinking"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BlinkingValue ("Blinking Value", Range(0,1)) = 0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
		LOD 100

		Pass
		{
			Name "ForwardUnlit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			// ==== Biến bẻ cong toàn cục ====
			// _WorldCurveStrength: do WorldCurvature.cs set mỗi frame (mặc định coi như 0 nếu quên gắn script,
			//   nhưng để AN TOÀN tuyệt đối, luôn nhớ gắn script này vào scene).
			// _WorldSpaceCameraPos: biến CÓ SẴN của Unity, luôn tự động đúng, không cần script nào set.
			float _WorldCurveStrength;
			float _WorldCurveUseZ;

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				float _BlinkingValue;
			CBUFFER_END

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv         : TEXCOORD0;
				float  fogCoord   : TEXCOORD1;
				float4 vertex     : SV_POSITION;
			};

			// ==== Hàm bẻ cong, giống các shader trước, giữ đồng bộ toàn game ====
			float3 ApplyWorldCurve(float3 positionWS)
			{
				float dist;
				if (_WorldCurveUseZ > 0.5)
				{
					dist = positionWS.z - _WorldSpaceCameraPos.z;
				}
				else
				{
					float2 d = positionWS.xz - _WorldSpaceCameraPos.xz;
					dist = length(d);
				}

				dist = max(dist, 0.0);
				float drop = _WorldCurveStrength * dist * dist;

				positionWS.y -= drop;
				return positionWS;
			}

			v2f vert (appdata v)
			{
				v2f o;

				// Object -> World (thay cho UnityObjectToClipPos cũ)
				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);

				// ==== DÒNG MỚI DUY NHẤT LIÊN QUAN GAMEPLAY: bẻ cong theo khoảng cách ====
				positionWS = ApplyWorldCurve(positionWS);

				// World -> Clip Space
				o.vertex = TransformWorldToHClip(positionWS);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				// Fog kiểu URP (thay cho UNITY_TRANSFER_FOG cũ)
				o.fogCoord = ComputeFogFactor(o.vertex.z);

				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				// sample the texture — giữ nguyên như code gốc
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

				// Hiệu ứng nháy màu — giữ y nguyên logic gốc
				col = saturate(lerp(col, half4(1, 1, 0.75, 1), _BlinkingValue));

				// apply fog — bản URP tương đương UNITY_APPLY_FOG cũ
				col.rgb = MixFog(col.rgb, i.fogCoord);

				return col;
			}
			ENDHLSL
		}
	}
}
