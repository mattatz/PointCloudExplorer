using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PointCloudEvent : UnityEvent<PointCloudRendererBase> { }

public abstract class PointCloudRendererBase : MonoBehaviour, IGeneratorResponsible
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

    public ComputeBuffer PointBuffer { get { return buffer; } }
    public PointCloudData Cloud { get { return cloud; } set { cloud = value; } }

    public PointCloudEvent onSetup;
    [SerializeField] protected PointCloudData cloud;
    [SerializeField] protected Material material;
    [SerializeField] protected ComputeShader compute;

    public Character Character { get; set; }
    protected Frustum frustum { get { return Character.Frustum; } }
    public List<GeneratorBase> Generators { get; set; }
    public float Alpha { get { return alpha; } set { alpha = value; } }

    [SerializeField, Range(0f, 1f)] protected float alpha = 1.0f;
    [SerializeField, Range(0f, 0.25f)] protected float pointSize = 0.05f;
    [SerializeField, Range(0f, 1f)] protected float decay = 0.9f;
    [SerializeField, Range(0f, 1f)] protected float deceleration = 0.5f;
    [SerializeField, Range(-1f, 1f)] protected float heightOffset = 0f;

    protected ComputeBuffer buffer;
    protected MaterialPropertyBlock block;
    protected Bounds localBounds;

    protected Coroutine iDisplay;
    protected Coroutine iRotater;

    protected virtual void Start()
    {
        block = new MaterialPropertyBlock();
    }

    protected void Update()
    {
        {
            // Compute
            // if (alpha > 0f)
            {
                Inertia();
                if (Generators != null)
                    Generators.ForEach(gen => Generator(gen));
                if (frustum != null && frustum.enabled && frustum.Alpha > 0f)
                    Frustum();
            }
        }

        if (alpha > 0f)
            Render();
    }

    protected void Inertia()
    {
        var kernel = compute.FindKernel("Update");
        compute.SetFloat("_DT", Time.deltaTime);
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);

        compute.SetVector("_Target", Character.transform.position);
        compute.SetFloat("_TargetForce", Character.Speed01);
        compute.SetFloat("_InvTargetDistance", 1f / 10f);

        compute.SetFloat("_Decay", decay);
        compute.SetFloat("_Deceleration", deceleration);
        compute.SetBuffer(kernel, "_PointBuffer", buffer);
        compute.SetInt("_InstancesCount", buffer.count);
        GPUHelper.Dispatch1D(compute, kernel, buffer.count);
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
        compute.SetBuffer(kernel, "_PointBuffer", buffer);
        compute.SetInt("_InstancesCount", buffer.count);
        GPUHelper.Dispatch1D(compute, kernel, buffer.count);
    }

    protected void Plane(PlaneGenerator planeGen)
    {
        var kernel = compute.FindKernel("Plane");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetFloat("_DT", Time.deltaTime);
        compute.SetBuffer(kernel, "_PointBuffer", buffer);
        compute.SetInt("_InstancesCount", buffer.count);

        var plane = planeGen.GetPlane();
        compute.SetVector("_PlaneNormal", plane.normal);
        compute.SetFloat("_PlaneDistance", plane.distance);
        compute.SetFloat("_PlaneWidth", planeGen.Width);
        GPUHelper.Dispatch1D(compute, kernel, buffer.count);
    }

    protected void Circle(CircleGenerator circle)
    {
        var kernel = compute.FindKernel("Circle");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetFloat("_DT", Time.deltaTime);
        compute.SetBuffer(kernel, "_PointBuffer", buffer);
        compute.SetInt("_InstancesCount", buffer.count);

        var center = transform.InverseTransformPoint(circle.transform.position);
        compute.SetVector("_CircleCenter", center);
        var mag = transform.localScale.magnitude;
        compute.SetFloat("_CircleRadius", circle.Radius / mag);
        compute.SetFloat("_CircleWidth", circle.Width / mag);
        GPUHelper.Dispatch1D(compute, kernel, buffer.count);
    }

    protected void Rotate(float t)
    {
        var kernel = compute.FindKernel("Rotate");
        compute.SetMatrix("_Transform", transform.localToWorldMatrix);
        compute.SetFloat("_Time", Time.timeSinceLevelLoad);
        compute.SetFloat("_DT", Time.deltaTime);
        compute.SetBuffer(kernel, "_PointBuffer", buffer);
        compute.SetInt("_InstancesCount", buffer.count);
        compute.SetVector("_RotateCenter", localBounds.center);

        // var localPoint = transform.InverseTransformPoint(worldCenter);
        // compute.SetVector("_RotateCenter", localPoint);

        var pi2 = Mathf.PI * 2f;
        compute.SetFloat("_RotateAngleQuadInOut", Easing.Quadratic.InOut(t) * pi2);
        compute.SetFloat("_RotateAngleExpoInOut", Easing.Exponential.InOut(t) * pi2);
        
        GPUHelper.Dispatch1D(compute, kernel, buffer.count);
    }

    public void Display(float duration, float delay, bool flag)
    {
        if (iDisplay != null) StopCoroutine(iDisplay);
        if (iRotater != null) StopCoroutine(iRotater);

        iDisplay = StartCoroutine(IDisplay(duration, delay, flag));
        iRotater = StartCoroutine(IRotater(duration, delay));
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

    protected IEnumerator IRotater(float duration, float delay)
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

    protected void Render()
    {
        block.SetMatrix("_Transform", transform.localToWorldMatrix);
        block.SetFloat("_PointSize", pointSize * alpha);
        Graphics.DrawProcedural(material, WorldBounds, MeshTopology.Points, buffer.count, 1, null, block);
    }

    protected void OnDrawGizmos()
    {
        if (buffer == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(localBounds.center, localBounds.size);
    }

    protected void OnDestroy()
    {
        if (buffer != null)
            buffer.Dispose();
    }

    public void Generator(GeneratorBase gen)
    {
        if (gen is PlaneGenerator)
            Plane(gen as PlaneGenerator);
        else if (gen is CircleGenerator)
            Circle(gen as CircleGenerator);
    }

}

