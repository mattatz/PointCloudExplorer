using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;

[System.Serializable]
public class PointCloudGrid : System.IDisposable
{
    public ComputeBuffer PointBuffer { get { return pointBuffer; } }
    public ComputeBuffer OriginBuffer { get { return originBuffer; } }

    public Bounds WorldBounds { get { return worldBounds; } }
    public Bounds LocalBounds { get { return localBounds; } }
    public bool On { get { return on; } }

    [SerializeField] protected Bounds localBounds, worldBounds;
    [SerializeField] protected bool on;
    protected ComputeBuffer pointBuffer, originBuffer;
    protected List<Point> points;

    public PointCloudGrid(Bounds lbounds)
    {
        localBounds = lbounds;
        points = new List<Point>();
    }

    public bool Visible(Vector3 world, float threshold)
    {
        var closest = worldBounds.ClosestPoint(world);
        var dx = closest.x - world.x;
        var dz = closest.z - world.z;
        return on = Mathf.Sqrt(dx * dx + dz * dz) <= threshold;
        // return on = Vector3.Distance(worldBounds.center, p) <= threshold;
    }

    public void Force(bool on)
    {
        this.on = on;
    }

    public void Add(Point p)
    {
        points.Add(p);
    }

    public void Add(IEnumerable<Point> points)
    {
        this.points.AddRange(points);
    }

    public void Transform(Transform root)
    {
        var min = localBounds.min;
        var max = localBounds.max;
        var corners = new Vector3[] {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z),
        }.Select(p => root.TransformPoint(p));
        worldBounds = new Bounds(corners.First(), Vector3.zero);
        foreach (var p in corners) worldBounds.Encapsulate(p);
    }

    public void Build()
    {
        pointBuffer = new ComputeBuffer(points.Count, Marshal.SizeOf(typeof(Point)));
        pointBuffer.SetData(points.ToArray());

        originBuffer = new ComputeBuffer(points.Count, Marshal.SizeOf(typeof(Vector3)));
        originBuffer.SetData(points.Select(p => p.position).ToArray());
    }

    public bool Empty(int threshold = 0)
    {
        return points.Count <= threshold;
    }

    public void Dispose()
    {
        if (pointBuffer != null)
            pointBuffer.Dispose();
        if (originBuffer != null)
            originBuffer.Dispose();
    }

}
