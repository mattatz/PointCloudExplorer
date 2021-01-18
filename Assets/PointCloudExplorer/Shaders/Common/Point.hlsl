#ifndef __POINT_INCLUDED__
#define __POINT_INCLUDED__

struct Point {
  float3 position;
  // float3 origin;
  float4 color;
  float size;
  float mass;
  float lifetime;
  bool scattering;
};

#endif 
