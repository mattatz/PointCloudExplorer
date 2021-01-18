using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct Point
{
    public Vector3 position;
    public Color color;
    public float size;
    public float mass;
    public float lifetime;
    public uint scattering;

    public Point(Vector3 position, Color color, float size = 1f, float mass = 1f)
    {
        this.position = position;
        this.color = color;
        this.size = size;
        this.mass = mass;
        this.lifetime = 1f;
        this.scattering = 0;
    }
}
