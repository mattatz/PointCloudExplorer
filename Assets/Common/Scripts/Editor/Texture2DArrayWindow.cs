using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;


public class Texture2DArrayWindow : EditorWindow
{

    protected int resolution = 512;

    [MenuItem("Window/Texture2DArray")]
    static void Init()
    {
        EditorWindow.GetWindow<Texture2DArrayWindow>().Show();
    }

    protected void OnGUI()
    {
        Object[] selection = Selection.objects;
        GUILayout.Label(string.Format("# of Selections: {0}", selection.Length));

        resolution = EditorGUILayout.IntField("resolution:", resolution);

        if(GUILayout.Button("Create"))
        {
            Texture2D[] textures = new Texture2D[selection.Length];

            for (int i = 0; i < textures.Length; i++)
            {
                // textures[i] = (Texture2D)selection[i];
                textures[i] = Create((Texture2D)selection[i], resolution);
            }

            Texture2DArray array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, true);
            for (int i = 0; i < textures.Length; i++)
            {
                Graphics.CopyTexture(textures[i], 0, 0, array, i, 0);
                // array.SetPixels(textures[i].GetPixels(), i);
            }
            array.Apply();

            var name = selection[0].name;
            AssetDatabase.CreateAsset(array, string.Format("Assets/{0}-{1}.asset", name, resolution));
        }
    }

    protected Texture2D Create(Texture2D source, int size)
    {
        var rt = new RenderTexture(size, size, 0);
        rt.Create();
        Graphics.Blit(source, rt);

        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);

        var tmp = RenderTexture.active;
        {
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
        }
        RenderTexture.active = tmp;

        rt.Release();
        return tex;
    }

}
