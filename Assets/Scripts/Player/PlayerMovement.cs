using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using UnityEngine.Windows;

[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    enum MovementType
    {
        Standing,
        Walking,
        SlopeWalk,
        Running,
        Jumping,
        Falling,
        Climbing,
        Grappling,
        Knockback
    }

    #region variable declaration
    [Header("Horizontal Movement")]
    [Tooltip("Top speed")]
    [SerializeField]//[Range(0f, 50f)]
    float walkSpeed = 10;
    [SerializeField]//[Range(0f, 10f)]
    [Tooltip("Horizontal movement acceleration")]
    float walkAcceleration = 5;
    [SerializeField]//[Range(0f, 10f)]
    [Tooltip("Horizontal movement decceleration")]
    float walkDeccelertaion = 5;
    [SerializeField]
    float minimumWalkSpeed = 0.5f;
    [Tooltip("Speed of horizontal movement when jumping or falling")]
    [SerializeField]//[Range(0f, 50f)]
    float inAirMoveSpeed = 10;
    [SerializeField]
    float slopeWalkSpeed = 10;

    [Header("Vertical Movement")]
    [Header("Jumping")]
    [Tooltip("Jumping force happens once")]
    [SerializeField]//[Range(0f, 20f)]
    float jumpForce = 10;

    [SerializeField]//[Range(0f, 5f)]
    [Tooltip("Multiplier for horizontal movement acceleration in air")]
    float jumpAcceleration = 1;
    [SerializeField]//[Range(0f, 5f)]
    [Tooltip("Multiplier for horizontal movement decceleration in air")]
    float jumpDecceleration = 1;
    [SerializeField]
    //[Range(0f, 5f)]
    [Tooltip("Adds a delay between each jump")]
    float coyoteTime;

    [Header("Gravity")]
    [Tooltip("Overall gravity multiplier")]
    [SerializeField]
    //[Range(0, 10)]
    float gravityMultiplier = 1;
    [SerializeField]
    //[Range(0f, 5f)]
    [Tooltip("Gravity multiplier while jumping and holding jump")]
    float jumpGravityJumpHeld = 1.2f;
    [SerializeField]
    //[Range(0f, 5f)]
    [Tooltip("Gravity multiplier while jumping and not holding jump")]
    float jumpGravity = 3;
    [SerializeField]
    //[Range(0f, 5f)]
    [Tooltip("Gravity multiplier while falling and holding jump")]
    float fallingGravityJumpHeld = 1.8f;
    [SerializeField]
    //[Range(0f, 5f)]
    [Tooltip("Gravity multiplier while falling and not holding jump")]
    float fallingGravity = 4;
    [SerializeField]//[Range(0, 5)]
    float maximumFallSpeed;
    
    [Header("Hang Times")]
    [Tooltip("Vertical velocity threshold at apex of jump to toggle better manouverability")]
    [SerializeField]//[Range(0f, 5f)]
    float hangTimeThreshold = 0.1f;
    [Tooltip("Extra acceleration at apex")]
    [SerializeField]//[Range(0f, 5f)]    
    float hangTimeAccelerationMult = 1.1f;    
    [Tooltip("Extra top speed at apex")]
    [SerializeField]//[Range(0f, 5f)]
    float hangTimeSpeedMult = 1.1f;

    [Header("Damage Knockback")]
    [Tooltip("Increaseed force will knock the player back further and at a faster speed")]
    //[Range(1, 100)]
    [SerializeField] float knockbackForce = 10;
    [Tooltip("The velocity that the player needs to slow down to before re-enabling movement")]
    //[Range(0, 1)]
    [SerializeField] float minKnockbackVelocity = 0.5f;    
    [SerializeField] float knockbackDecelleration = 0.1f;
    [SerializeField] float maxKnockbackTime = 1f;

    [Header("Ground Checks")]
    public LayerMask groundLayer;
    [SerializeField] private Vector2 leftGroundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private Vector2 leftGroundCheckPoint;
    [SerializeField] private Vector2 rightGroundCheckSize = new Vector2(0.49f, 0.03f);
    [SerializeField] private Vector2 rightGroundCheckPoint;

    [Header("Slope Detection")]
    [SerializeField] float slopeCheckDistance;
    [SerializeField] float maxSlopeAngle;

    [Header("Grappling")]
    [SerializeField] GameObject grapplingGun;
    [SerializeField] LineRenderer ropeRenderer;

    [Header("Animators")]
    //[SerializeField] Animator meleeAnimator;
    [SerializeField] Animator shotgunAnimator;
    [SerializeField] Animator leftArmAnimator;

    [Header("Sound Effects")]
    [SerializeField] SoundEffect jumpSound;
    [SerializeField] SoundEffect landSound;

    [Header("Physics Materials")]
    [SerializeField] PhysicsMaterial2D normalMaterial;
    [SerializeField] PhysicsMaterial2D slopeMaterial;

    [Header("Debugging")]
    [SerializeField] Vector2 velocity;
    #endregion

    #region private variables
    //private variables
    Transform aimPivot;

    // components
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private Animator animator;
    private AudioSource playerAudio;
    private InputHandler input;

    //input
    private float inputX, inputY;
    private bool jumpHeld = false;

    //timers
    [SerializeField]
    private float lastOnGroundTime;
    private float knockbackTimer = 0;

    // movement
    [SerializeField]
    private bool onGround;
    private float accelerationRate;
    private float targetSpeed;
    private bool minWalkSpeedReached = false;

    // used for slopes
    [SerializeField]
    private bool onSlope;
    private Vector2 slopeNormalPerp;
    private Vector2 slopeCheckPos;
    private float slopeDownAngle;
    private float slopeSideAngle;
    private float lastSlopeAngle;
    private bool canWalkOnSlope;
    private Vector2 targetVelocity;

    [SerializeField]
    private MovementType state;
    #endregion

    #region public functions and properties
    // properties
    public bool OnGround => lastOnGroundTime >= 0;

    public Vector2 FaceDirection
    {
        get
        {
            if (sprite.flipX)
                return Vector2.left;
            else
                return Vector2.right;
        }
    }

    // functions
    public void SetJumpState() { ChangeState(MovementType.Jumping); }

    public void SetGrappleState() { ChangeState(MovementType.Grappling); }

    public void StopMoving()
    {
        body.velocity = Vector2.zero;
        ChangeState(MovementType.Standing);
    }

    public void Knockback(Vector2 direction)
    {
        body.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
        ChangeState(MovementType.Knockback);        
    }

    public void ReleaseGrapplingHook()
    {
        if (!grapplingGun.activeInHierarchy && state != MovementType.Grappling)
            return;

        if(state == MovementType.Grappling || state == MovementType.Jumping)
        {
            grapplingGun.GetComponent<GrapplingGun>().ReleaseGrapple();

            if(state == MovementType.Grappling)
                ChangeState(MovementType.Jumping);
        }
    }
    #endregion

    #region Unity built-in functions
    void OnEnable()
    {
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerAudio = GetComponent<AudioSource>();
        input = GetComponent<InputHandler>();

        aimPivot = GetComponent<PlayerController>().aimPivot;

        state = MovementType.Standing;

        body.gravityScale = gravityMultiplier;
    }

    // Update is called once per frame
    void Update()
    {
        lastOnGroundTime -= Time.deltaTime;

        jumpHeld = input.JumpPressed;
        inputX = input.HorizontalInput;
        inputY = input.VerticalInput;

        GroundCheck();
        SlopeCheck();

        // check if player should switch to slope walking state
        if (inputX != 0 && onGround && onSlope && state != MovementType.SlopeWalk)
        {
            ChangeState(MovementType.SlopeWalk);
        }
        // check if player should switch to walking state
        else if (inputX != 0 && onGround && !onSlope
            && state != MovementType.Walking && state != MovementType.Knockback)
        {
            if (state == MovementType.Falling)
                playerAudio.PlayOneShot(landSound.audioClip, landSound.volume);

            ChangeState(MovementType.Walking);           
        }
                

        switch (state)
        {
            case MovementType.Standing:
                body.velocity = new Vector2(0, body.velocity.y); // prevents player from sliding backwards

                if (!OnGround)
                    ChangeState(MovementType.Falling);
                break;
            case MovementType.Walking: UpdateWalk(); break;
            case MovementType.Jumping: UpdateJump(); break;
            case MovementType.Falling: UpdateFall(); break;
            case MovementType.Knockback: UpdateKnockback(); break;
            case MovementType.SlopeWalk: UpdateWalk(); break;
            case MovementType.Grappling: break;
        }

        // switch animation if falling
        animator.SetFloat("yVelocity", body.velocity.y);
        animator.SetBool("onGround", OnGround);

        velocity = body.velocity;
    }    

    private void LateUpdate()
    {
        // update the face direction
        if (inputX > 0 && body.velocity.x > 0)
            sprite.flipX = false;

        if (inputX < 0 && body.velocity.x < 0)
            sprite.flipX = true;
    }

    private void OnDisable()
    {
        body.velocity = Vector2.zero;
        body.gravityScale = 0;
        animator.SetBool("walking", false);
    }
    #endregion

    #region private functions

    #region slope detection
    private void SlopeCheck()
    {
        Vector2 capsuleColliderSize = GetComponent<CapsuleCollider2D>().size;

        slopeCheckPos = transform.position - (Vector3)(new Vector2(0.0f, capsuleColliderSize.y / 2));

        SlopeCheckHorizontal(slopeCheckPos);
        SlopeCheckVertical(slopeCheckPos);
    }

    // checks if there is a slope in front of the player
    private void SlopeCheckHorizontal(Vector2 checkPos)
    {
        RaycastHit2D slopeHitFront = Physics2D.Raycast(checkPos, transform.right, slopeCheckDistance, groundLayer);
        RaycastHit2D slopeHitBack = Physics2D.Raycast(checkPos, -transform.right, slopeCheckDistance, groundLayer);

        if (slopeHitFront)
        {
            onSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitFront.normal, Vector2.up);

        }
        else if (slopeHitBack)
        {
            onSlope = true;

            slopeSideAngle = Vector2.Angle(slopeHitBack.normal, Vector2.up);
        }
        else
        {
            slopeSideAngle = 0.0f;
            onSlope = false;
        }

    }

    // checks if there is a slope below the player
    private void SlopeCheckVertical(Vector2 checkPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, groundLayer);

        if (hit)
        {

            slopeNormalPerp = Vector2.Perpendicular(hit.normal).normalized;

            slopeDownAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeDownAngle != lastSlopeAngle)
            {
                onSlope = true;
            }

            lastSlopeAngle = slopeDownAngle;

            Debug.DrawRay(hit.point, slopeNormalPerp, Color.blue);
            Debug.DrawRay(hit.point, hit.normal, Color.green);

        }

        if (slopeDownAngle > maxSlopeAngle || slopeSideAngle > maxSlopeAngle)
        {
            canWalkOnSlope = false;
        }
        else
        {
            canWalkOnSlope = true;
        }

        if (onSlope && canWalkOnSlope && inputX == 0.0f)
        {
            body.sharedMaterial = slopeMaterial;
        }
        else
        {
            body.sharedMaterial = normalMaterial;

        }
    }
    #endregion

    private void ChangeState(MovementType newState)
    {
       if (state == newState)
            return;

        state = newState;

        // send flipped state to animator controller
        animator.SetBool("flipped", sprite.flipX);

        switch (state)
        {
            case MovementType.Standing:
                animator.SetBool("walking", false);
                minWalkSpeedReached = false;
            break;

            case MovementType.Walking:
            case MovementType.SlopeWalk:
                animator.SetBool("walking", true);

                if (inputX < 0)
                    aimPivot.rotation = Quaternion.Euler(0, 0, 180);

                else if (inputX > 0)
                    aimPivot.rotation = Quaternion.identity;
            break;

            case MovementType.Jumping:
                StartJump();
                break;

            case MovementType.Knockback:                
                knockbackTimer = maxKnockbackTime;
                break;

            case MovementType.Falling:
                animator.SetTrigger("fall");
                break;
        }

        if (shotgunAnimator.gameObject.activeInHierarchy)
            shotgunAnimator.SetBool("walking", state == MovementType.Walking);

        if (leftArmAnimator.gameObject.activeInHierarchy)
            leftArmAnimator.SetBool("walking", state == MovementType.Walking);
    }

    private void UpdateWalk()
    {
        if (state == MovementType.SlopeWalk)
        {
            body.velocity = new Vector2(slopeWalkSpeed * slopeNormalPerp.x * -inputX, slopeWalkSpeed * slopeNormalPerp.y * -inputX);
            
            /*
            targetVelocity = new Vector2(slopeWalkSpeed * slopeNormalPerp.x * -inputX, slopeWalkSpeed * slopeNormalPerp.y * -inputX);

            accelerationRate = (Mathf.Abs(targetVelocity.x) > 0.01f) ? walkAcceleration : walkDeccelertaion;

            Vector2 speedDif = targetVelocity - body.velocity;
            Vector2 movement = speedDif * accelerationRate;

            body.AddForce(movement, ForceMode2D.Force);*/

            //Debug.Log("Movement:" + movement);  
            
        }
        else if (state == MovementType.Walking)
        {
            targetSpeed = inputX * walkSpeed;

            // We can reduce air control using Lerp() this smooths changes to the direction and speed
            targetSpeed = Mathf.Lerp(body.velocity.x, targetSpeed, 1);

            accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) ? walkAcceleration : walkDeccelertaion;             

            // Calculate difference between current velocity and desired velocity
            float speedDif = targetSpeed - body.velocity.x;
            // Calculate force along x-axis to apply to the player
            float movement = speedDif * accelerationRate;

            // Convert this to a vector and apply to rigidbody
            body.AddForce(movement * Vector2.right, ForceMode2D.Force);
        }

        // Update player state
        if (Mathf.Abs(body.velocity.x) >= minimumWalkSpeed)
            minWalkSpeedReached = true;

        if (minWalkSpeedReached)
        {
            if (OnGround && Mathf.Abs(body.velocity.x) <= minimumWalkSpeed && state != MovementType.Grappling &&
                inputX < 1 && inputX > -1)
                ChangeState(MovementType.Standing);
        }

        if (!OnGround && body.velocity.y < 0)
        {
            ChangeState(MovementType.Falling);
        }
        
    }

    private void StartJump()
    {
        if (lastOnGroundTime >= 0)
        {
            lastOnGroundTime = 0;

            body.gravityScale = gravityMultiplier * jumpGravityJumpHeld;
            animator.SetTrigger("jump");

            if (jumpSound.audioClip != null)
                playerAudio.PlayOneShot(jumpSound.audioClip, jumpSound.volume);

            body.AddForce(Vector2.up * (jumpForce - body.velocity.y), ForceMode2D.Impulse);
        }
    }

    private void UpdateJump()
    {
        if (body.velocity.y <= 0)
        {
            ChangeState(MovementType.Falling);
        }

        // higher gravity if let go of jump
        if (!input.JumpPressed)
        {
            body.gravityScale = gravityMultiplier * jumpGravity;
        }

        ApplyHorizontalAirMovement();
    }

    private void UpdateFall()
    {
        if (!OnGround)
        {
            if (jumpHeld)
            {
                body.gravityScale = gravityMultiplier * fallingGravityJumpHeld;
            }
            else
            {
                body.gravityScale = gravityMultiplier * fallingGravity;
            }

            body.velocity = new Vector2(body.velocity.x, Mathf.Clamp(body.velocity.y, -maximumFallSpeed, 0));

            ApplyHorizontalAirMovement();
        }
        else
        {
            ChangeState(MovementType.Standing);
            playerAudio.PlayOneShot(landSound.audioClip, landSound.volume);
        }

    }

    private void ApplyHorizontalAirMovement()
    {
        targetSpeed = inputX * inAirMoveSpeed;

        accelerationRate = (Mathf.Abs(targetSpeed) > 0.01f) ? walkAcceleration * jumpAcceleration :
                walkDeccelertaion * jumpDecceleration;

        if (Mathf.Abs(body.velocity.y) < hangTimeThreshold)
        {
            accelerationRate *= hangTimeAccelerationMult;
            targetSpeed *= hangTimeSpeedMult;
        }

        // Calculate difference between current velocity and desired velocity
        float speedDif = targetSpeed - body.velocity.x;
        // Calculate force along x-axis to apply to thr player
        float movement = speedDif * accelerationRate;

        // Convert this to a vector and apply to rigidbody
        body.AddForce(movement * Vector2.right, ForceMode2D.Force);

    }

    private void UpdateKnockback()
    {
        knockbackTimer -= Time.deltaTime;

        body.velocity = new Vector2(body.velocity.x / (1+knockbackDecelleration), body.velocity.y);

        if (Mathf.Abs(body.velocity.x) < minKnockbackVelocity)
            ChangeState(MovementType.Standing);

        if (knockbackTimer <= 0)
            ChangeState(MovementType.Standing);
    }

    private void GroundCheck()
    {
        var leftCheck = Physics2D.OverlapBox((Vector2)transform.position + leftGroundCheckPoint, leftGroundCheckSize, 0, groundLayer);           
        var rightCheck = Physics2D.OverlapBox((Vector2)transform.position + rightGroundCheckPoint, rightGroundCheckSize, 0, groundLayer);

        onGround = (leftCheck || rightCheck) && state != MovementType.Jumping;

        if (onGround)
        {
            lastOnGroundTime = coyoteTime;            

            if (state == MovementType.Jumping)
                state = MovementType.Standing;
        }
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(state == MovementType.Grappling)
        {
            ropeRenderer.enabled = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + leftGroundCheckPoint, leftGroundCheckSize);
        Gizmos.DrawWireCube((Vector2)transform.position + rightGroundCheckPoint, rightGroundCheckSize);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(slopeCheckPos, transform.right * slopeCheckDistance);
        Gizmos.DrawRay(slopeCheckPos, -transform.right * slopeCheckDistance);
        Gizmos.DrawRay(slopeCheckPos, -transform.right * slopeCheckDistance);
        Gizmos.DrawRay(slopeCheckPos, Vector2.down * slopeCheckDistance);
    }
    #endregion

}