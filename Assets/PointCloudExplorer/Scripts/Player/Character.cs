using System.Collections;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float Alpha { get { return alpha; } }
    public Frustum Frustum { get { return frustum; } }
    public float Speed01 { get { return Mathf.Clamp01(Velocity.magnitude); } }

    protected CharacterController characterController;
    public Vector3 Velocity => characterController.velocity;
    public Vector2 PitchYaw;

    [SerializeField] protected Frustum frustum;

    [SerializeField] protected ParticleSystem system;
    protected float rateOverTimeMultiplier = 0f;

    [SerializeField, Range(0f, 1f)] protected float alpha = 1f;
    [SerializeField] protected Renderer skinnedMeshRenderer;
    protected MaterialPropertyBlock block;

    protected virtual void Awake()
    {
        characterController = GetComponent<CharacterController>();

        var emission = system.emission;
        rateOverTimeMultiplier = emission.rateOverTimeMultiplier;

        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        block = new MaterialPropertyBlock();
        skinnedMeshRenderer.GetPropertyBlock(block);
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        block.SetFloat("_Alpha", alpha);
        skinnedMeshRenderer.SetPropertyBlock(block);

        var emission = system.emission;
        emission.rateOverTimeMultiplier = rateOverTimeMultiplier * Speed01;

        var dt = Time.deltaTime * 5f;
        frustum.transform.localRotation = Quaternion.Slerp(
            frustum.transform.localRotation,
            Quaternion.AngleAxis(PitchYaw.x, Vector3.right),
            dt
        );

        frustum.Focus(Speed01, dt);
    }

    public void Move(Vector3 movement, float rotationSpeed, float dt)
    {
        characterController.Move(movement * dt);
        OrientToTargetRotation(new Vector3(movement.x, 0f, movement.z), rotationSpeed * dt);
    }

    protected void OrientToTargetRotation(Vector3 horizontalMovement, float rotationSpeed)
    {
        if (horizontalMovement.sqrMagnitude > 0.0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed);
        }
    }

}
