Shader "PointCloudExplorer/PointGridBounds"
{
  Properties
  {
    [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
    _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
  }

  SubShader
  {
    // Tags { "RenderType" = "Opaque" }
    Tags { 
      "RenderType" = "Opaque"
      "Queue" = "Geometry"
    }

    LOD 100

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
  }
}
