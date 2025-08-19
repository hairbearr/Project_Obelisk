using UnityEngine;

public class PlayerGrapplingHookController : MonoBehaviour
{
    [SerializeField] private Attack grapple;
    private PlayerController player;
    private Animator animator;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire3") && grapple.IsReady())
        {
            grapple.lastUsedTime = Time.time;
            AnimationClip clip = grapple.GetAnimation(AnimationType.Grapple, player.Direction);
            if (clip != null) animator.Play(clip.name);
        }
    }
}