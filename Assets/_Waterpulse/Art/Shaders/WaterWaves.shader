// Superficie del agua con onadas suaves, solo visuales (la física sigue plana en y = 0).
// Tres ondas que se cruzan desplazan los vértices en altura; la normal sale de su pendiente para que la luz y un
// brillo especular las dejen ver. Transparente para que se vea el fondo de la piscina. Lo usa el material "Water".
// Además, la onada del Mur de Ferro: un anillo de agua que crece desde el muro y se apaga (variables globales
// _ShockA y _ShockB, que pone WaterWall con Shader.SetGlobalVector)
Shader "Waterpulse/WaterWaves"
{
    Properties
    {
        _TroughColor ("Color en las valles", Color) = (0.05, 0.38, 0.82, 0.72)
        _CrestColor ("Color en las crestas", Color) = (0.45, 0.78, 1.0, 0.78)
        _Amplitude ("Altura de las olas (m)", Float) = 0.035
        _Wavelength ("Longitud de onda (m)", Float) = 2.6
        _Speed ("Velocidad", Float) = 0.7
        _Sparkle ("Brillo del sol", Range(0, 2)) = 0.6
        _Shininess ("Tamaño del brillo (alto = más pequeño)", Float) = 90
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TroughColor;
                half4 _CrestColor;
                float _Amplitude;
                float _Wavelength;
                float _Speed;
                half _Sparkle;
                half _Shininess;
            CBUFFER_END

            // Onada del Mur de Ferro (globales): A = (centro x, centro z, inicio en _Time.y, duración),
            // B = (velocidad m/s, altura m, anchura m, espuma 0-1)
            float4 _ShockA;
            float4 _ShockB;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float crest : TEXCOORD2;   // -1 valle, 1 cresta
                float foam : TEXCOORD3;    // 0-1, espuma en la cresta de la onada
            };

            // Una onda senoidal: devuelve la altura (-1 a 1) y suma su pendiente a "slope"
            float Wave(float2 p, float2 direction, float k, float omega, float weight, inout float2 slope)
            {
                float phase = dot(direction, p) * k + _Time.y * omega;
                slope += direction * (k * cos(phase) * weight);
                return sin(phase) * weight;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float k = 6.2831853 / max(_Wavelength, 0.01);
                float omega = k * _Speed;
                float2 slope = 0;
                float h = 0;
                h += Wave(positionWS.xz, normalize(float2(1.0, 0.35)), k, omega, 1.0, slope);
                h += Wave(positionWS.xz, normalize(float2(-0.45, 1.0)), k * 1.7, omega * 1.3, 0.5, slope);
                h += Wave(positionWS.xz, normalize(float2(0.7, -0.6)), k * 2.9, omega * 1.9, 0.25, slope);

                positionWS.y += h * _Amplitude;
                slope *= _Amplitude;

                // Onada del muro: una cresta gaussiana en el radio que avanza, con un valle detrás
                float foam = 0;
                float age = _Time.y - _ShockA.z;
                if (_ShockA.w > 0 && age >= 0 && age < _ShockA.w)
                {
                    float2 fromCenter = positionWS.xz - _ShockA.xy;
                    float d = length(fromCenter);
                    float2 dir = d > 0.001 ? fromCenter / d : float2(0, 0);
                    float radius = age * _ShockB.x;
                    float fade = 1.0 - age / _ShockA.w;
                    fade *= fade;
                    float width = max(_ShockB.z, 0.05);
                    float x = (d - radius) / width;
                    float crest = exp(-x * x);
                    float trough = exp(-(x + 1.6) * (x + 1.6)) * 0.45;   // el agua baja justo detrás de la cresta
                    float shock = (crest - trough) * _ShockB.y * fade;
                    float dShock = (-2.0 * x * crest + 2.0 * (x + 1.6) * trough) / width * _ShockB.y * fade;
                    positionWS.y += shock;
                    slope += dir * dShock;
                    foam = saturate(crest * fade * _ShockB.w * 1.5);
                }
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = normalize(float3(-slope.x, 1.0, -slope.y));
                output.crest = h / 1.75;
                output.foam = foam;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                Light light = GetMainLight();
                half diffuse = saturate(dot(n, light.direction));
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half specular = pow(saturate(dot(reflect(-light.direction, n), viewDir)), _Shininess) * _Sparkle;

                half4 color = lerp(_TroughColor, _CrestColor, saturate(input.crest * 0.5 + 0.5));
                half3 rgb = color.rgb * (0.7 + 0.3 * diffuse) + specular * light.color;
                rgb = lerp(rgb, half3(0.95, 0.98, 1.0), input.foam);   // espuma blanca en la cresta de la onada
                return half4(rgb, saturate(color.a + specular + input.foam));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
