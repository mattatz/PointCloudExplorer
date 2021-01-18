using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldWall : MonoBehaviour, IPointCloudSetup, IPlayerResponsible, IGridTextureUser
{
    public Texture2D Texture { get; set; }

    public Character Player { get; set; }

    [SerializeField] protected Wall prefab;
    [SerializeField] protected List<Wall> walls;
    [SerializeField] protected Bounds bounds;
    [SerializeField] protected float padding = 300f;

    protected void Update()
    {
        if (Player == null) return;
        foreach (var wall in walls)
        {
            wall.Visualize(Player.transform.position);
        }
    }

    public void OnSetup(PointCloudController pcx)
    {
        bounds = pcx.WorldBounds;

        var up = Vector3.up;
        var ul = Mathf.Max(1e2f * 5f, Mathf.Abs(Vector3.Dot(bounds.size, up)));

        var directions = new Vector3[]
        {
            Vector3.left, Vector3.forward, Vector3.right, Vector3.back
        };

        foreach (var dir in directions)
        {
            var wall = Instantiate(prefab);
            wall.transform.SetParent(transform);

            var r = Vector3.Cross(dir, up).normalized;
            var dl = padding + Mathf.Abs(Vector3.Dot(dir, bounds.size));
            var rl = padding + Mathf.Abs(Vector3.Dot(r, bounds.size));

            var size = up * ul + r * rl;
            var center = Vector3.ProjectOnPlane(bounds.center, up) + dir * dl * 0.5f + up * ul * 0.5f;

            wall.Texture = Texture;
            wall.transform.position = center;
            wall.transform.localScale = new Vector3(rl, ul, 1f);
            wall.transform.rotation = Quaternion.LookRotation(dir, up);

            walls.Add(wall);
        }

        // build top
        {
            var wall = Instantiate(prefab);
            wall.transform.SetParent(transform);

            Vector3 f = Vector3.forward, r = Vector3.right;
            var dl = padding + Mathf.Abs(Vector3.Dot(f, bounds.size));
            var rl = padding + Mathf.Abs(Vector3.Dot(r, bounds.size));

            var size = f * dl + r * rl;
            var center = Vector3.ProjectOnPlane(bounds.center, up) + up * ul;

            wall.Texture = Texture;
            wall.transform.position = center;
            wall.transform.localScale = new Vector3(Mathf.Abs(rl), Mathf.Abs(dl), 1f);
            wall.transform.rotation = Quaternion.LookRotation(up);

            walls.Add(wall);
        }

    }

    protected Mesh Build(Bounds bounds)
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        var min = bounds.min;
        var max = bounds.max;

        var points = new Vector3[] {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z)
        };
        var indices = new int[]
        {
            /*
            // bottom
            0, 3, 1,
            2, 1, 3,

            // up
            4, 5, 7,
            6, 7, 5,
            */

            // left
            3, 0, 7,
            4, 7, 0,

            // right
            1, 2, 5,
            6, 5, 2,

            // forward
            2, 3, 6,
            7, 6, 3,

            // back
            0, 1, 4,
            5, 4, 1
        };

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0, n = indices.Length; i < n; i += 3)
        {
            var a = points[indices[i]];
            var b = points[indices[i + 1]];
            var c = points[indices[i + 2]];
            var ia = vertices.Count;
            var ib = ia + 1;
            var ic = ia + 2;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            triangles.Add(ia); triangles.Add(ib); triangles.Add(ic);
        }

        mesh.SetVertices(vertices);
        mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    protected void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

}
