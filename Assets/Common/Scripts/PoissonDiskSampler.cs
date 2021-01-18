using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class PoissonDiskSampler
{
    protected const int tries = 30;

    protected float width, height;
    protected readonly Rect rect;
    protected readonly float radius2;  // radius squared
    protected readonly float cellSize;
    protected PoissonDiskGrid2D[,] grid;

    public PoissonDiskSampler(PoissonDiskGrid2D[,] seed, float radius = 1f)
    {
        var iw = seed.GetLength(0);
        var ih = seed.GetLength(1);
        float invW = 1f / iw;
        float invH = 1f / ih;

        this.cellSize = Mathf.Min(invW, invH);
        var r = this.cellSize * radius;
        // Debug.Log(string.Format("{0}x{1}, {2}", iw, ih, radius));
        this.radius2 = r * r;
        this.width = 1f;
        this.height = 1f * ih / iw;
        this.grid = seed;
    }

    protected List<PoissonDiskIndex> GetValidIndices()
    {
        var indices = new List<PoissonDiskIndex>();

        var w = grid.GetLength(0);
        var h = grid.GetLength(1);

        // Debug.Log(string.Format("{0} x {1}", w, h));
        for (int ix = 0; ix < w; ix++)
        {
            for (int iy = 0; iy < h; iy++)
            {
                var g = grid[ix, iy];
                if (g.Valid)
                    indices.Add(new PoissonDiskIndex(ix, iy));
            }
        }

        return indices;
    }

    public Vector2 GetGridCenter(int ix, int iy)
    {
        int w = grid.GetLength(0), h = grid.GetLength(1);
        float invW = 1f / w, invH = 1f / h;
        return new Vector2(
            ix * invW + invW * 0.5f,
            iy * invH + invH * 0.5f
        );
    }

    protected bool FindHead(out PoissonDiskGrid2D found, out Vector2 point)
    {
        found = default(PoissonDiskGrid2D);
        point = default(Vector2);

        var w = grid.GetLength(0);
        var h = grid.GetLength(1);
        float invW = 1f / w;
        float invH = 1f / h;

        // Debug.Log(string.Format("{0} x {1}", w, h));
        for (int ix = 0; ix < w; ix++)
        {
            for (int iy = 0; iy < h; iy++)
            {
                var g = grid[ix, iy];
                if (g.Valid)
                {
                    found = g;
                    point = new Vector2(
                        ix * invW + invW * 0.5f, 
                        iy * invH + invH * 0.5f
                    );
                    return true;
                }
            }
        }

        return false;
    }

    public List<Vector2> Samples()
    {
        var activeSamples = new List<Vector2>();

        /*
        PoissonDiskGrid2D head;
        Vector2 point;
        if (!FindHead(out head, out point))
            return activeSamples;

        AddPoint(point);
        activeSamples.Add(point);
        */

        var indices = GetValidIndices();
        if (indices.Count <= 0)
            return activeSamples;

        var seeds = new List<Vector2>();
        int n = Mathf.Min(4, indices.Count);
        for (int i = 0; i < n; i++)
        {
            var idx = Random.Range(0, indices.Count);
            var di = indices[idx];
            var p = GetGridCenter(di.X, di.Y);
            seeds.Add(p);
        }

        foreach(var point in seeds)
        {
            AddPoint(point);
            activeSamples.Add(point);
        }

        while (activeSamples.Count > 0)
        {
            int i = Mathf.FloorToInt(Random.value * activeSamples.Count);
            var sample = activeSamples[i];

            bool found = false;
            for (int j = 0; j < tries; ++j)
            {
                var angle = 2 * Mathf.PI * Random.value;
                var r = Mathf.Sqrt(Random.value * 3 * radius2 + radius2);
                var candidate = sample + r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (
                    (0 < candidate.x && candidate.x < width && 0 < candidate.y && candidate.y < height) &&
                    IsValid(candidate)
                )
                {
                    found = true;
                    AddPoint(candidate);
                    activeSamples.Add(candidate);
                    break;
                }
            }

            if (!found)
            {
                activeSamples[i] = activeSamples[activeSamples.Count - 1];
                activeSamples.RemoveAt(activeSamples.Count - 1);
            }
        }

        return GetFilledPoints();
    }

    protected List<Vector2> GetFilledPoints()
    {
        var points = new List<Vector2>();
        var w = grid.GetLength(0);
        var h = grid.GetLength(1);
        for (int ix = 0; ix < w; ix++)
        {
            for (int iy = 0; iy < h; iy++)
            {
                var g = grid[ix, iy];
                if (g.Filled)
                    points.Add(g.Position);
            }
        }
        return points;
    }

    protected void AddPoint(Vector2 point)
    {
        int gx, gy;
        GetIndex(point, out gx, out gy);
        // Debug.Log(string.Format("Add point at ({0}, {1})", gx, gy));
        grid[gx, gy].Fill(point);
    }

    protected bool IsValid(Vector2 point)
    {
        int gx, gy;
        GetIndex(point, out gx, out gy);

        if (!grid[gx, gy].Valid)
        {
            return false;
        }

        // check neighbors
        int xmin = Mathf.Max(gx - 2, 0);
        int ymin = Mathf.Max(gy - 2, 0);
        int xmax = Mathf.Min(gx + 2, grid.GetLength(0) - 1);
        int ymax = Mathf.Min(gy + 2, grid.GetLength(1) - 1);

        for (int x = xmin; x <= xmax; x++)
        {
            for (int y = ymin; y <= ymax; y++)
            {
                var g = grid[x, y];
                if (g.Filled)
                {
                    Vector2 d = g.Position - point;
                    if (d.x * d.x + d.y * d.y < radius2) return false;
                }
            }
        }

        return true;
    }

    protected void GetIndex(Vector2 sample, out int x, out int y)
    {
        x = Mathf.FloorToInt(sample.x / cellSize);
        y = Mathf.FloorToInt(sample.y / cellSize);
    }

}


public class PoissonDiskIndex
{
    public int X { get { return x; } }
    public int Y { get { return y; } }
    protected int x, y;
    public PoissonDiskIndex(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class PoissonDiskGrid2D
{
    public bool Valid { get { return valid; } }
    public bool Filled { get { return filled; } }
    public Vector2 Position { get { return position; } }

    protected bool valid;
    protected bool filled = false;
    protected Vector2 position;

    public PoissonDiskGrid2D(bool valid = true)
    {
        this.valid = valid;
    }

    public void Fill(Vector2 position)
    {
        this.filled = true;
        this.position = position;
    }

}
