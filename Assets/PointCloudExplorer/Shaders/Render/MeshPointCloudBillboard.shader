Shader "PointCloudExplorer/MeshPointCloudRenderer"
{
  Properties
  {
      _Tint ("Tint", Color) = (0.5, 0.5, 0.5, 1)
      _PointSize ("Point Size", Float) = 0.05
      _LifetimeCurve ("Lifetime Curve", 2D) = "white" {}
  }

  SubShader
  {
    Cull Off
    ZWrite On

    CGINCLUDE

    #include "../Common/Point.hlsl"

    static const float SQ = 0.35355339059;
    static const float INVSQ = 1.0 / 0.35355339059;

    half _PointSize;
    float4x4 _Transform;
    sampler2D _LifetimeCurve;

    StructuredBuffer<Point> _PointBuffer;

    void Setup() {
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
#endif
    }

    ENDCG

    Pass
    {
      Tags {
        "RenderType" = "Opaque"
      }

      CGPROGRAM

      #pragma vertex Vertex
      #pragma fragment Fragment
      #pragma multi_compile_fog
      #pragma multi_compile_instancing
      #pragma instancing_options procedural:Setup

      #include "UnityCG.cginc"

      half4 _Tint;

      struct Attributes
      {
        float4 position: POSITION;
        float2 uv: TEXCOORD;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct Varyings
      {
        float4 position : SV_POSITION;
        float2 uv : TEXCOORD0;
        float intensity : TEXCOORD1;
        half3 color : COLOR;
        UNITY_FOG_COORDS(0)
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      UNITY_INSTANCING_BUFFER_START(Props)
      UNITY_INSTANCING_BUFFER_END(Props)

      Varyings Vertex(Attributes input)
      {
        Varyings o;

        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_TRANSFER_INSTANCE_ID(input, o);

        float4 pos = input.position;
        half3 col = (1.0).xxx;
        float size = 1.0;
        float intensity = 0.0;

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        uint id = unity_InstanceID;
        Point pt = _PointBuffer[id];
        pos = mul(_Transform, float4(pt.position, 1));
        col = pt.color;
        float lifetime = tex2Dlod(_LifetimeCurve, float4(pt.lifetime, 0.5, 0, 0)).x;
        size = pt.size * lifetime;
        intensity = pt.color.a;
#endif

        float2 uv = input.uv;
        float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize) * size * uv;
        o.position = UnityWorldToClipPos(pos);
        o.position.xy += extent;
        o.uv = uv;
        o.intensity = intensity;

#if UNITY_COLORSPACE_GAMMA
        col *= _Tint.rgb;
#else
        col *= LinearToGammaSpace(_Tint.rgb);
        col = GammaToLinearSpace(col);
#endif
        o.color = col.rgb;
        UNITY_TRANSFER_FOG(o, o.position);
        return o;
      }

      half4 Fragment(Varyings input) : SV_Target
      {
        clip(SQ - length(input.uv));

        UNITY_SETUP_INSTANCE_ID(input);
        half4 c = half4(input.color, _Tint.a) * (1.0 + input.intensity);
        UNITY_APPLY_FOG(input.fogCoord, c);
        return c;
      }

      ENDCG
    }

    Pass {
      Name "CastShadow"
      Tags { 
        "LightMode" = "ShadowCaster"
      }

      CGPROGRAM
      #pragma vertex VertShadow
      #pragma fragment FragShadow
      #pragma multi_compile_shadowcaster
      #pragma multi_compile_instancing
      #pragma instancing_options procedural:Setup
      #include "UnityCG.cginc"

      struct Attributes
      {
        float4 position: POSITION;
        float2 uv: TEXCOORD;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct VaryingsShadow
      {
        float4 position: SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 hpos: TEXCOORD1;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      VaryingsShadow VertShadow(Attributes v)
      {
        VaryingsShadow o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        float4 proj = float4(0, 0, 0, 1);
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        uint id = unity_InstanceID;
        Point pt = _PointBuffer[id];
        float3 pos = mul(_Transform, float4(pt.position, 1));
        float lifetime = tex2Dlod(_LifetimeCurve, float4(pt.lifetime, 0.5, 0, 0)).x;
        float size = pt.size * lifetime;
        float2 extent = abs(UNITY_MATRIX_P._11_22 * _PointSize) * size * v.uv.xy;
        proj = UnityWorldToClipPos(pos);
        proj.xy += extent;
#endif
        o.position = proj;
        o.uv.xy = v.uv.xy;
        proj.z += saturate(unity_LightShadowBias.x / proj.w);
        float clamped = max(proj.z, 0);
        proj.z = lerp(proj.z, clamped, unity_LightShadowBias.y);
        o.hpos = proj;

        return o;
      }

      float4 FragShadow(VaryingsShadow i) : COLOR
      {
        clip(SQ - length(i.uv));
        SHADOW_CASTER_FRAGMENT(i)
      }
      ENDCG
    }

  }
}

