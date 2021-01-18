Shader "PointCloudExplorer/CharacterParticle"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
    [HDR] _HDR ("HDR", Color) = (1, 1, 1, 1)
  }

  SubShader
  {
    Tags { 
      "RenderType" = "Transparent" 
      "Queue" = "Transparent+2"
    }
    LOD 100

    Pass
    {
      Cull Back
      Blend SrcAlpha One

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

      sampler2D _MainTex;
      half4 _HDR;

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }

      half4 frag(v2f i) : SV_Target
      {
        half4 col = tex2D(_MainTex, i.uv) * _HDR;
        return col * col.a;
      }

      ENDCG
    }
  }
}
