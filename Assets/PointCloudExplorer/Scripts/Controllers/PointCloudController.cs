using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PointCloudControllerEvent : UnityEvent<PointCloudController> { }

public class PointCloudController : MonoBehaviour, IPlayerResponsible
{
    public Bounds WorldBounds { get { return worldBounds; } }
    public Character Player { get; set; }

    [SerializeField] protected List<MeshPointCloudRenderer> clouds;
    [SerializeField] protected PointCloudControllerEvent onSetup;

    protected int counter = 0;
    protected Bounds worldBounds;
    protected int current = 0;

    protected void OnEnable()
    {
        var receivers = FindObjectsOfType<MonoBehaviour>().OfType<IPointCloudSetup>();
        foreach (var r in receivers)
            onSetup.AddListener(r.OnSetup);

        clouds = GetComponentsInChildren<MeshPointCloudRenderer>().ToList();

        // Hide except for first one
        for (int i = 0; i < clouds.Count; i++)
            clouds[i].Alpha = i <= 0 ? 1f : 0f;

        foreach (var cl in clouds)
            cl.onSetup.AddListener(OnSetupPcx);
    }

    protected void Start()
    {
        FeedInteraction();
    }

    protected void Update()
    {
        FeedInteraction();
    }

    protected void OnDestroy()
    {
    }

    public void Next(float duration = 5f)
    {
        clouds[current].Display(duration, 0f, false);
        current = (current + 1) % clouds.Count;
        clouds[current].Display(duration, 0f, true);
    }

    protected void OnSetupPcx(MeshPointCloudRenderer renderer)
    {
        counter++;

        if (counter >= clouds.Count)
        {
            worldBounds = renderer.WorldBounds;
            clouds.ForEach(rnd =>
            {
                var other = rnd.WorldBounds;
                worldBounds.Encapsulate(other);
            });

            onSetup.Invoke(this);
        }
    }

    protected void FeedInteraction()
    {
        foreach (var cl in clouds)
        {
            cl.Character = Player;
        }
    }

    protected void OnDrawGizmos()
    {
    }

    protected struct PointLight
    {
        public Vector3 position;
        public float invDistance;
    };

}
