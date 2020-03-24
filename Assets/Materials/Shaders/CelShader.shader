Shader "TD/CelShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (1, 0, 0, 1)
        _Glossiness("Glossiness", Float) = 32
        _RimAmount("Rim Amount", Range(0, 1)) = 0.7
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.3
        _AmbientColor("Ambient Color", Color) = (0.4, 0, 0, 1)
        _SpecularColor("Specular Color", Color) = (0, 1, 0, 1)
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "LightMode" = "ForwardBase"
            "PassFlags" = "OnlyDirectional"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Tint;
            float _Glossiness;
            float _RimAmount;
            float _RimThreshold;
            float4 _AmbientColor;
            float4 _SpecularColor;
            float4 _RimColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Color from texture
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Apply lighting based on single directional light
                float3 normal = normalize(i.normal);
                float dotl = dot(normal, _WorldSpaceLightPos0);
                
                // Intensity of light to apply
                float intensityL = smoothstep(0.f, 0.01f, clamp(ceil(dotl), 0.f, 1.f));

                // Final lighting to apply
                float finalLighting = _LightColor0 * intensityL;

                // Specular we add based on view from camera
                float3 viewDir = normalize(i.viewDir);

                // Half vector is addition of dir light and view dir (normalized)
                float3 halfVector = normalize(_WorldSpaceLightPos0 + viewDir);
                float dotv = dot(normal, halfVector);

                float intensityS = pow(dotv * intensityL, _Glossiness * _Glossiness);

                // Apply smoothing similar to what we did for lighting
                float specularIntensitySmooth = smoothstep(0.005, 0.01, intensityS);
                float4 finalSpecular = _SpecularColor * intensityS;

                // Effect to apply to the rim, we simply dot product against the camera,
                // we inverse result so directions facing away from us (the camera) are highlighted
                float4 dotr = 1.f - dot(viewDir, normal);

                // Apply smoothing similar to what we have done for lighting/specular             
                float intensityR = smoothstep(_RimAmount - 0.01f, _RimAmount + 0.01f, dotr * pow(dotl, _RimThreshold));
                float4 finalRim = intensityR * _RimColor;

                float4 blendColor = _AmbientColor + finalLighting + finalSpecular + finalRim;
                float4 finalCol = texColor * _Tint * blendColor;
                return finalCol;
            }
            ENDCG
        }
    }
}
