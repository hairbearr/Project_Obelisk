using UnityEngine;

public class PlayerShieldController : MonoBehaviour
{
    [SerializeField] private Attack block;
    private PlayerController player;
    private Animator animator;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetButton("Fire2") && block.IsReady())
        {
            block.lastUsedTime = Time.time;
            AnimationClip clip = block.GetAnimation(AnimationType.Block, player.Direction);
            if (clip != null) animator.Play(clip.name);
        }
    }
}
