using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyController))]
public abstract class EnemyMovement : MonoBehaviour
{
    #region variable declaration   
    [Header("Platform Edge Detection")]
    [Range(0, 2)]
    [SerializeField] protected float offsetFromEdge = 0.5f;
    [Range(0f, 2f)]
    [SerializeField] protected float castingLength = 1.4f;

    [Header("Wall Dectection")]
    [SerializeField] protected Vector2 leftBoxOffset = Vector2.one;
    [SerializeField] protected Vector2 leftBoxSize = new Vector2(0.1f, 0.1f);
    [SerializeField] protected Vector2 rightBoxOffset = -Vector2.one;
    [SerializeField] protected Vector2 rightBoxSize = new Vector2(0.1f, 0.1f);
    [SerializeField] protected LayerMask wallLayer;

    [Header("Ground and ceiling detection")]
    [SerializeField] protected Vector2 topBoxOffset = Vector2.up;
    [SerializeField] protected Vector2 topBoxSize = Vector2.one;
    [SerializeField] protected Vector2 bottomBoxOffset = Vector2.down;
    [SerializeField] protected Vector2 bottomBoxSize = Vector2.one;
    [SerializeField] LayerMask groundLayer;

    [Header("Knockback from Wall")]
    [SerializeField] protected float raycastLength = 1;
    [SerializeField] protected float raycastOffset = 0.5f;
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] protected float knockBackForce = 20f;
    [SerializeField] protected float knockBackTime = 0.25f;

    // inherited variables
    protected Transform player;
    protected SpriteRenderer sprite;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected AudioSource enemyAudio;
    protected EnemyController controller;
    protected bool knockBack = false;
    protected Vector2 moveDirection = Vector2.left;
    protected Vector2 castDirection;

    private float timeSinceKnockback = 0;
    #endregion

    // public functions
    public void Knockback(Vector2 direction)
    {
        knockBack = true;
        StopMoving();

        if (rb != null)
        {
            rb.AddForce(direction * knockBackForce, ForceMode2D.Impulse);
        }
    }


    // Start is called before the first frame update
    protected void Start()
    {        
        try
        {
            player = GameObject.FindWithTag("Player").transform;
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }

        rb = GetComponent<Rigidbody2D>();

        animator = GetComponent<Animator>();
        controller = GetComponent<EnemyController>();
        sprite = GetComponent<SpriteRenderer>();
        enemyAudio = GetComponent<AudioSource>();

        moveDirection = (sprite.flipX) ? Vector2.left : Vector2.right;
    }

    protected void Update()
    {
        if (knockBack)
        {
            timeSinceKnockback += Time.deltaTime;

            if (timeSinceKnockback >= knockBackTime)
            {
                rb.velocity = Vector2.zero;
                timeSinceKnockback = 0;
                knockBack = false;
            }
            else
                return;
        }

        // if the enemy is on the ground when dead
        // remove the collider and stop it from moving
        if(!controller.Alive && OnFloor())
        {
            if(GetComponent<Collider2D>() != null) 
            {
                GetComponent<Collider2D>().enabled = false;
            }

            rb.bodyType = RigidbodyType2D.Static;
        }

    }

    #region protected and private functions
    protected void StopMoving()
    {
        rb.velocity = Vector2.zero;       
    }

    protected bool NextToWall(Vector2 direction)
    {
        if (direction.x < 0)
        {
            var collision = Physics2D.OverlapBox((Vector2)transform.position + leftBoxOffset, leftBoxSize, 0, wallLayer);

            if (collision)
                return true;
        }
        else if (direction.x > 0)
        {
            var collision = Physics2D.OverlapBox((Vector2)transform.position + rightBoxOffset, rightBoxSize, 0, wallLayer);

            if (collision)
                return true;
        }

        return false;
    }

    // Checks if there is a solid tile below to the left or right of the enemy
    protected bool SolidTileBelow(Vector2 direction)
    {
        var raycast = Physics2D.Raycast((Vector2)transform.position + direction * offsetFromEdge, Vector2.down, castingLength);

        if (raycast)
        {
            return true;
        }

        return false;
    }

    // Checks if there is a solid tile below to the left or right of the enemy
    protected bool SolidTileAbove(Vector2 direction)
    {
        var raycast = Physics2D.Raycast((Vector2)transform.position + direction * offsetFromEdge, Vector2.up, castingLength);

        if (raycast)
        {
            return true;
        }

        return false;
    }

    protected bool PlayerBehindObject(float offsetDistance, float maxDistance)
    {
        Vector2 checkDirection = (player.position - transform.position).normalized;
        Vector2 checkStartPosition = (Vector2)transform.position + checkDirection * offsetDistance;
        
        var hit = Physics2D.Raycast(checkStartPosition, checkDirection, maxDistance);

        if(hit.collider != null)
        {
            Debug.DrawLine(checkStartPosition, hit.transform.position, Color.green);

            if (hit.collider.gameObject != this.gameObject & hit.collider.gameObject.layer != 6 
                && hit.transform.tag != "Player")
                return true;
        }
        else
        {
            Debug.DrawRay(checkStartPosition, checkDirection * maxDistance, Color.green);
        }

        return false;
    }

    protected bool OnFloor()
    {
        var collision = Physics2D.OverlapBox((Vector2)transform.position + bottomBoxOffset, bottomBoxSize, 0, groundLayer);

        if (collision && collision.transform != transform)
            return true;

        return false;
    }

    protected bool OnCeiling()
    {
        var collision = Physics2D.OverlapBox((Vector2)transform.position + topBoxOffset, topBoxSize, 0, groundLayer);

        if (collision && collision.transform != transform)
            return true;

        return false;
    }

    protected bool HitPlayer()
    {
        // check for collisions in all four directions
        var collisionLeft = Physics2D.OverlapBox((Vector2)transform.position + leftBoxOffset, leftBoxSize, 0);
        var collisionRight = Physics2D.OverlapBox((Vector2)transform.position + rightBoxOffset, rightBoxSize, 0);
        var collisionAbove = Physics2D.OverlapBox((Vector2)transform.position + topBoxOffset, topBoxSize, 0);
        var collisionBelow = Physics2D.OverlapBox((Vector2)transform.position + bottomBoxOffset, bottomBoxSize, 0);

        // check if any box touches the player
        if ((collisionLeft != null && collisionLeft.transform == player) ||
            (collisionRight != null && collisionRight.transform == player) ||
            (collisionAbove != null && collisionAbove.transform == player) ||
            (collisionBelow != null && collisionBelow.transform == player))
        {
            return true;
        }

        return false;
    }

    protected void OnDrawGizmos()
    {       
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + leftBoxOffset, leftBoxSize);
        Gizmos.DrawWireCube((Vector2)transform.position + rightBoxOffset, rightBoxSize);
        Gizmos.DrawWireCube((Vector2)transform.position + topBoxOffset, topBoxSize);
        Gizmos.DrawWireCube((Vector2)transform.position + bottomBoxOffset, bottomBoxSize);
    }
    #endregion

}
