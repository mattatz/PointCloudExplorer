using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PointCloudData : ScriptableObject
{
    public Point[] Points { get { return points; } }
    public Bounds LocalBounds { get { return localBounds; } }

    [SerializeField] protected Point[] points;
    [SerializeField] protected Bounds localBounds;

    public static PointCloudData Build(List<Mesh> meshes, string name)
    {
        var data = Build(meshes);
#if UNITY_EDITOR
        var path = AssetDatabase.GenerateUniqueAssetPath(string.Format("Assets/{0}.asset", name));
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
        return data;
    }

    public static PointCloudData Build(List<Mesh> meshes)
    {
        var data = ScriptableObject.CreateInstance<PointCloudData>();
        data.Setup(meshes);
        return data;
    }

    public void Setup(List<Mesh> meshes)
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        meshes.ForEach(m =>
        {
            vertices.AddRange(m.vertices);
            colors.AddRange(m.colors);
        });

        localBounds = new Bounds();
        if (meshes.Count > 0)
        {
            localBounds = meshes.First().bounds;
            for (int i = 1; i < meshes.Count; i++)
            {
                var bb = meshes[i].bounds;
                localBounds.Encapsulate(bb);
            }
        }

        var min = localBounds.min;
        var max = localBounds.max;
        var size = localBounds.size;
        var hsize = size * 0.5f;

        int n = vertices.Count();
        points = new Point[n];
        for (int i = 0; i < n; i++)
            points[i] = new Point(vertices[i], colors[i], 0f, Random.value);
    }

}
