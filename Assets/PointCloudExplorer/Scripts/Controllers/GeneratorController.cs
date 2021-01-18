using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GeneratorController : MonoBehaviour, IPointCloudSetup
{

    public List<GeneratorBase> Generators { get { return generators; } }

    public Character Character { get; set; }

    [Header("Generators")]
    [SerializeField] protected List<GeneratorBase> generators;
    [SerializeField] protected PlaneGenerator planeGenPrefab;
    [SerializeField] protected CircleGenerator circleGenPrefab;
    [SerializeField] protected RectangleGenerator rectangleGenPrefab;

    protected Bounds worldBounds;
    protected Coroutine iPlaneGenerator;

    protected List<IGeneratorResponsible> responsibles;

    protected void Start()
    {
        responsibles = FindObjectsOfType<MonoBehaviour>().OfType<IGeneratorResponsible>().ToList();
        // iPlaneGenerator = StartCoroutine(IPlaneGenerator(Vector3.back));
    }

    protected void Update()
    {
        foreach (var r in responsibles)
        {
            r.Generators = generators;
        }
        CheckGenerators();
    }

    public void OnSetup(PointCloudController pcx)
    {
        worldBounds = pcx.WorldBounds;
    }

    protected IEnumerator IPlaneGenerator(Vector3 dir, float interval = 5f)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            // var point = worldBounds.center - dir * Mathf.Abs(Vector3.Dot(dir, worldBounds.size)) * 0.5f;
            AddPlane(dir);
        }
    }

    public void AddPlane(Vector3 dir)
    {
        var point = -dir * Mathf.Abs(Vector3.Dot(dir, worldBounds.size)) * 0.5f;
        AddPlane(point, dir);
    }

    protected void AddPlane(Vector3 point, Vector3 forward)
    {
        var gen = Instantiate(planeGenPrefab);
        var go = gen.gameObject;
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.position = point;
        go.transform.rotation = Quaternion.LookRotation(forward);
        gen.SetBounds(worldBounds);
        generators.Add(gen);
    }

    public void AddCircle(Vector3 position, float speed = 1f, float delay = 0f)
    {
        StartCoroutine(ICircleGenerator(position, speed, delay));
    }

    protected IEnumerator ICircleGenerator(Vector3 center, float speed, float delay)
    {
        yield return new WaitForSeconds(delay);
        var gen = Instantiate(circleGenPrefab);
        gen.speedMultiplier = speed;
        var go = gen.gameObject;
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.position = center;
        gen.SetBounds(worldBounds);
        generators.Add(gen);
    }

    public void AddRectangle()
    {
        var gen = Instantiate(rectangleGenPrefab);
        var go = gen.gameObject;
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(worldBounds.center.x, worldBounds.min.y, worldBounds.center.z);
        gen.SetBounds(worldBounds);
        generators.Add(gen);
    }

    protected void CheckGenerators()
    {
        for (int i = generators.Count - 1; i >= 0; i--)
        {
            var gen = generators[i];
            if (gen.Finished())
            {
                Destroy(gen.gameObject);
                generators.RemoveAt(i);
            }
        }
    }

}
