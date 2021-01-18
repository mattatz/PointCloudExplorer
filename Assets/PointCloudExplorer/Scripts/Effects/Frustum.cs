using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Frustum : MonoBehaviour
{
    public Matrix4x4 V { get { return viewMatrix; } }
    public Matrix4x4 P { get { return projectionMatrix; } }
    public Matrix4x4 VP { get { return viewProjectionMatrix; } }
    public float Near { get { return near; } }
    public float Far { get { return far; } }
    public float Alpha { get { return alpha; } }
    public float Radius { get { return radius; } }

    [SerializeField, Range(0f, 1f)] protected float alpha = 1f;
    [SerializeField, Range(-3f, 3f)] protected float offset = 1.7f;
    [SerializeField] protected float near = 0.1f, far = 100f;
    [SerializeField] protected float fieldOfView = 60f;
    [SerializeField] protected float fovMin = 35f, fovMax = 80f;
    protected float targetFov;

    [SerializeField, Range(0.1f, 2f)] protected float aspect = 1f;
    [SerializeField] protected float radius = 8f;
    protected Matrix4x4 viewMatrix, projectionMatrix, viewProjectionMatrix;

    protected Coroutine iFader;

    protected Mesh mesh;
    protected Renderer rnd;
    protected MaterialPropertyBlock block;

    protected void OnEnable()
    {
        mesh = CreateFrustumMesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        block = new MaterialPropertyBlock();
        rnd = GetComponent<Renderer>();
        rnd.GetPropertyBlock(block);
    }

    protected void Start()
    {
    }

    protected void Update()
    {
        UpdateMatrix();

        if (rnd != null)
        {
            block.SetFloat("_Alpha", alpha);
            rnd.SetPropertyBlock(block);
        }
    }

    public void Focus(float t01, float dt)
    {
        targetFov = Mathf.Lerp(fovMin, fovMax, Mathf.Clamp01(1f - t01));
        fieldOfView = Mathf.Lerp(fieldOfView, targetFov, dt);
        UpdateFrustumMesh(mesh);
    }

    protected void UpdateMatrix()
    {
        viewMatrix = transform.worldToLocalMatrix;

        float left, right, bottom, top;
        GetCorners(near, far, fieldOfView, aspect, out left, out right, out bottom, out top);
        projectionMatrix = Matrix4x4.Frustum(left, right, bottom, top, near, far);

        viewProjectionMatrix = projectionMatrix * viewMatrix;
    }

    protected void GetCorners(float near, float far, float fieldOfView, float aspect, out float left, out float right, out float bottom, out float top)
    {
        var tan = Mathf.Tan(0.5f * fieldOfView * Mathf.Deg2Rad);
        top = near * tan;
        bottom = -top;
        right = top * aspect;
        left = -right;
    }

    protected Vector3[] GetFrustumMeshVertices()
    {
        var tan = Mathf.Tan(0.5f * fieldOfView * Mathf.Deg2Rad);

        float nearLeft, nearRight, nearBottom, nearTop;
        nearTop = near * tan;
        nearBottom = -nearTop;
        nearRight = nearTop * aspect;
        nearLeft = -nearRight;

        float farLeft, farRight, farBottom, farTop;
        farTop = far * tan;
        farBottom = -farTop;
        farRight = farTop * aspect;
        farLeft = -farRight;

        var up = new Vector3(0, offset, 0);

        Vector3
            nearLT = new Vector3(nearLeft, nearTop, near),
            nearRT = new Vector3(nearRight, nearTop, near),
            nearRB = new Vector3(nearRight, nearBottom, near),
            nearLB = new Vector3(nearLeft, nearBottom, near);

        Vector3
            farLT = new Vector3(farLeft, farTop, far),
            farRT = new Vector3(farRight, farTop, far),
            farRB = new Vector3(farRight, farBottom, far),
            farLB = new Vector3(farLeft, farBottom, far);

        return new Vector3[]
        {
            nearLT, nearRT, nearRB, nearLB,
            farLT, farRT, farRB, farLB
        }.Select(v => v + up).ToArray();
    }

    protected void UpdateFrustumMesh(Mesh mesh)
    {
        var vertices = GetFrustumMeshVertices();
        mesh.SetVertices(vertices);
        mesh.MarkModified();
    }

    protected Mesh CreateFrustumMesh()
    {
        var mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave;

        var vertices = GetFrustumMeshVertices();
        var indices = new int[] {
            0, 1, 1, 2, 2, 3, 3, 0, // near
            4, 5, 5, 6, 6, 7, 7, 4, // far
            0, 4, 1, 5, 2, 6, 3, 7 // side
        };
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.MarkDynamic();
        return mesh;
    }

    public void Show(float duration = 1f)
    {
        if (iFader != null) StopCoroutine(iFader);
        iFader = StartCoroutine(IFader(1f, duration));
    }

    public void Hide(float duration = 1f)
    {
        if (iFader != null) StopCoroutine(iFader);
        iFader = StartCoroutine(IFader(0f, duration));
    }

    protected IEnumerator IFader(float to, float duration)
    {
        yield return 0;

        var time = 0f;
        var from = alpha;
        while (time < duration)
        {
            yield return 0;
            time += Time.deltaTime;
            alpha = Mathf.Lerp(from, to, time / duration);
        }
        alpha = to;
    }

    protected void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, fieldOfView, far, near, aspect);
    }

}
