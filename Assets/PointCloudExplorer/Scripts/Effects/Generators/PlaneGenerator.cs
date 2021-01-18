using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneGenerator : GeneratorBase
{
    public float Width { get { return width; } }
    public float Distance { get { return distance; } }
    public override bool Finished() { return amount > Distance; }

    [SerializeField] protected float width = 30f;
    [SerializeField] protected float speed = 320f;
    protected float distance = 0f;
    protected float amount = 0f;

    protected Renderer rnd;
    protected MaterialPropertyBlock block;

    protected void OnEnable()
    {
        rnd = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        rnd.GetPropertyBlock(block);
    }

    protected void Update()
    {
        float t01 = Mathf.Clamp01(amount / distance);
        block.SetFloat("_Alpha", (amount <= 0f || distance <= 0f) ? 0f : Mathf.Sin(t01 * Mathf.PI));
        rnd.SetPropertyBlock(block);
    }

    protected void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime * speed;
        var translation = transform.forward * dt;
        transform.Translate(translation, Space.World);
        amount += dt;
    }

    public void SetBounds(Bounds worldBounds)
    {
        var size = worldBounds.size;
        distance = Mathf.Abs(Vector3.Dot(transform.forward, size)) + width;

        var length = Mathf.Abs(Vector3.Dot(transform.right, size));
        Build(length);
    }

    protected void Build(float length)
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;
        mesh.SetVertices(new Vector3[] { new Vector3(-length, 0f, 0f), new Vector3(length, 0f, 0f) });
        mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public Plane GetPlane()
    {
        var normal = transform.forward;
        var distance = Vector3.Dot(normal, transform.position);
        return new Plane(normal, distance);
    }

    protected void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        const float size = 1e3f;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size, size, width));
    }

}
