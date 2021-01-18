using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PolarCoordinate {

    [Range(-1.45f, 1.45f)] public float theta0;
    [Range(0f, 6.28f)] public float theta1;

    public PolarCoordinate(float t0, float t1)
    {
        theta0 = t0;
        theta1 = t1;
    }

    public void Move(float t0, float t1)
    {
        theta0 = t0;
        theta1 = t1;
    }

    public void Vertical(float dt)
    {
        theta0 += dt;
    }

    public void Horizontal(float dt)
    {
        theta1 += dt;
    }

    public Vector3 Cartesian(float radius, float offset = 0f)
    {
        var t0 = theta0 + offset;
        return new Vector3(
            -radius * Mathf.Cos(t0) * Mathf.Cos(theta1),
             radius * Mathf.Sin(t0),
             radius * Mathf.Cos(t0) * Mathf.Sin(theta1)
        );
    }

}
