Shader "PointCloudExplorer/Wall"
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
    Tags { "RenderType"="Transparent" "Queue"="Transparent" }
    LOD 100

    Pass
    {
      Cull Off
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex: POSITION;
        float3 normal: NORMAL;
      };

      struct v2f
      {
        float4 vertex: SV_POSITION;
        float3 world: TANGENT;
        float2 uv: TEXCOORD0;
      };

      half3 _Position;
      half _Radius, _Power;
      fixed3 _Up, _Right;

      sampler2D _MainTex;
      float4 _MainTex_ST;

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.world = world;
        o.uv = float2(dot(world, _Right), dot(world, _Up));
        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        fixed alpha = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw).x;
        fixed4 col = fixed4(1, 1, 1, alpha);
        float d = distance(i.world, _Position) / _Radius;
        col.a = 0.75 * alpha * saturate(1.0 - pow(d, _Power));
        return col;
      }

      ENDCG
    }
  }
}
