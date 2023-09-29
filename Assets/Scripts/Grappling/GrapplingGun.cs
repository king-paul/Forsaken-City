/** Reference: https://bitbucket.org/Vespper/grappling-hook/src/master/ **/

using UnityEngine;
using UnityEngine.Events;

public class GrapplingGun : MonoBehaviour
{
    #region public & exposed variables
    [Header("Scripts:")]
    public GrappleRope grappleRope;
    [Header("Layer Settings:")]
    [SerializeField] private bool grappleToAll = false;
    [SerializeField] private int grappableLayerNumber = 9;

    //[Header("Main Camera")]
    Camera m_camera;

    [Header("Transform Refrences:")]
    Transform gunHolder;
    Transform gunPivot;
    public Transform firePoint;

    [Header("Rotation:")]
    [SerializeField] private bool rotateOverTime = true;
    //[Range(0, 80)]
    [SerializeField] private float rotationSpeed = 4;
    [SerializeField] private bool rotateUsingDrag = true;

    [Header("Distance:")]
    [SerializeField] private bool hasMaxDistance = true;
    [SerializeField] private float maxDistance = 4;

    [Header("Launching")]
    [SerializeField] private bool launchToPoint = true;
    [SerializeField] private LaunchType Launch_Type = LaunchType.Transform_Launch;
    //[Range(0, 5)]
    [SerializeField] private float launchSpeed = 5;

    [Header("No Launch To Point")]
    [SerializeField] private bool autoCongifureDistance = false;
    [SerializeField] private float targetDistance = 3;
    [SerializeField] private float targetFrequency = 3;

    [Header("Positioning")]
    [SerializeField] float flippedGunOffset = 0.2f;
    [SerializeField] float flippedfirePointOffset = 0.25f;

    [Header("Events")]
    public UnityEvent onGrappleShotFired;
    #endregion

    private enum LaunchType
    {
        Transform_Launch,
        Physics_Launch,
    }

    #region private variables
    //[Header("Component Refrences:")]
    SpringJoint2D m_springJoint2D;
    new Rigidbody2D rigidbody;
    
    Vector2 Mouse_FirePoint_DistanceVector;

    [Header("Sound Effects")]
    [SerializeField] SoundEffect fireSound;
    [SerializeField] SoundEffect hitObjectSound;
    private AudioSource playerAudio;

    // sprites
    private SpriteRenderer playerSprite;
    private SpriteRenderer grapplingSprite;

    // animators
    private Animator grapplingAnimator;

    // positioning
    private Vector2 initialGunPosition;
    private Vector2 intialFiringPointPos;
    private Vector2 grappleCastPoint;

    private Vector2 aimDirection = Vector2.right;

    // external scripts
    private PlayerController player;
    #endregion

    // public variables and properties
    [HideInInspector] public Vector2 grapplePoint;
    [HideInInspector] public Vector2 DistanceVector;

    public bool HitObject { get; private set; } = false;

    #region functions
    private void Awake()
    {
        grappleRope.enabled = false;        
        //rigidbody.gravityScale = 1;

        gunPivot = transform.parent;
        gunHolder = transform.root;

        m_springJoint2D = transform.root.GetComponent<SpringJoint2D>();
        rigidbody = transform.root.GetComponent<Rigidbody2D>();
        playerAudio = transform.root.GetComponent<AudioSource>();
        playerSprite = transform.root.GetComponent<SpriteRenderer>();
        grapplingSprite = GetComponent<SpriteRenderer>();
        player = transform.root.GetComponent<PlayerController>();
        grapplingAnimator = GetComponent<Animator>();

        m_springJoint2D.enabled = false;

        m_camera = Camera.main;   
    }

    private void Start()
    {
        initialGunPosition = transform.localPosition;
        intialFiringPointPos = firePoint.localPosition;
    }

    private void Update()
    {   
        // Update spite according whether player is flipped
        if(playerSprite.flipX)
        {
            grapplingSprite.sortingOrder = -1;
            firePoint.localPosition = intialFiringPointPos + (Vector2.left * flippedfirePointOffset);
            transform.localPosition = initialGunPosition + (Vector2.left * flippedGunOffset);
        }
        else
        {
            grapplingSprite.flipX = false;
            grapplingSprite.sortingOrder = 1;
            transform.localPosition = initialGunPosition;
            firePoint.localPosition = intialFiringPointPos;
        }

        Mouse_FirePoint_DistanceVector = m_camera.ScreenToWorldPoint(Input.mousePosition) - gunPivot.position;
 
        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (grappleRope.enabled)
            {
                RotateGun(grapplePoint, false);
            }
            if (!grappleRope.enabled)
            {
                RotateGun(m_camera.ScreenToWorldPoint(Input.mousePosition), false);
            }

            if (launchToPoint && grappleRope.isGrappling)
            {
                if (Launch_Type == LaunchType.Transform_Launch)
                {
                    gunHolder.position = Vector3.Lerp(gunHolder.position, grapplePoint, Time.deltaTime * launchSpeed);
                }
            }

        }
        else
        {
            if(rotateUsingDrag)
                aimDirection = player.UpdateAim(out grappleCastPoint);
            else
                RotateGun(m_camera.ScreenToWorldPoint(Input.mousePosition), true);
        }

    }

    private void RotateGun(Vector3 lookPoint, bool allowRotationOverTime)
    {
        Vector3 distanceVector = lookPoint - gunPivot.position;

        float angle = Mathf.Atan2(distanceVector.y, distanceVector.x) * Mathf.Rad2Deg;
        if (rotateOverTime && allowRotationOverTime)
        {
            Quaternion startRotation = gunPivot.rotation;
            gunPivot.rotation = Quaternion.Lerp(startRotation, Quaternion.AngleAxis(angle, Vector3.forward), 
                Time.deltaTime * rotationSpeed);
        }
        else
            gunPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public void SetGrapplePoint()
    {
        ReleaseGrapple();

        if (!rotateUsingDrag)
            aimDirection = Mouse_FirePoint_DistanceVector.normalized;

        RaycastHit2D hit;

        if (hasMaxDistance)
            hit = Physics2D.Raycast(firePoint.position, aimDirection, maxDistance);
        else
            hit = Physics2D.Raycast(firePoint.position, aimDirection);

        if (hit.collider == null) // the rope does not hit anything
        {
            if (hasMaxDistance)
            {       
                grapplePoint = (Vector2)firePoint.position + aimDirection * maxDistance;
                DistanceVector = (Vector2)gunPivot.position - grapplePoint;
            }

            HitObject = false;            
        }
        // the rope hits a grapable object
        else if (hit.transform.gameObject.layer == grappableLayerNumber || grappleToAll)
        {         
            grapplePoint = hit.point;
            DistanceVector = (Vector2)gunPivot.position - grapplePoint;

            HitObject = true;           
        }            

        grappleRope.enabled = true; // turns on rope rendering

        if (fireSound.audioClip != null)
            playerAudio.PlayOneShot(fireSound.audioClip, fireSound.volume);
        
        grapplingAnimator.SetBool("grappling", true); // plays shoot animations

        onGrappleShotFired.Invoke();
    }

    public void ReleaseGrapple()
    {
        grappleRope.enabled = false;
        m_springJoint2D.enabled = false;
        //rigidbody.gravityScale = 1;

        grapplingAnimator.SetBool("grappling", false);
    }

    public void Grapple()
    {
        if (!launchToPoint && !autoCongifureDistance)
        {
            m_springJoint2D.distance = targetDistance;
            m_springJoint2D.frequency = targetFrequency;
        }

        if (!launchToPoint)
        {
            if (autoCongifureDistance)
            {
                m_springJoint2D.autoConfigureDistance = true;
                m_springJoint2D.frequency = 0;
            }
            m_springJoint2D.connectedAnchor = grapplePoint;
            m_springJoint2D.enabled = true;
        }

        else
        {
            if (Launch_Type == LaunchType.Transform_Launch)
            {
                rigidbody.gravityScale = 0;
                rigidbody.velocity = Vector2.zero;
            }
            if (Launch_Type == LaunchType.Physics_Launch)
            {
                m_springJoint2D.connectedAnchor = grapplePoint;
                m_springJoint2D.distance = 0;
                m_springJoint2D.frequency = launchSpeed;
                m_springJoint2D.enabled = true;
            }
        }

        if(hitObjectSound.audioClip != null)
            playerAudio.PlayOneShot(hitObjectSound.audioClip, hitObjectSound.volume);
    }

    private void OnDrawGizmos()
    {
        if (hasMaxDistance)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(firePoint.position, maxDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, aimDirection * maxDistance);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.root.position, aimDirection * 100);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(grapplePoint, 0.2f);
    }
    #endregion

}
