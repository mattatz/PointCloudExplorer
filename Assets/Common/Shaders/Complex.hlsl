#ifndef __COMPLEX_INCLUDED__
#define __COMPLEX_INCLUDED__

float2 c_add(float2 c1, float2 c2)
{
  float a = c1.x;
  float b = c1.y;
  float c = c2.x;
  float d = c2.y;
  return float2(a + c, b + d);
}

float2 c_sub(float2 c1, float2 c2)
{
  float a = c1.x;
  float b = c1.y;
  float c = c2.x;
  float d = c2.y;
  return float2(a - c, b - d);
}

float2 c_mul(float2 c1, float2 c2)
{
  float a = c1.x;
  float b = c1.y;
  float c = c2.x;
  float d = c2.y;
  return float2(a*c - b * d, b*c + a * d);
}

float2 c_div(float2 c1, float2 c2)
{
  float a = c1.x;
  float b = c1.y;
  float c = c2.x;
  float d = c2.y;
  float real = (a*c + b * d) / (c*c + d * d);
  float imag = (b*c - a * d) / (c*c + d * d);
  return float2(real, imag);
}

float c_abs(float2 c)
{
  return sqrt(c.x*c.x + c.y*c.y);
}

float2 c_pol(float2 c)
{
  float a = c.x;
  float b = c.y;
  float z = c_abs(c);
  float f = atan2(b, a);
  return float2(z, f);
}

float2 c_rec(float2 c)
{
  float z = abs(c.x);
  float f = c.y;
  float a = z * cos(f);
  float b = z * sin(f);
  return float2(a, b);
}

float2 c_pow(float2 base, int n)
{
  float2 r = base;
  for (int iter = 0; iter < n; iter++) {
    r = c_mul(r, base);
  }
  return r;
}

#endif
