using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Frame
{
    public Vector3 Tangent { get { return tangent; } }
    public Vector3 Normal { get { return normal; } }
    public Vector3 Bitangent { get { return bitangent; } }

    protected Vector3 tangent, normal, bitangent;

    public Frame(Vector3 tangent)
    {
        this.tangent = tangent.normalized;

        var dx = Vector3.right;
        if (Mathf.Abs(Vector3.Dot(dx, this.tangent)) >= 1f)
            dx = Vector3.up;

        var dy = Vector3.Cross(this.tangent, dx);
        dx = Vector3.Cross(this.tangent, dy);

        this.normal = dx;
        this.bitangent = dy;
    }

    public Frame(Vector3 tangent, Vector3 normal)
    {
        this.tangent = tangent.normalized;
        this.normal = normal.normalized;
        this.bitangent = Vector3.Cross(this.tangent, this.normal);
    }

    public Frame(Vector3 tangent, Vector3 normal, Vector3 bitangent)
    {
        this.tangent = tangent.normalized;
        this.normal = normal.normalized;
        this.bitangent = bitangent.normalized;
    }

}
