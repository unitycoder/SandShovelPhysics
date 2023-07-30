// Standard shader with triplanar mapping
// https://github.com/keijiro/StandardTriplanar

Shader "Standard Triplanar Slots"
{
    Properties
    {
        _Color("", Color) = (1, 1, 1, 1)
        _MainTex("", 2D) = "white" {}

		_GlossinessTex("Glossiness (R)", 2D) = "white" {}
        _Glossiness("Glossiness Multiplier", Range(0, 1)) = 0.5
		_MetallicTex("Metallic (R)", 2D) = "white" {}
		[Gamma] _Metallic("Metallic Multiplier", Range(0, 1)) = 0

        _BumpScale("", Float) = 1
        _BumpMap("", 2D) = "bump" {}

        _OcclusionStrength("", Range(0, 1)) = 1
        _OcclusionMap("", 2D) = "white" {}

        _MapScale("", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        CGPROGRAM

        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow

        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _OCCLUSIONMAP
        #pragma shader_feature _GLOSSINESSMAP
        #pragma shader_feature _METALLICMAP

        #pragma target 3.0

        half4 _Color;
        sampler2D _MainTex;
        sampler2D _GlossinessTex;
        sampler2D _MetallicTex;

        half _Glossiness;
        half _Metallic;

        half _BumpScale;
        sampler2D _BumpMap;

        half _OcclusionStrength;
        sampler2D _OcclusionMap;

        half _MapScale;

        struct Input
        {
            float3 localCoord;
            float3 localNormal;
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.localCoord = v.vertex.xyz;
            data.localNormal = v.normal.xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            float3 bf = normalize(abs(IN.localNormal));
            bf /= dot(bf, (float3)1);

            // Triplanar mapping
            float2 tx = IN.localCoord.yz * _MapScale;
            float2 ty = IN.localCoord.zx * _MapScale;
            float2 tz = IN.localCoord.xy * _MapScale;

            // Base color
            half4 cx = tex2D(_MainTex, tx) * bf.x;
            half4 cy = tex2D(_MainTex, ty) * bf.y;
            half4 cz = tex2D(_MainTex, tz) * bf.z;
            half4 color = (cx + cy + cz) * _Color;

            o.Albedo = color.rgb;
            o.Alpha = color.a;

        #ifdef _NORMALMAP
            // Normal map
            half4 nx = tex2D(_BumpMap, tx) * bf.x;
            half4 ny = tex2D(_BumpMap, ty) * bf.y;
            half4 nz = tex2D(_BumpMap, tz) * bf.z;
            o.Normal = UnpackScaleNormal(nx + ny + nz, _BumpScale);
        #endif

        #ifdef _OCCLUSIONMAP
            // Occlusion map
            half ox = tex2D(_OcclusionMap, tx).g * bf.x;
            half oy = tex2D(_OcclusionMap, ty).g * bf.y;
            half oz = tex2D(_OcclusionMap, tz).g * bf.z;
            o.Occlusion = lerp((half4)1, ox + oy + oz, _OcclusionStrength);
        #endif

#ifdef _OCCLUSIONMAP
			// glossines
			half gx = tex2D(_GlossinessTex, tx).r * bf.x;
			half gy = tex2D(_GlossinessTex, ty).r * bf.y;
			half gz = tex2D(_GlossinessTex, tz).r * bf.z;
			half gloss = (gx + gy + gz);
			o.Smoothness = gloss* _Glossiness;
#endif

#ifdef _METALLICMAP
			// metallic
			half mx = tex2D(_MetallicTex, tx).r * bf.x;
			half my = tex2D(_MetallicTex, ty).r * bf.y;
			half mz = tex2D(_MetallicTex, tz).r * bf.z;
			half metal = (mx + my + mz);
			o.Metallic = metal* _Metallic;
		#endif
		}
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "StandardTriplanarInspector"
}
