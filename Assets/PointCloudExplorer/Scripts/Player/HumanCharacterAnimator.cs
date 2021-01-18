using UnityEngine;

[RequireComponent (typeof(Animator), typeof(Character))]
public class HumanCharacterAnimator : MonoBehaviour
{
    protected Animator animator;
    protected Character character;
    protected readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        character = GetComponent<Character>();
    }

    protected void FixedUpdate()
    {
        UpdateState();
    }

    protected void UpdateState()
    {
        var v = character.Velocity;
        animator.SetFloat(HorizontalSpeed, (new Vector3(v.x, 0f, v.z)).magnitude);
    }

}

