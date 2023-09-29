using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HangingEnemy : EnemyMovement
{
    enum EnemyState { Crawling, Launching, Dropping, Falling, Running, Dead, }

    [Header("Movement")]
    [Tooltip("speed when attached to ceiling")]
    [SerializeField] float crawlSpeed = 1;
    [Tooltip("Speed when on floor")]
    [SerializeField] float runSpeed = 2;
    [Tooltip("Speed when dropping down on top of player")]
    [SerializeField] float dropSpeed = 2;

    [Header("Ceiling Vision")]
    [SerializeField] Vector2 visionDirection = Vector2.down;
    [SerializeField] Vector2 visionOffset = Vector2.down;
    [SerializeField] float visionLength = 5;
    [SerializeField] float visionWidth = 1;

    private EnemyState state;

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        State = EnemyState.Crawling;        
    }

    // Update is called once per frame
    new void Update()
    {
        base.Update();

        switch (state)
        {
            case EnemyState.Crawling: CrawlCeiling();
                break;
            case EnemyState.Dropping: case EnemyState.Falling:
                break;
            case EnemyState.Running: RunAcrossFloor();
                break;
            case EnemyState.Dead:
                break;
        }

        if (!controller.Alive && state != EnemyState.Dead)
            State = EnemyState.Dead;
    }

    public void DropFromCeiling()
    {
        if(state  == EnemyState.Crawling)
            State = EnemyState.Falling;
    }

    EnemyState State
    {
        get => state;
        set 
        { 
            state = value;

            // perform state change actions
            switch (state)
            {
                case EnemyState.Crawling:
                    rb.gravityScale = 0;                   
                    sprite.flipY = true;
                    animator.SetTrigger("Walk");
                break;

                case EnemyState.Launching:
                    StopMoving();
                    animator.ResetTrigger("Walk");
                    animator.SetTrigger("Jump");

                    if (controller.attackSound.audioClip != null)
                        enemyAudio.PlayOneShot(controller.attackSound.audioClip, controller.attackSound.volume);
                    break;

                case EnemyState.Falling:
                    //sprite.flipY = false;
                    rb.velocity = Vector2.zero;
                    rb.gravityScale = dropSpeed;

                    animator.SetTrigger("Fall");

                break;

                case EnemyState.Dropping:
                    transform.Translate(moveDirection.x, -1, 0);
                    transform.Rotate(0, 0, 90);                   
                    rb.gravityScale = dropSpeed;                    
                break;

                case EnemyState.Running:
                    transform.rotation = Quaternion.identity;
                    sprite.flipY = false;
                    transform.Translate(Vector2.down / 2);
                    rb.gravityScale = 1;

                    if (player.transform.position.x > transform.position.x)
                    {
                        moveDirection = Vector2.right;
                        sprite.flipX = false;
                    }
                    else if (player.transform.position.x < transform.position.x)
                    {
                        moveDirection = Vector2.left;
                        sprite.flipX = true;
                    }

                    // update animation
                    animator.ResetTrigger("Stop");
                    animator.SetTrigger("Run");
                break;

                case EnemyState.Dead:
                    sprite.flipY = false;

                    if(!OnFloor())
                        rb.velocity = Vector2.zero;
                        rb.gravityScale = dropSpeed;
                break;
            }
        }
    }

    private void CrawlCeiling()
    {
        rb.velocity = moveDirection * crawlSpeed;

        if (moveDirection == Vector2.left)
        {
            if (NextToWall(Vector2.left) || !SolidTileAbove(Vector2.left))
            {
                moveDirection = Vector2.right;
                sprite.flipX = false;
            }
                
        }
        else if(moveDirection == Vector2.right)
        {
            if (NextToWall(Vector2.right) || !SolidTileAbove(Vector2.right))
            {
                moveDirection = Vector2.left;
                sprite.flipX = true;
            }
        }

        if(PlayerIsBelow())
        {           
            State = EnemyState.Launching;
        }
    }

    private void RunAcrossFloor()
    {
        rb.velocity = moveDirection * runSpeed;

        if (moveDirection == Vector2.left)
        {
            if (NextToWall(Vector2.left) || !SolidTileBelow(Vector2.left))
            {
                moveDirection = Vector2.right;
                sprite.flipX = false;
            }

        }
        else if (moveDirection == Vector2.right)
        {
            if (NextToWall(Vector2.right) || !SolidTileBelow(Vector2.right))
            {
                moveDirection = Vector2.left;
                sprite.flipX = true;
            }
        }
    }

    private bool PlayerIsBelow()
    {
        var hit = Physics2D.BoxCast((Vector2)transform.position + visionOffset, new Vector2(visionWidth, visionLength),
            Mathf.Atan2(visionDirection.y, visionDirection.x), Vector2.down, visionLength, playerLayer);

        if (hit.transform != null && hit.transform.tag == "Player")
        {
            player = hit.transform;
            return true;
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 3) // ground layer
        {
            if (state == EnemyState.Dropping || state == EnemyState.Falling)
            {
                State = EnemyState.Running;
            }
            else if(state == EnemyState.Dead)
            {
                GetComponent<BoxCollider2D>().enabled = false;
                rb.velocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Static;
                sprite.flipY = false;                
            }
        }
    }

    private void ResetSprite()
    {
        animator.ResetTrigger("Fall");
        animator.SetTrigger("Stop");
        transform.rotation = Quaternion.identity;
        transform.Translate(0, -1, 0);

        sprite.flipX = false;
        sprite.flipY = false;
    }


    private new void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.red;

        // draw ground dectection raycast
        Gizmos.DrawRay((Vector2)transform.position + Vector2.left * offsetFromEdge, Vector2.down * castingLength);
        Gizmos.DrawRay((Vector2)transform.position + Vector2.right * offsetFromEdge, Vector2.down * castingLength);

        // draw ceiling dectection raycast
        Gizmos.DrawRay((Vector2)transform.position + Vector2.left * offsetFromEdge, Vector2.up * castingLength);
        Gizmos.DrawRay((Vector2)transform.position + Vector2.right * offsetFromEdge, Vector2.up * castingLength);

        // Draw Vision Box
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(((Vector2)transform.position + visionOffset) + (Vector2.down * (visionLength / 2)),
                            new Vector2(visionWidth, visionLength));

        // draw wall raycast
        Gizmos.DrawRay((Vector2)transform.position + castDirection * raycastOffset, 
                       castDirection * raycastLength);
    }

}
