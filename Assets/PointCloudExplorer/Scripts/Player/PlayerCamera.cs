using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] protected float positionSmoothTime = 0.1f;
    protected float positionSmoothTimeTarget = 0f;
    [SerializeField, Range(0.1f, 10f)] protected float positionSmoothChangeSpeed = 0.5f;

    [SerializeField, Range(0.1f, 10f)] protected float rotationSpeed = 0.5f;
    [SerializeField] protected Transform rig, pivot;

    protected Vector3 cameraVelocity;

    protected void OnEnable()
    {
        positionSmoothTimeTarget = positionSmoothTime;
    }

    protected void Update()
    {
        var dt = Time.deltaTime;
        positionSmoothTime = Mathf.Lerp(positionSmoothTime, positionSmoothTimeTarget, dt * positionSmoothChangeSpeed);
    }

    public void Move(Vector3 position, Vector2 pitchYaw)
    {
        rig.position = Vector3.SmoothDamp(rig.position, position, ref cameraVelocity, positionSmoothTime);

        Quaternion rigTargetLocalRotation = Quaternion.Euler(0.0f, pitchYaw.y, 0.0f);
        Quaternion pivotTargetLocalRotation = Quaternion.Euler(pitchYaw.x, 0.0f, 0.0f);
        var dt = rotationSpeed * Time.deltaTime;
        var t = Mathf.Clamp01(dt);
        rig.localRotation = Quaternion.Slerp(rig.localRotation, rigTargetLocalRotation, t);
        pivot.localRotation = Quaternion.Slerp(pivot.localRotation, pivotTargetLocalRotation, t);
    }

}
