using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleGenerator : GeneratorBase
{

    public float Radius { get { return radius * amount; } }
    public float Width { get { return width; } }
    public override bool Finished() { return amount > 1f; }

    [SerializeField] protected float width = 30f;
    [SerializeField] protected float speed = 3f;
    [SerializeField] protected float radius = 100f;
    [Range(0.1f, 3f)] public float speedMultiplier = 1f;
    protected float amount = 0f;

    protected Renderer rnd;
    protected MaterialPropertyBlock block;

    protected void OnEnable()
    {
        rnd = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        rnd.GetPropertyBlock(block);

        Build(radius);
    }

    protected void Update()
    {
        float t01 = Mathf.Clamp01(amount);
        block.SetFloat("_Alpha", 1f - Easing.Quadratic.In(t01));
        rnd.SetPropertyBlock(block);
    }

    protected void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime * speed * speedMultiplier;
        transform.localScale = new Vector3(amount, 1f, amount);
        amount += dt;
    }

    public void SetBounds(Bounds worldBounds)
    {
    }

    protected void Build(float radius, int resolution = 32)
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        var vertices = new Vector3[resolution];
        var indices = new int[resolution * 2];
        var pi2 = Mathf.PI * 2f;
        var scale = (1f / resolution) * pi2;
        for (int i = 0; i < resolution; i++)
        {
            var t = i * scale;
            var dx = Mathf.Cos(t) * radius;
            var dy = Mathf.Sin(t) * radius;
            vertices[i].Set(dx, 0f, dy);
            indices[i * 2] = i;
            indices[i * 2 + 1] = (i + 1) % resolution;
        }

        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    protected void OnDrawGizmos()
    {
        // Gizmos.matrix = transform.localToWorldMatrix;
        // Gizmos.DrawWireSphere(Vector3.zero, radius);
    }

}
