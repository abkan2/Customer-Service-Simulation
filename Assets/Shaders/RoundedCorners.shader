Shader "UI/RoundedCorners/RoundedCorners"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)
        // X = rect width, Y = rect height, Z = corner radius*2, W = unused
        _WidthHeightRadius ("WidthHeightRadius", Vector) = (100,100,40,0)
        // Outer UV of the sprite to respect trimmed sprites
        _OuterUV ("OuterUV", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4     _MainTex_ST;
            fixed4     _Color;
            float4     _WidthHeightRadius; // x=width, y=height, z=radius*2
            float4     _OuterUV;           // xy = uv.min, zw = uv.max

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // sample the sprite
                fixed4 texcol = tex2D(_MainTex, IN.uv) * IN.color;

                // remap UV to 0..width, 0..height
                float2 pixPos;
                pixPos.x = lerp(0, _WidthHeightRadius.x, (IN.uv.x - _OuterUV.x) / (_OuterUV.z - _OuterUV.x));
                pixPos.y = lerp(0, _WidthHeightRadius.y, (IN.uv.y - _OuterUV.y) / (_OuterUV.w - _OuterUV.y));

                float w = _WidthHeightRadius.x;
                float h = _WidthHeightRadius.y;
                float r = _WidthHeightRadius.z * 0.5; // actual radius

                // distance to nearest rectangle edge
                float2 dist = float2(
                    min(pixPos.x, w - pixPos.x),
                    min(pixPos.y, h - pixPos.y)
                );

                // if both dist components are < r then inside corner circle area
                float cornerDist = length(max(float2(r, r) - dist, 0.0));

                // alpha smoothstep to anti-alias
                float alpha = smoothstep(r, r - 1.0, cornerDist);

                texcol.a *= alpha;

                return texcol;
            }
            ENDCG
        }
    }
}
