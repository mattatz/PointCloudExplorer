Shader "PointCloudExplorer/Path"
{
  Properties
  {
    [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
    _T ("T", Range(0.0, 1.0)) = 0.0
    _Length ("Length", Range(0.0, 1.0)) = 0.1
  }

  SubShader
  {
    LOD 100

    CGINCLUDE

    fixed _T, _Length;

    float GetAlpha(float uv) {
      float u = uv.x;
      float alpha = saturate(1.0 - (abs(_T - u) / _Length));
      return alpha * (u <= _T);
    }

    ENDCG

    Pass
    {
      Tags { 
        "RenderType" = "Opaque" 
        // "RenderType" = "Transparent" 
        // "Queue" = "Transparent+2"
      }

      // Blend SrcAlpha One

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
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
      };

      half4 _Color;

      v2f vert(appdata v)
      {
        v2f o;
        float3 position = v.vertex.xyz;
        o.vertex = UnityObjectToClipPos(float4(position, 1));
        o.uv = v.uv;
        return o;
      }

      half4 frag(v2f i) : SV_Target
      {
        float alpha = GetAlpha(i.uv);
        clip(alpha - 1e-5);
        return _Color * alpha;
      }

      ENDCG
    }

    Pass {
      Name "CastShadow"
      Tags { 
        "LightMode" = "ShadowCaster" 
      }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_shadowcaster
      #include "UnityCG.cginc"

      struct v2f
      {
        V2F_SHADOW_CASTER;
        float2 uv : TEXCOORD1;
      };

      v2f vert(appdata_base v)
      {
        v2f o;
        TRANSFER_SHADOW_CASTER(o)
        o.uv = v.texcoord.xy;
        return o;
      }

      float4 frag(v2f i) : COLOR
      {
        float alpha = GetAlpha(i.uv);
        clip(alpha - 1e-5);
        SHADOW_CASTER_FRAGMENT(i)
      }
      ENDCG
    }

  }
}
