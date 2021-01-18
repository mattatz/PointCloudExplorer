Shader "PointCloudExplorer/Frustum"
{
  Properties
  {
    [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
    _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
  }

  SubShader
  {
    LOD 100

    Pass
    {
      Tags { 
        "RenderType" = "Transparent"
        "Queue" = "Transparent+1"
      }

      Blend SrcAlpha OneMinusSrcAlpha

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

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }

      fixed4 _Color;
      fixed _Alpha;

      fixed4 frag(v2f i) : SV_Target
      {
        return _Color * _Alpha;
      }

      ENDCG
    }

    Pass {
      Name "CastShadow"
      Tags { "LightMode" = "ShadowCaster" }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_shadowcaster
      #include "UnityCG.cginc"

      struct v2f
      {
        V2F_SHADOW_CASTER;
      };

      v2f vert(appdata_base v)
      {
        v2f o;
        TRANSFER_SHADOW_CASTER(o)
        return o;
      }

      float4 frag(v2f i) : COLOR
      {
        SHADOW_CASTER_FRAGMENT(i)
      }
      ENDCG
    }
  }
}
