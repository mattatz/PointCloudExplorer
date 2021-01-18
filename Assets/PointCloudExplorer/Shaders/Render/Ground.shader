Shader "PointCloudExplorer/Ground"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "" {}
    _Position ("Position", Vector) = (0, 0, 0, -1)
    _Radius ("Radius", Float) = 10.0
    _Power ("Power", Float) = 2.0
  }

  SubShader
  {
    LOD 100

    Pass
    {
      Tags { "RenderType"="Transparent" "Queue"="Transparent" }
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      // Blend Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
      };

      struct v2f
      {
        float4 vertex : SV_POSITION;
        float3 world : NORMAL;
        float2 uv : TEXCOORD0;
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
        // o.world = floor(o.world);
        o.uv.xy = o.world.xz * _MainTex_ST.xy + _MainTex_ST.zw;
        return o;
      }

      float3 _Position;
      float _Radius, _Power;

      fixed4 frag (v2f i) : SV_Target
      {
        // float2 uv = i.world.xz * _MainTex_ST.xy + _MainTex_ST.zw;
        fixed4 col = tex2D(_MainTex, i.uv);
        float d = distance(i.world.xz, _Position.xz) / _Radius;
        col.a *= col.x * saturate(1.0 - pow(d, _Power));
        return col;
      }
      ENDCG
    }

    Pass {
      Name "CastShadow"
      Tags { 
        "LightMode" = "ShadowCaster" 
      }
      Blend Off

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
