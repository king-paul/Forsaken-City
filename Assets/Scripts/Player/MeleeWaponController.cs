using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWaponController : MonoBehaviour
{
    [Header("Sprite Objects")]
    [SerializeField] SpriteRenderer weaponArm;
    [SerializeField] SpriteRenderer backArm;

    [Header("Positioning")]
    [SerializeField] Vector2 leftShoulderPosition;
    [SerializeField] Vector2 rightShoulderPosition;
    [SerializeField] Vector2 leftAttackBoxPosition;
    [SerializeField] Vector2 rightAttackBoxPosition;
    [SerializeField] Vector2 upperAttackBoxPosition;

    [Header("Weapon Stats")]
    [SerializeField] uint weaponDamage = 1;
    [SerializeField] Vector2 attackBoxSize = Vector2.one / 2;
    [SerializeField] Vector2 upperAttackBoxSize = Vector2.one / 2;

    [Header("Sound Effects")]
    [SerializeField] SoundEffect swingSound;
    [SerializeField] SoundEffect upwardSwingSound;
    [SerializeField] SoundEffect hitSound;

    SpriteRenderer playerSprite;    
    Animator animator;
    Animator playerAnimator;
    AudioSource playerAudio;

    private Vector2 attackDirection = Vector2.right;
    private Vector2 meleeCenterPoint;
    private bool upwardsAim;

    private void OnEnable()
    {
        meleeCenterPoint = transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        playerSprite = transform.root.GetComponent<SpriteRenderer>();

        animator = GetComponent<Animator>();
        playerAnimator = transform.root.GetComponent<Animator>();

        playerAudio = transform.root.GetComponent<AudioSource>();        
    }

    // Update is called once per frame
    void Update()
    { 
        UpdateMeleeDirection();
    }

    public void Attack()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if(upwardsAim)
            playerAnimator.SetTrigger("MeleeAttackUpwards");        
        else
            playerAnimator.SetTrigger("MeleeAttackHorizontal");

        if (swingSound.audioClip != null)
            playerAudio.PlayOneShot(swingSound.audioClip, swingSound.volume);
    }

    public void SetUpwardsAttack(bool upwards) 
    { 
        upwardsAim = upwards;
    }

    public void AttackUpwards()
    {
        if (upwardSwingSound.audioClip != null)
            playerAudio.PlayOneShot(upwardSwingSound.audioClip, upwardSwingSound.volume);
    }

    public void DealDamage()
    {
        bool playSound = false;

        Vector2 boxSize = (upwardsAim) ? upperAttackBoxSize : attackBoxSize;

        Collider2D[] collisions = Physics2D.OverlapBoxAll(meleeCenterPoint, boxSize, 0);

        foreach (Collider2D collision in collisions)
        {
            playSound = true;

            if (collision.gameObject.layer == 7) // enemy layer
            {
                EnemyController controller = collision.gameObject.GetComponent<EnemyController>();                

                if (controller.Alive)
                {
                    controller.TakeDamage(weaponDamage);
                    collision.gameObject.GetComponent<EnemyMovement>().Knockback(attackDirection);
                }
            }
        }

        if (playSound && hitSound.audioClip != null)
            playerAudio.PlayOneShot(hitSound.audioClip, hitSound.volume);
    }

    private void UpdateMeleeDirection()
    {
        if (upwardsAim)
        {
            meleeCenterPoint = (Vector2)transform.position + upperAttackBoxPosition;
        }
        else
        {
            if (playerSprite.flipX)
            {
                meleeCenterPoint.x = transform.position.x + leftAttackBoxPosition.x;
                attackDirection = Vector2.left;
            }
            else
            {
                meleeCenterPoint.x = transform.position.x + rightAttackBoxPosition.x;
                attackDirection = Vector2.right;
            }

            meleeCenterPoint.y = transform.position.y + leftAttackBoxPosition.y;
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube((Vector2)transform.position + rightAttackBoxPosition, attackBoxSize);
        Gizmos.DrawWireCube((Vector2)transform.position + leftAttackBoxPosition, attackBoxSize);
        Gizmos.DrawWireCube((Vector2)transform.position + upperAttackBoxPosition, upperAttackBoxSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(meleeCenterPoint, 0.1f);
    }
}