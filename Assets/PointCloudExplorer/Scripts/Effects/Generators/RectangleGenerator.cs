using UnityEngine;

public class RectangleGenerator : GeneratorBase
{
    public float Width { get { return bounds.size.x; } }
    public float Height { get { return bounds.size.y; } }
    public float Depth { get { return bounds.size.z; } }
    public override bool Finished() { return false; }

    [SerializeField] protected float speed = 1f;
    protected float amount = 0f;
    protected Bounds bounds;

    protected Renderer rnd;
    protected MaterialPropertyBlock block;

    protected void OnEnable()
    {
        rnd = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        rnd.GetPropertyBlock(block);
        block.SetFloat("_Alpha", 1f);
        rnd.SetPropertyBlock(block);
    }

    protected void Update()
    {
    }

    protected void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime * speed;
        float y01 = (Mathf.Sin(amount) + 1f) * 0.5f;
        transform.position = new Vector3(0f, Mathf.Lerp(bounds.min.y, bounds.max.y, y01), 0f);
        amount += dt;
    }

    public void SetBounds(Bounds worldBounds)
    {
        bounds = worldBounds;
        Build(bounds.size.x, bounds.size.z);
    }

    protected void Build(float width, float depth)
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        var hw = width * 0.5f;
        var hh = depth * 0.5f;
        var vertices = new Vector3[] {
            new Vector3(-hw, 0f, -hh),
            new Vector3(hw, 0f, -hh),
            new Vector3(hw, 0f, hh),
            new Vector3(-hw, 0f, hh)
        };
        var indices = new int[]
        {
            0, 1, 1, 2, 2, 3, 3, 0
        };
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    protected void OnDrawGizmos()
    {
    }

}
