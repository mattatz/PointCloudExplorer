using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

[System.Serializable]
public class MeshPointCloudEvent : UnityEvent<MeshPointCloudRenderer> { }

public class MeshPointCloudRenderer : MonoBehaviour, IGeneratorResponsible
{
    public Bounds LocalBounds { get { return localBounds; } }
    public Bounds WorldBounds {
        get {
            var min = localBounds.min;
            var max = localBounds.max;

            var corners = new Vector3[] {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(min.x, max.y, max.z),
            }.Select(p => transform.TransformPoint(p));
            var wbounds = new Bounds(corners.First(), Vector3.zero);
            foreach (var p in corners) wbounds.Encapsulate(p);
            return wbounds;
        }
    }

    public PointCloudData Cloud { get { return cloud; } set { cloud = value; } }

    public MeshPointCloudEvent onSetup;
    [SerializeField] protected List<Mesh> meshes;
    [SerializeField] protected PointCloudData cloud;
    [SerializeField] protected ComputeShader compute;
    [SerializeField] protected bool interactive = true;
    [SerializeField] protected bool debug = false;

    [Header ("Rendering")]
    [SerializeField] protected Material material;
    [SerializeField] protected ShadowCastingMode shadowCasting;
    [SerializeField] protected AnimationCurve lifetimeCurve;
    protected Texture2D lifetime;

    public Character Character { get; set; } = null;
    protected Frustum frustum { get { return Character.Frustum; } }
    public List<GeneratorBase> Generators { get; set; }
    public float Alpha { get { return alpha; } set { alpha = value; } }

    [SerializeField, Range(0f, 1f)] protected float alpha = 1.0f;
    [SerializeField, Range(0f, 0.5f)] protected float pointSize = 0.05f;
    [SerializeField, Range(0f, 1f)] protected float decay = 0.9f;
    [SerializeField, Range(0f, 1f)] protected float deceleration = 0.5f;

    protected Mesh quad;
    protected MaterialPropertyBlock block;
    protected Bounds localBounds;

    [Header("Grids")]
    [SerializeField, Range(1, 64)] protected int gridResolution = 16;
    [SerializeField] protected float gridValidationThreshold = 30f;
    [SerializeField] protected List<PointCloudGrid> grids;
    [SerializeField] protected Material gridMaterial;
    [SerializeField] protected bool renderGrids;
    protected float gridUnitLength;

    protected Coroutine iDisplay, iRotater;

    protected virtual void Start()
    {
        quad = BuildQuad();

        block = new MaterialPropertyBlock();
        lifetime = CreateCurve(lifetimeCurve);
        block.SetTexture("_LifetimeCurve", lifetime);

        cloud = PointCloudData.Build(meshes);
        meshes.Clear();

        var points = cloud.Points;
        localBounds = cloud.LocalBounds;

        grids = BuildGrids(cloud, gridResolution);
        foreach (var g in grids)
            Invalidate(g);

        onSetup.Invoke(this);
    }

    protected void Update()
    {
        foreach(var grid in grids)
            grid.Transform(transform);

        ValidateGrids(gridValidationThreshold);

        {
            // Compute
            // if (alpha > 0f)
            {
                if (Character != null)
                    Inertia();

                if (interactive)
                {
                    if (Generators != null)
                        Generators.ForEach(gen => Generator(gen));
                    if (Character != null && frustum != null && frustum.enabled && frustum.Alpha > 0f)
                        Frustum();
                }
            }
        }

        if (alpha > 0f)
            RenderGrids();
    }

    #region Kernels

    protected void Invalidate(PointCloudGrid grid)
    {
        var kernel = compute.FindKernel("Invalidate");
        compute.SetBuffer(kernel, "_PointBuffer", grid.PointBuffer);
        compute.SetInt("_InstancesCount", grid.PointBuffer.count);
        GPUHelper.Dispatch1D(compute, kernel, grid.PointBuffer.count);
    }

    protected void Inertia()
    {
        var kernel = compute.FindKernel("Update");
        compute.SetFloat("_DT", Time.deltaTime);
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);

        compute.SetVector("_Target", Character.transform.position);
        compute.SetFloat("_TargetForce", Character.Speed01);
        compute.SetFloat("_InvTargetDistance", 1f / 8f);
        compute.SetFloat("_InvScale", 1f / transform.localScale.x);

        compute.SetFloat("_Decay", decay);
        compute.SetFloat("_Deceleration", deceleration);
        compute.SetFloat("_Debug", debug ? 1f : 0f);

        KernelGrids(kernel);
    }

    protected void Frustum()
    {
        var kernel = compute.FindKernel("Frustum");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetMatrix("_View", frustum.V);
        compute.SetMatrix("_Projection", frustum.P);
        compute.SetMatrix("_MVP", frustum.VP * transform.localToWorldMatrix);
        compute.SetFloat("_Near", frustum.Near);
        compute.SetFloat("_Far", frustum.Far);
        compute.SetFloat("_DT", Time.deltaTime * frustum.Alpha);
        // compute.SetFloat("_InvFrustumRadius", 1f / (frustum.Radius * (1f - Character.Speed01)));
        compute.SetFloat("_InvFrustumRadius", 1f / frustum.Radius);

        KernelGrids(kernel);
    }

    protected void Plane(PlaneGenerator planeGen)
    {
        var kernel = compute.FindKernel("Plane");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetFloat("_DT", Time.deltaTime);

        var plane = planeGen.GetPlane();
        compute.SetVector("_PlaneNormal", plane.normal);
        compute.SetFloat("_PlaneDistance", plane.distance);
        compute.SetFloat("_PlaneWidth", planeGen.Width);

        KernelGrids(kernel);
    }

    protected void Circle(CircleGenerator circle)
    {
        var kernel = compute.FindKernel("Circle");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetFloat("_DT", Time.deltaTime);

        var center = transform.InverseTransformPoint(circle.transform.position);
        compute.SetVector("_CircleCenter", center);
        var mag = transform.localScale.magnitude;
        compute.SetFloat("_CircleRadius", circle.Radius / mag);
        compute.SetFloat("_CircleWidth", circle.Width / mag);

        KernelGrids(kernel);
    }

    protected void Rectangle(RectangleGenerator rectangle)
    {
        var kernel = compute.FindKernel("Rectangle");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetFloat("_DT", Time.deltaTime);
        compute.SetFloat("_RectangleWidth", rectangle.Width);
        compute.SetFloat("_RectangleHeight", rectangle.transform.position.y);
        compute.SetFloat("_RectangleDepth", rectangle.Depth);

        KernelGrids(kernel);
    }

    protected void Rotate(float t)
    {
        var kernel = compute.FindKernel("Rotate");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetMatrix("_InvTransform", transform.worldToLocalMatrix);
        compute.SetFloat("_Time", Time.timeSinceLevelLoad);
        compute.SetFloat("_DT", Time.deltaTime);
        // compute.SetVector("_WorldRotateCenter", transform.TransformPoint(localBounds.center));
        compute.SetVector("_WorldRotateCenter", Character.transform.position);

        const float pi2 = Mathf.PI * 2f;
        compute.SetFloat("_RotateAngleQuadInOut", Easing.Quadratic.InOut(t) * pi2);
        compute.SetFloat("_RotateAngleExpoInOut", Easing.Exponential.InOut(t) * pi2);

        KernelGrids(kernel);
    }

    #endregion

    public void Display(float duration, float delay, bool flag)
    {
        if (iDisplay != null) StopCoroutine(iDisplay);
        if (iRotater != null) StopCoroutine(iRotater);
        iDisplay = StartCoroutine(IDisplay(duration, delay, flag));
        iRotater = StartCoroutine(IRotater(duration, delay));

        // if (iDisplay != null) StopCoroutine(iDisplay);
        // iDisplay = StartCoroutine(IDisplay(duration, delay, flag));
    }

    public void Rotater()
    {
        if (iRotater != null) StopCoroutine(iRotater);
        iRotater = StartCoroutine(IRotater(5f));
    }

    protected IEnumerator IDisplay(float duration, float delay, bool on)
    {
        yield return new WaitForSeconds(delay);

        var time = 0f;
        var from = alpha;
        var to = on ? 1f : 0f;
        while (time < duration)
        {
            yield return 0;
            alpha = Mathf.Lerp(
                from, 
                to, 
                // Easing.Quadratic.InOut(time / duration)
                on ? Easing.Cubic.In(time / duration) : Easing.Quadratic.Out(time / duration)
            );
            time += Time.deltaTime;
        }

        alpha = to;
    }

    protected IEnumerator IRotater(float duration, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);

        var time = 0f;

        while (time < duration)
        {
            yield return 0;
            Rotate(time / duration);
            time += Time.deltaTime;
        }

        Rotate(1f);
    }

    protected void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(localBounds.center, localBounds.size);
    }

    protected void OnRenderObject()
    {
        if (!renderGrids) return;
        foreach (var g in grids)
            if (g.On) RenderGridBounds(g);
    }

    protected void OnDestroy()
    {
        foreach (var g in grids)
            g.Dispose();
        grids.Clear();
    }

    public void Generator(GeneratorBase gen)
    {
        if (gen is PlaneGenerator)
            Plane(gen as PlaneGenerator);
        else if (gen is CircleGenerator)
            Circle(gen as CircleGenerator);
        else if (gen is RectangleGenerator)
            Rectangle(gen as RectangleGenerator);
    }

    #region Mesh

    protected Mesh BuildQuad()
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;
        mesh.SetVertices(new Vector3[] {
            new Vector3(-0.5f,  0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0)
        });
        mesh.SetUVs(0, new Vector2[] {
            new Vector2(-0.5f, -0.5f),
            new Vector2( 0.5f, -0.5f),
            new Vector2( 0.5f,  0.5f),
            new Vector2(-0.5f,  0.5f)
        });
        mesh.SetIndices(
            new int[] {
                0, 2, 1,
                2, 0, 3
            },
            MeshTopology.Triangles, 0
        );
        mesh.RecalculateBounds();
        return mesh;
    }

    #endregion

    #region Grid

    protected List<PointCloudGrid> BuildGrids(PointCloudData cloud, int gridResolution = 10)
    {
        var grids = new List<PointCloudGrid>();
        if (gridResolution <= 1)
        {
            var one = new PointCloudGrid(cloud.LocalBounds);
            one.Add(cloud.Points);
            one.Build();
            grids.Add(one);
            return grids;
        }

        var points = cloud.Points.ToList();
        var localBounds = cloud.LocalBounds;
        var min = localBounds.min;

        gridUnitLength = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z) / gridResolution;
        var w = Mathf.CeilToInt(localBounds.size.x / gridUnitLength);
        var h = Mathf.CeilToInt(localBounds.size.y / gridUnitLength);
        var d = Mathf.CeilToInt(localBounds.size.z / gridUnitLength);
        // Debug.Log(string.Format("{0}:{1}:{2}", w, h, d));

        var usize = Vector3.one * gridUnitLength;
        for (int iz = 0; iz < d; iz++)
        {
            float cz = (iz + 0.5f) * gridUnitLength;
            for (int iy = 0; iy < h; iy++)
            {
                float cy = (iy + 0.5f) * gridUnitLength;
                for (int ix = 0; ix < w; ix++)
                {
                    float cx = (ix + 0.5f) * gridUnitLength;
                    var bb = new Bounds(min + new Vector3(cx, cy, cz), usize);
                    grids.Add(new PointCloudGrid(bb));
                }
            }
        }

        var wxh = w * h;
        foreach (var p in points)
        {
            var position = (p.position - min);
            int ix = Mathf.FloorToInt(position.x / gridUnitLength);
            int iy = Mathf.FloorToInt(position.y / gridUnitLength);
            int iz = Mathf.FloorToInt(position.z / gridUnitLength);
            int index = iz * wxh + iy * w + ix;
            if (0 <= index && index < grids.Count)
                grids[index].Add(p);
        }

        const int threshold = 64;
        var valids = grids.FindAll(g => !g.Empty(threshold));
        valids.ForEach(g => g.Build());
        // Debug.Log("grids :" + valids.Count);
        return valids;
    }

    protected void ValidateGrids(float threshold)
    {

        if (debug)
        {
            foreach (var grid in grids) grid.Force(true);
            return;
        }

        foreach (var grid in grids)
        {
            var prev = grid.On;
            var current = grid.Visible(Character.transform.position, threshold);
            if (prev && !current)
                Invalidate(grid);
        }
    }

    protected void RenderGrids()
    {
        block.SetMatrix("_Transform", transform.localToWorldMatrix);
        block.SetMatrix("_InvTransform", transform.worldToLocalMatrix);
        block.SetFloat("_PointSize", pointSize * alpha);

        foreach(var grid in grids)
        {
            if (grid.On)
            {
                var buffer = grid.PointBuffer;
                block.SetBuffer("_PointBuffer", buffer);
                Graphics.DrawMeshInstancedProcedural(quad, 0, material, grid.WorldBounds, buffer.count, block, shadowCasting);
            }
        }
    }

    protected void KernelGrids(int kernel)
    {
        foreach (var grid in grids)
        {
            if (grid.On)
            {
                var buffer = grid.PointBuffer;
                compute.SetBuffer(kernel, "_PointBuffer", buffer);
                compute.SetBuffer(kernel, "_OriginBuffer", grid.OriginBuffer);
                compute.SetInt("_InstancesCount", buffer.count);
                GPUHelper.Dispatch1D(compute, kernel, buffer.count);
            }
        }
    }

    protected void RenderGridBounds(PointCloudGrid grid)
    {
        var bounds = grid.LocalBounds;
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        var min = bounds.min;
        var max = bounds.max;
        var p0 = new Vector3(min.x, min.y, min.z);
        var p1 = new Vector3(max.x, min.y, min.z);
        var p2 = new Vector3(max.x, min.y, max.z);
        var p3 = new Vector3(min.x, min.y, max.z);
        var p4 = new Vector3(min.x, max.y, min.z);
        var p5 = new Vector3(max.x, max.y, min.z);
        var p6 = new Vector3(max.x, max.y, max.z);
        var p7 = new Vector3(min.x, max.y, max.z);

        gridMaterial.SetPass(0);

        GL.Begin(GL.LINES);

        GL.Vertex(p0); GL.Vertex(p1);
        GL.Vertex(p1); GL.Vertex(p2);
        GL.Vertex(p2); GL.Vertex(p3);
        GL.Vertex(p3); GL.Vertex(p0);

        GL.Vertex(p4); GL.Vertex(p5);
        GL.Vertex(p5); GL.Vertex(p6);
        GL.Vertex(p6); GL.Vertex(p7);
        GL.Vertex(p7); GL.Vertex(p4);

        GL.Vertex(p0); GL.Vertex(p4);
        GL.Vertex(p1); GL.Vertex(p5);
        GL.Vertex(p2); GL.Vertex(p6);
        GL.Vertex(p3); GL.Vertex(p7);

        GL.End();

        GL.PopMatrix();
    }


    #endregion

    #region Texture

    protected Texture2D CreateCurve(AnimationCurve curve, int width = 64)
    {
        var tex = new Texture2D(width, 1);
        tex.wrapMode = TextureWrapMode.Clamp;
        var inv = 1f / (width - 1);
        for (int x = 0; x < width; x++)
        {
            float t = x * inv;
            var value = curve.Evaluate(t);
            tex.SetPixel(x, 0, new Color(value, value, value, value));
        }
        tex.Apply();
        return tex;
    }

    #endregion

}
