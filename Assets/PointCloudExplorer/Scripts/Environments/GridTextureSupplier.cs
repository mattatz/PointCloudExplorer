using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

[CustomEditor (typeof(GridTextureSupplier))]
public class GridTextureSupplierEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}

#endif

public class GridTextureSupplier : MonoBehaviour
{

    protected IEnumerable<IGridTextureUser> users;

    public void OnEnable()
    {
        var pattern = GridTexture.CreatePattern(1 << 7);
        var users = GetUsers();
        foreach (var u in users)
            u.Texture = pattern;
    }

    protected IEnumerable<IGridTextureUser> GetUsers()
    {
        return FindObjectsOfType<MonoBehaviour>().OfType<IGridTextureUser>();
    }

}
