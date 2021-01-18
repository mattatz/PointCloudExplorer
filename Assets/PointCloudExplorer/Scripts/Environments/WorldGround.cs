using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGround : MonoBehaviour, IPointCloudSetup, IGridTextureUser
{
    public Texture2D Texture { get; set; }

    protected MaterialPropertyBlock block;
    protected Renderer mRenderer;
    [SerializeField] protected Character target;
    [SerializeField] protected float radius = 10f;
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected float padding = 300f;

    protected Vector3 position;
    protected int counter = 0;

    protected void OnEnable()
    {
    }

    protected void Start()
    {
        block = new MaterialPropertyBlock();
        mRenderer = GetComponent<Renderer>();
        mRenderer.GetPropertyBlock(block);
        block.SetTexture("_MainTex", Texture);
        mRenderer.SetPropertyBlock(block);

        position = target.transform.position;
    }

    protected void Update()
    {
        var dt = Time.deltaTime;

        var current = target.transform.position;

        position = Vector3.Lerp(position, current, dt * speed);

        block.SetVector("_Position", position);
        block.SetFloat("_Radius", radius * target.Alpha);
        mRenderer.SetPropertyBlock(block);
    }

    public void OnSetup(PointCloudController controller)
    {
        var bounds = controller.WorldBounds;
        var min = bounds.min;
        var max = bounds.max;
        var center = bounds.center;
        transform.position = new Vector3(center.x, transform.position.y, center.z);
        var scale = transform.localRotation * (bounds.size + Vector3.one * padding);
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        scale.z = 1.0f;
        transform.localScale = scale;
    }

}
