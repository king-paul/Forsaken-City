using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(AudioSource))]
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] int maxHealth = 5;
    [SerializeField] float visionRadius = 5.0f;
    [SerializeField] Color damageColor = Color.red;
    [SerializeField] uint damageDealt = 1;

    [Header("Sound Effects")]
    public SoundEffect attackSound;
    public SoundEffect damagedSound;
    public SoundEffect dieSound;

    public UnityEvent OnTakeDamage;

    protected int health;    

    protected AudioSource enemyAudio;
    protected Animator animator;
    private GameManager gameManager;

    public uint DamagerPerHit => damageDealt;
    public float VisionRadius => visionRadius;
    public bool Alive { get; private set; } = true;

    protected void Start()
    {
        health = maxHealth;

        enemyAudio = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        gameManager = GameManager.Instance;        
    }

    protected void Die()
    {
        // disable colliders
        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        // disable scripts
        GetComponent<EnemyMovement>().enabled = false;
        this.enabled = false;
    }

    public void TakeDamage(uint amount)
    {
        health -= (int)amount;

        if (health <= 0)
        {
            gameManager.PlaySound(damagedSound.audioClip, damagedSound.volume);
            animator.SetTrigger("Die");
            transform.rotation = Quaternion.identity;

            // disable collision with player
            if (GetComponent<BoxCollider2D>() != null)
                Physics2D.IgnoreCollision(GetComponent<BoxCollider2D>(), GameObject.FindWithTag("Player").GetComponent<CapsuleCollider2D>());

            Alive = false;
        }
        else
        {
            StartCoroutine(FlashDamage());

            if (damagedSound.audioClip != null)
                enemyAudio.PlayOneShot(damagedSound.audioClip, damagedSound.volume);

            OnTakeDamage.Invoke();
        }

    }

    IEnumerator FlashDamage()
    {
        GetComponent<SpriteRenderer>().color = damageColor;
        yield return new WaitForSeconds(0.1f);
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    private void OnDrawGizmos()
    {
        // Note: These seems to be bug where the enemy pushes walls backwards
        // while this gizmo is turned on
        Gizmos.DrawWireSphere(transform.position, visionRadius);        
    }

}