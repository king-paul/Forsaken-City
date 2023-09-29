using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingEnemy : EnemyMovement
{
    [Header("Movement")]
    //[Range(0, 10)]
    [SerializeField] float walkSpeed = 1;
    [SerializeField] bool followPlayer = true;   
    
    new void Start()
    {
        base.Start();
    }   
    
    new void Update()
    {
        base.Update();

        if (!controller.Alive)
        {
            StopMoving();
            GetComponent<CapsuleCollider2D>().enabled = false;
            return;
        }

        if (knockBack)
            return;

        if (followPlayer)
            FollowPlayer();

        if (moveDirection == Vector2.right)
            MoveRight();
        else
            MoveLeft();

        castDirection = rb.velocity.normalized;

        if (player.GetComponent<PlayerController>().IsAlive == false)
        {
            followPlayer = false;
        }
    }

    private void MoveLeft()
    {
        rb.velocity = Vector2.left * walkSpeed;// * Time.deltaTime;
        animator.SetBool("Walking", true);

        if (NextToWall(Vector2.left) || !SolidTileBelow(Vector2.left))
            moveDirection = Vector2.right;
    }

    private void MoveRight()
    {
        rb.velocity = Vector2.right * walkSpeed;// * Time.deltaTime;

        animator.SetBool("Walking", true);

        if (NextToWall(Vector2.right) || !SolidTileBelow(Vector2.right))
            moveDirection = Vector2.left;
    }

    private void FollowPlayer()
    {
        // distance check        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= controller.VisionRadius)
        {
            if (transform.position.x > player.position.x)
                moveDirection = Vector2.left;
            else if (transform.position.x < player.position.x)
                moveDirection = Vector2.right;
        }
    }   

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.gameObject.layer == 3) // ground layer
            moveDirection = -moveDirection;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 6) // player layer
        {           
            // check distance from wall
            var hit = Physics2D.Raycast((Vector2)transform.position + castDirection * raycastOffset, castDirection,
                                        raycastLength, wallLayer);

            if (hit.collider != null)
            {                              
                Knockback(-castDirection);

                collision.gameObject.GetComponent<PlayerMovement>().SetJumpState();
            }
        }
        
    }

    private new void OnDrawGizmos()
    {        
        base.OnDrawGizmos();

        Gizmos.color = Color.red;

        // draw ground raycast
        Gizmos.DrawRay((Vector2)transform.position + Vector2.left * offsetFromEdge, Vector2.down * castingLength);
        Gizmos.DrawRay((Vector2)transform.position + Vector2.right * offsetFromEdge, Vector2.down * castingLength);

        // draw wall raycast
        Gizmos.DrawRay((Vector2)transform.position + castDirection * raycastOffset, castDirection * raycastLength);
    }
}
