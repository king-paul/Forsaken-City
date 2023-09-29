using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeController : MonoBehaviour
{
    [SerializeField] uint maximumGrenades = 5;

    [Header("Arm Sprite")]
    [SerializeField] SpriteRenderer armSprite;
    [SerializeField] float flippedXOffset = 0.25f;

    [Header("Grenades")]
    [SerializeField] GameObject grenadePrefab;
    [SerializeField] float throwPower = 300;
    [SerializeField] float spawnOffsetDistance = 1;

    [Header("Aiming")]
    [SerializeField] Transform aimPivot;
    [SerializeField] Transform crosshair;

    [Header("Overlap Spawning Prevention")]
    [SerializeField] LayerMask collisionLayers;
    [SerializeField] private float spawnCheckRadius = 0.5f;

    [Header("Sound Effects")]
    [SerializeField] SoundEffect throwGrenadeSound;
    [SerializeField] SoundEffect outOfGrenadesSound;

    // componenets & external scripts
    private HUD gui;
    private PlayerController player;
    private SpriteRenderer playerSprite;
    private AudioSource playerAudio;
    private Animator grenadeAnimator;

    private uint grenadesLeft = 5;
    Vector2 direction;
    private Vector2 spawnPosition;
    private Vector2 armOrgin;
    private Vector2 aimingOrigin;

    private List<Grenade> thrownGrenades = new List<Grenade>();

    void Awake()
    {       
        player = transform.root.GetComponent<PlayerController>();
        playerSprite = transform.root.GetComponent<SpriteRenderer>();
        playerAudio = transform.root.GetComponent<AudioSource>();
        grenadeAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        gui = HUD.Instance;
        gui.SetGrenadeTotal((int)grenadesLeft);

        armOrgin = armSprite.transform.localPosition;
        aimingOrigin = aimPivot.localPosition;
    }


    // Update is called once per frame
    void Update()
    {        
        direction = player.UpdateAim(out spawnPosition);

        if (playerSprite.flipX)
        {
            armSprite.transform.localPosition = armOrgin + Vector2.left * flippedXOffset;
            armSprite.sortingOrder = -1;
            aimPivot.localPosition = aimingOrigin;
        }
        else
        {
            armSprite.transform.localPosition = armOrgin;
            armSprite.sortingOrder = 1;
            aimPivot.localPosition = aimingOrigin;
        }
    }

    public void SetGrenadeTotal(uint total)
    {
        grenadesLeft = total;
        gui.SetGrenadeTotal((int)grenadesLeft);
    }

    public void AddGrenade()
    { 
        grenadesLeft++;
        gui.SetGrenadeTotal((int)grenadesLeft);
    }

    public void RestoreGrenades()
    {
        grenadesLeft = maximumGrenades;
        gui.SetGrenadeTotal((int)grenadesLeft);
    }

    public void RestoreGrenades(uint amount)
    {
        grenadesLeft += amount;
    }

    public void ThrowGrenade()
    {
        grenadeAnimator.SetTrigger("Throw");
    }

    // spawns a grenade infront of the player and adds a force to it the aiming direction
    public void SpawnGrenade()
    {
        if (grenadesLeft == 0)
        {
            if (outOfGrenadesSound.audioClip != null)
                playerAudio.PlayOneShot(outOfGrenadesSound.audioClip, outOfGrenadesSound.volume);

            return;
        }

        // if the grenade would spawn inside another object exit this function
        var collision = Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, collisionLayers);
        if (collision != null)
            return;

        float spawnAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;
        var grenadeInstance = Instantiate(grenadePrefab, spawnPosition, Quaternion.Euler(0, 0, spawnAngle));

        var grenade = grenadeInstance.GetComponent<Grenade>();

        grenade.Direction = direction;
        grenade.ThrowPower = throwPower;

        thrownGrenades.Add(grenade);

        grenadesLeft--;
        gui.SetGrenadeTotal((int)grenadesLeft);

        if (throwGrenadeSound.audioClip != null)
            playerAudio.PlayOneShot(throwGrenadeSound.audioClip, throwGrenadeSound.volume);

        if (grenadesLeft <= 0)
        {
            player.GetComponent<Animator>().SetBool("HoldingGrenade", false);
            GetComponent<SpriteRenderer>().enabled = false;

            return;
        }
    }

    // drops a grenade directly infront of the player
    public void DropGrenade()
    {
        if (grenadesLeft <= 0)
            return;

        Vector2 direction = (playerSprite.flipX) ? Vector2.left : Vector2.right;
        //Vector2 spawnPosition = (Vector2)transform.position + direction * spawnOffsetDistance;

        // if the grenade would spawn inside another object exit this function
        var collision = Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, collisionLayers);
        if (collision != null)
            return;

        var grenadeInstance = Instantiate(grenadePrefab, spawnPosition, Quaternion.identity);

        var grenade = grenadeInstance.GetComponent<Grenade>();
        grenade.Direction = direction;
        thrownGrenades.Add(grenade);

        grenadesLeft--;
        gui.SetGrenadeTotal((int)grenadesLeft);
    }

    // blows up a grenade after it has been thrown/dropped
    public void DetonateGrenade()
    {
        foreach (Grenade grenade in thrownGrenades)
        {
            grenade.Detonate();
        }

        thrownGrenades.Clear();
    }

    public void IncreaseMaxGrenades(uint amount, bool restorToMax)
    {
        maximumGrenades += amount;

        if(restorToMax)
            RestoreGrenades();
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(spawnPosition, spawnCheckRadius);
    }

}