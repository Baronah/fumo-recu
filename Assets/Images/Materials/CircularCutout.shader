Shader "Custom/CircularCutoutFixedAspect" {
    Properties {
        _Center ("Circle Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 0.37
        _EdgeSoftness ("Edge Softness", Float) = 0.25
        _Color ("Background Color", Color) = (0, 0, 0, 1)
        _ScreenAspect ("Screen Aspect Ratio", Float) = 1.0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Center;
            float _Radius;
            float _EdgeSoftness;
            float _ScreenAspect;
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv;

                // Adjust UVs for aspect ratio
                uv.x *= _ScreenAspect;

                // Calculate the distance from the center
                float dist = distance(uv, float2(_Center.x * _ScreenAspect, _Center.y));

                // Smooth transition at the edge
                float alpha = smoothstep(_Radius, _Radius - _EdgeSoftness, dist);

                // Correct blending order
                return lerp(_Color, fixed4(0, 0, 0, 0), alpha); // Smooth transition
            }
            ENDCG
        }
    }
}
