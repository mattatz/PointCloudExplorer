using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MovementSettings
{
    public float acceleration = 25.0f;
    public float decceleration = 25.0f;
    public float maxHorizontalSpeedOnWalking = 2.0f;
    public float maxHorizontalSpeedOnRunning = 10.0f;
    public float gravity = -20.0f;

    public float minPitchAngle = -45.0f, maxPitchAngle = 75.0f;
    public float minRotationSpeed = 600.0f, maxRotationSpeed = 1200.0f;

    public float GetMaxHorizontalSpeed(bool running)
    {
        return running ? maxHorizontalSpeedOnRunning : maxHorizontalSpeedOnWalking;
    }
}

[System.Serializable]
public class PlayerKeyInputEvent : UnityEvent<KeyCode> { }

public class PlayerController : MonoBehaviour {

    [SerializeField] protected GeneratorController generator;
    [SerializeField] protected PointCloudController pointCloud;

    protected const float movementThreshold = 0.2f;
    protected Vector2 movement, movementInput;
    protected bool hasMovementInput;

    protected Vector2 rotationInput, pitchYaw;

    [SerializeField] protected PlayerCamera playerCamera;
    [SerializeField] protected Character character;
    protected float horizontalSpeed, targetHorizontalSpeed;
    [SerializeField] protected MovementSettings movementSettings;

    [SerializeField] protected PlayerKeyInputEvent onKeyDown, onKeyUp;


    protected KeyCode[] keyCodes = new KeyCode[]
    {
        KeyCode.Escape,
        KeyCode.Return,
        KeyCode.KeypadEnter,
        KeyCode.Space,
        KeyCode.E,
        KeyCode.N,
    };

    #region MonoBehaviour

    protected void Awake()
    {
        var receivers = FindObjectsOfType<MonoBehaviour>().OfType<IPlayerResponsible>();
        foreach (var r in receivers)
            r.Player = character;
    }

    protected void Start()
    {
        movementInput = Vector3.zero;

        onKeyDown.AddListener((key) =>
        {
            switch (key)
            {
                case KeyCode.Space:
                    generator.AddCircle(character.transform.position);
                    break;
                case KeyCode.E:
                    generator.AddPlane(Vector3.back);
                    break;
                case KeyCode.KeypadEnter:
                case KeyCode.Return:
                    pointCloud.Next(10f);
                    break;
            }
        });
    }

    protected void Update()
    {
        var dt = Time.deltaTime;
        UpdateInput(dt);
        pitchYaw = UpdatePitchYaw(pitchYaw, rotationInput);
        character.PitchYaw = pitchYaw;

        foreach (var k in keyCodes)
        {
            if (Input.GetKeyDown(k))
                onKeyDown.Invoke(k);
            else if (Input.GetKeyUp(k))
                onKeyUp.Invoke(k);
        }
    }

    protected void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime;
        UpdateHorizontalSpeed(dt);

        var dir = GetMovementDirection(movementInput, pitchYaw);
        float rotationSpeed = Mathf.Lerp(movementSettings.maxRotationSpeed, movementSettings.minRotationSpeed, horizontalSpeed / targetHorizontalSpeed);
        character.Move(dir * horizontalSpeed + Vector3.up * movementSettings.gravity, rotationSpeed, dt);

        playerCamera.Move(character.transform.position, pitchYaw);
    }

    protected void OnGUI()
    {
        const int offset = 20;
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Space(offset);
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Space(offset);
                GUILabel("Enter", "Next point cloud");
                GUILabel("Space", "Pulse");
                GUILabel("E", "Scanline");
                GUILabel("WASD", "Movement");
                GUILabel("Arrow or Drag", "Rotation");
                GUILabel("Shift", "Dash");
            }
        }
    }

    protected void GUILabel(string key, string content)
    {
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(key, GUILayout.Width(100));
            GUILayout.Label(string.Format("- {0}", content));
        }
    }

    #endregion

    #region Control

    protected void UpdateInput(float dt)
    {
        movementInput = movement = UpdateMovementByKeyInput(movement, dt * 5f);

        if (Mathf.Abs(movementInput.x) < movementThreshold)
            movementInput.x = 0.0f;
        if (Mathf.Abs(movementInput.y) < movementThreshold)
            movementInput.y = 0.0f;

        hasMovementInput = movementInput.sqrMagnitude > 0.0f;
        rotationInput = UpdateRotationByArrowInput(rotationInput, dt * 2f);
    }

    protected Vector2 UpdateMovementByKeyInput(Vector2 current, float dt, float decay = 0.9f)
    {
        current.x = UpdateValue(current.x, dt, decay, Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A));
        current.y = UpdateValue(current.y, dt, decay, Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));
        return current;
    }

    protected Vector2 UpdateRotationByArrowInput(Vector2 current, float dt, float decay = 0.9f)
    {
        current.x = UpdateValue(current.x, dt, decay, Input.GetKey(KeyCode.RightArrow), Input.GetKey(KeyCode.LeftArrow));
        current.y = UpdateValue(current.y, dt, decay, Input.GetKey(KeyCode.UpArrow), Input.GetKey(KeyCode.DownArrow));

        if (Input.GetMouseButton(0))
        {
            var dx = Input.GetAxis("Mouse X");
            var dy = Input.GetAxis("Mouse Y");
            current.x += dx * 0.25f;
            current.y += dy * 0.25f;
        }

        return current;
    }

    protected float UpdateValue(float current, float delta, float decay, bool forward, bool backward)
    {
        if (forward)
        {
            current += delta;
            return Mathf.Min(current, 1f);
        }
        else if (backward)
        {
            current -= delta;
            return Mathf.Max(current, -1f);
        }
        return current * decay;
    }

    protected Vector2 UpdatePitchYaw(Vector2 pitchYaw, Vector2 rotationInput, float intensity = 2f)
    {
        float pitch = pitchYaw.x;
        pitch -= rotationInput.y * intensity;
        pitch = Mathf.Clamp(pitch, movementSettings.minPitchAngle, movementSettings.maxPitchAngle);

        float yaw = pitchYaw.y;
        yaw += rotationInput.x * intensity;

        return new Vector2(pitch, yaw);
    }

    protected void UpdateHorizontalSpeed(float dt)
    {
        var maxSpeed = movementSettings.GetMaxHorizontalSpeed(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        targetHorizontalSpeed = movementInput.magnitude * maxSpeed;
        float delta = hasMovementInput ? movementSettings.acceleration : movementSettings.decceleration;
        horizontalSpeed = Mathf.MoveTowards(horizontalSpeed, targetHorizontalSpeed, delta * dt);
    }

    protected Vector3 GetMovementDirection(Vector3 movementInput, Vector2 controlRotation)
    {
        Quaternion yawRotation = Quaternion.Euler(0.0f, controlRotation.y, 0.0f);
        Vector3 forward = yawRotation * Vector3.forward;
        Vector3 right = yawRotation * Vector3.right;

        Vector3 direction = (forward * movementInput.y + right * movementInput.x);
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();
        return direction;
    }

    #endregion

}
