using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour, IGridTextureUser
{
    public Texture2D Texture { get; set; }
    protected Renderer mRenderer;
    protected MaterialPropertyBlock block;

    protected void Start()
    {
        mRenderer = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        mRenderer.GetPropertyBlock(block);
        block.SetTexture("_MainTex", Texture);
        block.SetVector("_Up", transform.up);
        block.SetVector("_Right", transform.right);
    }

    public void Visualize(Vector3 world)
    {
        if (block != null)
        {
            block.SetVector("_Position", world);
            mRenderer.SetPropertyBlock(block);
        }
    }

}

