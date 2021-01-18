using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PointCloudDataWindow : EditorWindow
{
    protected string path = "Assets/MeshPointCloud.asset";
    protected int sampleCount = 1024;
    protected Object mesh;
    protected Texture2D texture;

    [MenuItem("Window/PointCloudData")]
    public static void Init()
    {
        var window = EditorWindow.GetWindow(typeof(PointCloudDataWindow)) as PointCloudDataWindow;
        window.Show();
        window.Focus();
    }

    protected void OnGUI()
    {
        const float headerSize = 120f;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Source mesh file", GUILayout.Width(headerSize));
            mesh = EditorGUILayout.ObjectField(mesh, typeof(Mesh), true);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Output path", GUILayout.Width(headerSize));
            path = EditorGUILayout.TextField(path);
        }

        GUI.enabled = (mesh != null);
        if (GUILayout.Button("Build"))
        {
            //Build(mesh as Mesh, texture, path, sampleCount);
        }
    }

    /*
    protected void Build(
        Mesh mesh, Texture2D texture,
        string path,
        int samples
    )
    {
        var points = SurfaceSampler.Sample(mesh, texture, samples);
        var cloud = ScriptableObject.CreateInstance<SurfacePointCloud>();
        cloud.Initialize(points);
        AssetDatabase.CreateAsset(cloud, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    */

}

