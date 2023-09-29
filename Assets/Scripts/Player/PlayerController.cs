using System.Collections;
using UnityEngine;

public enum ItemType
{
    None = 0,
    MeleeWeapon = 1,
    Gun = 2,
    Grenade = 3,
    GrapplingHook = 4,
    GasMask = 5,
}

public enum AimingMode
{
    UseDrag,
    FollowCursor
}

[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    #region variables
    [Header("Player Stats")]
    [SerializeField] int maxHealth = 10;
    [SerializeField] uint startingAmmo = 0;
    public uint startingGrenades = 0;
    [SerializeField] uint maxGunAmmo = 50;
    

    [Header("Aiming")]
    public Transform aimPivot;
    [SerializeField] Transform crosshair;
    [SerializeField] AimingMode aimingMode = AimingMode.UseDrag;
    //[Range(0, 360)]
    [SerializeField] float aimingRange = 180;
    [SerializeField] float flippedAimPivotOffset = 0.3f;

    [Header("Use Drag Mode Options")]
    //[Range(0, 10)]
    [SerializeField] float aimRotationSpeed = 1;
    [SerializeField] bool useVerticalDrag = true;
    [SerializeField] bool useHorizontalDrag = true;

    [Header("Melee Weapon")]
    [SerializeField] GameObject meleeObject;

    [Header("Ranged Weapons")]
    [SerializeField] GameObject shotgun;
    public GunController gunController;

    [Header("Grenades")]
    [SerializeField] GameObject grenadeArm;
    [SerializeField] GrenadeController grenadeController;

    [Header("Grappling hook")]
    public GameObject grapplingGun;
    [SerializeField] float maxGrapplingDistance = 5.0f;
    [SerializeField] float timeBetweenGrapples = 1.0f;
    [SerializeField] float spawnOffsetDistance = 0.5f;

    [Header("Abilities")]
    [SerializeField] bool enableMelee = true;
    [SerializeField] bool enableGun = false;
    [SerializeField] bool enableGrenade;
    [SerializeField] bool enableGrapplingHook;
    [SerializeField] bool wearingMask = false;

    [Header("Enemy Collisions")]
    [SerializeField] float delayBetweenDamage = 0.5f;
    [SerializeField] Color damageColor = Color.red;

    [Header("Sound Effects")]
    [SerializeField] SoundEffect damagedSound;
    [SerializeField] SoundEffect deathSound;
    [SerializeField] SoundEffect openCrateSound;

    [Header("Animator Controllers")]
    [SerializeField] RuntimeAnimatorController unmaskedController;
    [SerializeField] RuntimeAnimatorController maskedController;

    // private varialbes
    private int health;
    private uint totalAmmo;
    private float pivotAngle = 0; // used for aiming
    private Vector3 defaultAimPivotPosition;
    private float damageTimer = 0;
    private float pollutedTimer;
    private Transform backArm;
    private ItemType itemSelected;
    private bool isFlipped = false;


    // grappling hook
    private float grappleTimer = 0;
    private bool grappledToObject = false;

    // components
    Animator animator;
    SpriteRenderer sprite;
    AudioSource playerAudio;

    // other scripts
    GameManager gameManager;
    HUD gui;
    PlayerMovement playerMovement;
    InputHandler input;
    #endregion

    // properties
    public int MaxHealth => maxHealth;
    public int CurrentHealth => health;
    public uint StartingAmmo => startingAmmo;
    public uint TotalAmmo { get => totalAmmo; set => totalAmmo = value; }
    public bool IsAlive { get; private set; } = true;
    public ItemType ItemSelected => itemSelected;

    #region unity functions
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.Instance;
        gui = HUD.Instance;
        playerMovement = GetComponent<PlayerMovement>();
        input = GetComponent<InputHandler>();

        health = maxHealth;
        totalAmmo = startingAmmo;

        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        playerAudio = GetComponent<AudioSource>();

        itemSelected = ItemType.None;
        gui.SetItem(itemSelected);

        grapplingGun.SetActive(false);

        backArm = shotgun.transform.GetChild(0);        

        defaultAimPivotPosition = aimPivot.localPosition;

        if (wearingMask)
            PutOnMask();
        else
            animator.runtimeAnimatorController = unmaskedController;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameManager.GameRunning)
            return;

        if (itemSelected == ItemType.Gun)
        {
            // update arm behind gun
            float depth = sprite.flipX ? 0 : 0.2f;
            backArm.position = new Vector3(backArm.position.x, backArm.position.y, depth);
        }

    }

    private void FixedUpdate()
    {
        UpdateTimers();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7) // Enemy layer
        {
            if (damageTimer <= 0)
            {
                damageTimer = delayBetweenDamage;

                if (transform.position.x > collision.transform.position.x)
                    playerMovement.Knockback(Vector2.right);
                else
                    playerMovement.Knockback(Vector2.left);

                TakeDamage(collision.gameObject.GetComponent<EnemyController>().DamagerPerHit);
            }
        }

        if (collision.gameObject.layer == 8) // item layer
        {
            GameObject.Destroy(collision.gameObject);

            if (collision.gameObject.tag == "Grenade")
            {
                grenadeController.AddGrenade();                
            }
        }

    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7) // Enemy layer
        {
            if (damageTimer <= 0)
            {
                damageTimer = delayBetweenDamage;

                TakeDamage(collision.gameObject.GetComponent<EnemyController>().DamagerPerHit);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 9) // enemy projectile
        {
            var damage = collision.gameObject.GetComponent<EnemyController>().DamagerPerHit;
            TakeDamage(damage);
        }

        if (collision.gameObject.layer == 8) // item layer
        {
            collision.gameObject.GetComponent<ItemBox>().OpenBox();
            playerMovement.StopMoving();
            playerMovement.enabled = false;

            if (openCrateSound.audioClip != null)
                playerAudio.PlayOneShot(openCrateSound.audioClip, openCrateSound.volume);
        }

        if(collision.tag == "Finish")
        {
            // disable scripts
            input.enabled = false;
            playerMovement.enabled = false;
            this.enabled = false;

            gameManager.StartEndSequence(true); // ends the game
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7) // Enemy layer
        {
            if (damageTimer <= 0)
            {
                damageTimer = delayBetweenDamage;

                if (transform.position.x > collision.transform.position.x)
                    playerMovement.Knockback(Vector2.right);
                else
                    playerMovement.Knockback(Vector2.left);

                TakeDamage(collision.gameObject.GetComponent<EnemyController>().DamagerPerHit);
            }
        }

        if (collision.gameObject.layer == 11 && !wearingMask) // Polluted layer
        {
            if (!IsAlive) // dont take damage if already dead
                return;

            var pollution = PollutionManager.Instance;

            //Debug.Log("You have entered a polluted zone");
            pollutedTimer -= Time.fixedDeltaTime;

            if (pollutedTimer <= 0)
            {
                pollutedTimer = pollution.TimePerImpact;
                TakeDamage(pollution.DamagePerImpact);
            }
        }

    }
    #endregion

    #region public functions
    public void SetItem(ItemType item)
    {
        itemSelected = item;

        gui.SetItem(itemSelected);
        input.SwitchItem((int)itemSelected);

        switch (itemSelected)
        {
            case ItemType.None:
                crosshair.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                break;

            case ItemType.MeleeWeapon:
                crosshair.gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                break;

            case ItemType.Gun:
                gunController.ReloadGun();

                crosshair.gameObject.SetActive(true);

                if (aimingMode == AimingMode.UseDrag)
                    Cursor.lockState = CursorLockMode.Locked;
                else
                    Cursor.lockState = CursorLockMode.Confined;
            break;

            case ItemType.Grenade:
                crosshair.gameObject.SetActive(true);

                if (aimingMode == AimingMode.UseDrag)
                    Cursor.lockState = CursorLockMode.Locked;
                else
                    Cursor.lockState = CursorLockMode.Confined;
            break;

            case ItemType.GrapplingHook:
                grapplingGun.SetActive(true);
                crosshair.gameObject.SetActive(true);

                Cursor.lockState = CursorLockMode.Locked;
            break;

            default:
                crosshair.gameObject.SetActive(false);
            break;
        }

        grapplingGun.SetActive(itemSelected == ItemType.GrapplingHook);
        meleeObject.SetActive(itemSelected == ItemType.MeleeWeapon);
        shotgun.SetActive(itemSelected == ItemType.Gun);
        grenadeArm.SetActive(itemSelected == ItemType.Grenade);

        gui.SetAmmoVisible(itemSelected == ItemType.Gun);
        grenadeController.enabled = itemSelected == ItemType.Grenade;

        // animation parameters
        animator.SetBool("HoldingMelee", itemSelected == ItemType.MeleeWeapon);
        animator.SetBool("HoldingSingleHand", (itemSelected == ItemType.Grenade || itemSelected == ItemType.GrapplingHook));
        animator.SetBool("HoldingDoubleHand", itemSelected == ItemType.Gun);

        animator.SetTrigger("SwitchItem");
    }

    public void NextItem()
    {
        itemSelected++;

        if ((int)itemSelected > 4)
            itemSelected = ItemType.None;

        switch (itemSelected)
        {
            case ItemType.None:
                SetItem(ItemType.None);
                break;

            case ItemType.MeleeWeapon:
                if (enableMelee)
                    SetItem(ItemType.MeleeWeapon);
                else
                    NextItem();
                break;

            case ItemType.Gun:
                if (enableGun)
                    SetItem(ItemType.Gun);
                else
                    NextItem();
                break;

            case ItemType.Grenade:
                if (enableGrenade)
                    SetItem(ItemType.Grenade);
                else
                    NextItem();
                break;

            case ItemType.GrapplingHook:
                if (enableGrapplingHook)
                    SetItem(ItemType.GrapplingHook);
                else
                    NextItem();
                break;
        }

    }

    public void PreviousItem()
    {
        itemSelected--;

        if ((int)itemSelected < 0)
            itemSelected = ItemType.GrapplingHook;

        switch (itemSelected)
        {
            case ItemType.None:
                SetItem(ItemType.None);
                break;

            case ItemType.MeleeWeapon:
                if (enableMelee)
                    SetItem(ItemType.MeleeWeapon);
                else
                    PreviousItem();
                break;

            case ItemType.Gun:
                if (enableGun)
                    SetItem(ItemType.Gun);
                else
                    PreviousItem();
                break;

            case ItemType.Grenade:
                if (enableGrenade)
                    SetItem(ItemType.Grenade);
                else
                    PreviousItem();
                break;

            case ItemType.GrapplingHook:
                if (enableGrapplingHook)
                    SetItem(ItemType.GrapplingHook);
                else
                    PreviousItem();
                break;
        }
    }

    public void GiveUpgrade(ItemType item)
    {
        switch (item)
        {
            case ItemType.GasMask:
                PutOnMask();
                break;

            case ItemType.MeleeWeapon:
                enableMelee = true;
                break;

            case ItemType.Gun:
                enableGun = true;
                break;

            case ItemType.Grenade:
                enableGrenade = true;
                break;

            case ItemType.GrapplingHook:
                enableGrapplingHook = true;
                break;
        }

        if (item != ItemType.GasMask)
            SetItem(item);

        playerMovement.enabled = true;
    }

    public void PutOnMask()
    {
        animator.runtimeAnimatorController = maskedController;
        gui.SetMaskIconVisible(true);
        wearingMask = true;
    }

    #region health, ammo & grenades
    public void RestoreHealth()
    {
        health = maxHealth;
        gui.SetHealthDisplay(health, maxHealth);
    }

    public void RestoreHealth(uint amount)
    {
        health += (int) amount;
        gui.SetHealthDisplay(health, maxHealth);
    }

    public void IncreaseMaxHealth(uint amount, bool restoreToMax)
    {
        maxHealth += (int)amount;

        if(restoreToMax)
            RestoreHealth();
    }

    public void RestoreAmmoAndGrenades()
    {
        totalAmmo = maxGunAmmo;
        grenadeController.RestoreGrenades();
    }

    public void RestoreAmmo(uint amount)
    {
        totalAmmo += amount;
        gunController.ReloadGun();
    }

    public void RestoreAmmo()
    {
        totalAmmo = maxGunAmmo;
        gunController.EmptyMagazine();
        gunController.ReloadGun();
    }

    public void IncreaseMaxAmmo(uint amount, bool restoreToMax) 
    { 
        maxGunAmmo += amount;

        if(restoreToMax)
            RestoreAmmo();
    }
    #endregion

    public Vector2 UpdateAim(out Vector2 spawnPosition)
    {
        Vector2 direction = new Vector2();

        // update pivot position
        if (sprite.flipX)
            aimPivot.localPosition = defaultAimPivotPosition + (Vector3.right * flippedAimPivotOffset);
        else
            aimPivot.localPosition = defaultAimPivotPosition;

        if (aimingMode == AimingMode.FollowCursor)
        {
            Cursor.lockState = CursorLockMode.Confined;

            Vector2 worldPos = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            sprite.flipX = worldPos.x < sprite.transform.position.x;

            // flip the pivot and aim direction if the player sprite is flipped
            if (sprite.flipX)
            {
                aimPivot.rotation = Quaternion.Euler(0, 180, pivotAngle);
                direction = ((Vector2)transform.position - worldPos).normalized;
                direction.y = -direction.y;
            }
            else
            {
                aimPivot.rotation = Quaternion.Euler(0, 0, pivotAngle);
                direction = (worldPos - (Vector2)transform.position).normalized;
            }

            // rotate the pivot
            pivotAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            pivotAngle = Mathf.Clamp(pivotAngle, -aimingRange / 2, aimingRange / 2); // restricts angle to range
        }

        if (aimingMode == AimingMode.UseDrag)
        {
            if (useHorizontalDrag)
                pivotAngle -= input.AimInput.x * aimRotationSpeed * Time.deltaTime;

            if (useVerticalDrag)
                pivotAngle += input.AimInput.y * aimRotationSpeed * Time.deltaTime;

            pivotAngle = Mathf.Clamp(pivotAngle, -aimingRange / 2, aimingRange / 2);

            aimPivot.rotation = sprite.flipX ? Quaternion.Euler(0, 180, pivotAngle) :
                                aimPivot.rotation = Quaternion.Euler(0, 0, pivotAngle);

            direction = (crosshair.position - transform.position).normalized;
        }

        spawnPosition = (Vector2)transform.position + VectorToBoxEdge(direction) + (direction * spawnOffsetDistance);

        return direction;
    }

    public void DealMeleeDamage()
    {
        meleeObject.GetComponent<MeleeWaponController>().DealDamage();
    }

    // Only used in Grappling Test scene 1
    public void FireGrappingHook()
    {
        if (!enableGrapplingHook || grappleTimer > 0)
            return;

        GetComponent<GrapplingHook>().DisconnectJoint();

        Vector2 direction = (crosshair.position - transform.position).normalized;
        Vector2 spawnPosition = (Vector2)transform.position + direction * spawnOffsetDistance;

        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, transform.position);

        // raycase firing
        var hit = Physics2D.Raycast(spawnPosition, direction, maxGrapplingDistance);

        if (hit.transform != null)
        {
            lineRenderer.SetPosition(1, hit.transform.position);

            if (hit.transform.gameObject.layer == 3) // ground layer
            {
                GetComponent<GrapplingHook>().GrappleToObject(hit.transform);
                grappledToObject = true;
            }
            else
            {
                grappledToObject = false;
            }
        }
        else
        {
            Vector2 endPoint = spawnPosition + direction * maxGrapplingDistance;
            lineRenderer.SetPosition(1, endPoint);
            grappledToObject = false;
        }

        grappleTimer = timeBetweenGrapples;
    }

    public void TakeDamage(uint amount)
    {
        health -= (int)amount;

        StartCoroutine(FlashDamage());

        if (health < 0)
            health = 0;

        gui.SetHealthDisplay(health, maxHealth);

        if (health <= 0)
        {
            if (deathSound.audioClip != null)
                playerAudio.PlayOneShot(deathSound.audioClip, deathSound.volume);

            animator.SetTrigger("death");            

            // disable scripts
            input.enabled = false;
            playerMovement.enabled = false;
            this.enabled = false;

            IsAlive = false;
            Physics2D.IgnoreLayerCollision(6, 7, true);
        }
        else if (damagedSound.audioClip != null)
        {
            playerAudio.PlayOneShot(damagedSound.audioClip, damagedSound.volume);
        }
    }

    public void Die() {
        gameManager.StartEndSequence(false);     
    }

    #endregion

    #region private functions
    private void UpdateTimers()
    {
        if (damageTimer > 0)
            damageTimer -= Time.fixedDeltaTime;
        else if (damageTimer < 0)
            damageTimer = 0;

        if (grappleTimer > 0)
        {
            grappleTimer -= Time.fixedDeltaTime;
        }
        else if (grappleTimer < 0)
        {
            grappleTimer = 0;

            if (!grappledToObject)
                GetComponent<LineRenderer>().enabled = false;
        }
    }

    private Vector2 VectorToBoxEdge(Vector2 direction)
    {
        Vector2 boxSize = transform.root.GetComponent<CapsuleCollider2D>().size;
        Vector2 vectorToEdge;

        // Edge-fitting required, determine side of intersection
        if (Mathf.Abs(direction.x) * boxSize.y <= Mathf.Abs(direction.y) * boxSize.x)
        {
            // Intersection with top or bottom side of quad
            vectorToEdge.x = (boxSize.y / 2f) * (direction.x / Mathf.Abs(direction.y));
            vectorToEdge.y = Mathf.Sign(direction.y) * (boxSize.y / 2f);
        }
        else
        {
            // Intersection with left or right side of quad
            vectorToEdge.x = Mathf.Sign(direction.x) * (boxSize.x / 2f);
            vectorToEdge.y = boxSize.x / 2f * (direction.y / Mathf.Abs(direction.x));
        }

        return vectorToEdge;
    }

    private IEnumerator FlashDamage()
    {
        sprite.color = damageColor;
        yield return new WaitForSeconds(0.1f);
        sprite.color = Color.white;
    }
    #endregion

}