Shader "Custom/GTAMissionMarker"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _FadeHeight ("Fade Height", Range(0,1)) = 0.7
        _MinAlpha ("Minimum Alpha", Range(0,1)) = 0.0
        _MaxAlpha ("Maximum Alpha", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off  // Changed from "Cull Back" to "Cull Off"

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
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 objectPos : TEXCOORD2;
            };

            fixed4 _Color;
            float _FadeHeight;
            float _MinAlpha;
            float _MaxAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objectPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate height factor (0 at bottom, 1 at top)
                float heightFactor = saturate(i.objectPos.y + 0.5); // Clamp to 0-1 range

                // Apply fade curve
                float alpha = lerp(_MaxAlpha, _MinAlpha, pow(heightFactor, _FadeHeight));

                fixed4 col = _Color;
                col.a *= alpha;

                // Ensure we don't exceed normal color range
                col.rgb = saturate(col.rgb);

                return col;
            }
            ENDCG
        }
    }
}