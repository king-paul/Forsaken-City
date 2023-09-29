using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemy : EnemyMovement
{
    enum FlyingState { Normal, Attack, Follow, Return, Retreat, Dead };

    [Header("Enemy Movement")]
    [SerializeField] float movementSpeed = 3;
    [SerializeField] float chaseSpeed = 3;
    [Tooltip("The maxmium horizontal distance the enemy will fly before changing direction when not following the player")]
    [SerializeField] float maxFlyDistance = 10;
    [SerializeField] bool startInCenterOfDistance = false;

    [Header("Player Detection")]
    [SerializeField] float visionRadius = 5f;
    [SerializeField] float raycastOffSetDistance = 1;
    [SerializeField] bool canSeeThroughWalls = false;

    [Header("Enemy Attacking")]
    [SerializeField] float attackRange = 3;
    [SerializeField] float chargeSpeed = 6;
    [SerializeField] float maxChargeDistance = 3;

    [HeaderAttribute("Enemy Retreating")]
    [SerializeField] float retreatSpeed = 6;

    private float distanceTravelled = 0;
    private Vector2 startPoint;
    private Vector2 attackPoint;

    [SerializeField]
    private FlyingState enemyState = FlyingState.Normal;

    FlyingState State
    {
        get => enemyState;
        set
        {
            enemyState = value;

            switch (enemyState)
            {
                case FlyingState.Normal:                   
                    moveDirection = (sprite.flipX) ? Vector2.right : Vector2.left;
                    animator.SetBool("Attacking", false);

                    sprite.flipY = false;
                    break;

                case FlyingState.Attack:
                    StopMoving();
                    LookAtPlayer();
                    moveDirection = (player.position - transform.position).normalized;
                    attackPoint = transform.position;
                    animator.SetBool("Attacking", true);

                    sprite.flipY = false;

                    if (controller.attackSound.audioClip != null)
                        enemyAudio.PlayOneShot(controller.attackSound.audioClip, controller.attackSound.volume);

                break;

                case FlyingState.Follow:
                    animator.SetBool("Attacking", false);
                    sprite.flipY = false;
                    break;

                case FlyingState.Return:
                    if (player.position.x < transform.position.x)
                        moveDirection = new Vector2(1, 1);
                    else
                        moveDirection = new Vector2(-1, 1);

                    sprite.flipX = moveDirection.x > 0;

                    animator.SetBool("Attacking", false);
                    //sprite.flipY = true;
               break;

               case FlyingState.Retreat:
                    moveDirection = (startPoint - (Vector2)transform.position).normalized;
                    sprite.flipX = moveDirection.x > 0;

                    animator.SetBool("Attacking", false);
               break;

               case FlyingState.Dead:
                    animator.SetTrigger("Die");
                    rb.velocity = Vector2.zero;
                    rb.gravityScale = 1;
               break;
            }
        }
    }

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();

        moveDirection = (sprite.flipX) ? Vector2.right : Vector2.left;
        startPoint = transform.position;

        if(startInCenterOfDistance)
        {
            distanceTravelled = maxFlyDistance / 2;
        }
    }

    // Update is called once per frame
    new void Update()
    {
        base.Update();

        if(!controller.Alive && State != FlyingState.Dead)
        {
            State = FlyingState.Dead;
        }

        switch(State)
        {
            case FlyingState.Normal: 
                FlyAcrossLevel();
                CheckForPlayer();
            break;

            case FlyingState.Attack: AttackPlayer();
                break;

            case FlyingState.Follow: FollowPlayer();
                break;

            case FlyingState.Return: FlyUp();
                break;

            case FlyingState.Retreat: 
                RetreatToStart();
                CheckForPlayer();
            break;
        }  
        
    }

    private void FlyAcrossLevel()
    {
        rb.velocity = moveDirection * movementSpeed;

        distanceTravelled = Vector2.Distance(transform.position, startPoint);

        if (NextToWall(moveDirection) || distanceTravelled > maxFlyDistance)
        {
            sprite.flipX = !sprite.flipX;
            moveDirection = -moveDirection;
            startPoint = transform.position;
        }
    }

    private void CheckForPlayer()
    {
        bool playerInRange = false;
        var collisions = Physics2D.OverlapCircleAll(transform.position, visionRadius);

        foreach (var collision in collisions)
        {
            if (collision.gameObject.tag == "Player")
            {                
                playerInRange = true;
            }
        }

        if (!canSeeThroughWalls)
        {
            if (!PlayerBehindObject(raycastOffSetDistance, visionRadius - raycastOffSetDistance) && playerInRange)
            {
                // check if player is in attack range
                if (Vector2.Distance(player.position, transform.position) <= attackRange)
                    State = FlyingState.Attack;
                else
                    State = FlyingState.Follow;
            }
        }
        else if(playerInRange)
        {
            // check if player is in attack range
            if (Vector2.Distance(player.position, transform.position) <= attackRange)
                State = FlyingState.Attack;
            else
                State = FlyingState.Follow;
        }
    }

    private void LookAtPlayer()
    {
        // check direction to player
        if (player.position.x > transform.position.x)
        {
            sprite.flipX = true;            
        }
        else
        {
            sprite.flipX = false;            
        }
    }

    private void AttackPlayer()
    {      
        rb.velocity = moveDirection * chargeSpeed;

        if (Vector2.Distance(attackPoint, transform.position) > maxChargeDistance)
        {
            State = FlyingState.Return;
        }        
    }

    private void FollowPlayer()
    {
        LookAtPlayer(); // rotates sprite to face player

        // follow player
        moveDirection = (player.position - transform.position).normalized;
        rb.velocity = moveDirection * chaseSpeed;

        if (!canSeeThroughWalls && PlayerBehindObject(raycastOffSetDistance, visionRadius - raycastOffSetDistance))
        {
            State = FlyingState.Retreat;
        }
        else if(Vector2.Distance(transform.position, player.transform.position) > visionRadius)
        {
            State = FlyingState.Retreat;
        }
        else if (Vector2.Distance(player.position, transform.position) <= attackRange)
        {
           State = FlyingState.Attack;
        }

    }

    private void FlyUp()
    {
        rb.velocity = moveDirection * retreatSpeed;

        if(transform.position.y >= startPoint.y)
            State = FlyingState.Follow;
    }

    private void RetreatToStart()
    {
        rb.velocity = moveDirection * movementSpeed;

        if (Vector2.Distance(startPoint, transform.position) <= 0.5f)
        {
            State = FlyingState.Normal;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (State == FlyingState.Attack)
            State = FlyingState.Return;
        else if (State == FlyingState.Return)
            State = FlyingState.Follow;
        else if (State == FlyingState.Dead && collision.gameObject.layer == 3)
        {
            animator.SetTrigger("HitGround");

            rb.bodyType = RigidbodyType2D.Static;
            GetComponent<BoxCollider2D>().enabled = false;            
        }
            
    }

    private new void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        player = GameObject.FindWithTag("Player").transform;

        if (player != null && State == FlyingState.Follow)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // draw top and bottom detection boxes
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireCube((Vector2)transform.position + topBoxOffset, topBoxSize);
        //Gizmos.DrawWireCube((Vector2)transform.position + bottomBoxOffset, bottomBoxSize);

        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(attackPoint, 0.5f);
    }

}