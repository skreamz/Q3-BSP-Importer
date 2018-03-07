Shader "Custom/Q3Lightmap" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LightMap ("Lightmap (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _LightMap;

		struct Input {
			float2 uv_MainTex;
			float2 uv2_LightMap;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

			c.rbg *= DecodeLightmap(tex2D(_LightMap, IN.uv2_LightMap));

			o.Albedo = c.rgb;

			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
