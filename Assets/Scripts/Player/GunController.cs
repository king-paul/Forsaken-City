using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GunController : MonoBehaviour
{
    [SerializeField] Transform gunArm;
    [SerializeField] SpriteRenderer leftArm;
    [SerializeField] Transform firingPoint;
    [SerializeField] Transform crosshair;
    [SerializeField] Rigidbody2D shotgunShell;
    [SerializeField] float shotGunShellForce = 5;
    [SerializeField] SpriteRenderer shotgunParticles;

    [Header("Gun Stats")]
    [SerializeField] uint maxAmmoInMagazine = 10;
    //[Range(1, 50)]
    [SerializeField] float maxFiringDistance = 10f;
    //[Range(0, 5)]
    [SerializeField] float delayBetweenShot = 0;
    [SerializeField] uint rangedWeaponDamage = 1;

    [Header("Sound Effects")]
    [SerializeField] SoundEffect fireSound;
    [SerializeField] SoundEffect reloadSound;
    [SerializeField] SoundEffect outOfAmmoSound;

    PlayerController player;
    Animator animator;
    HUD gui;
    AudioSource playerAudio;
    SpriteRenderer playerSprite;
    SpriteRenderer gunSprite;

    private uint magazineAmmo;
    private Vector2 aimDirection;
    private Vector2 castPosition;
    private float lastShotTime;
    private bool canShoot = true;
    private float originalX;

    private Vector2 shellSpawnPoint;

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = transform.root.GetComponent<PlayerController>();
        playerSprite = transform.root.GetComponent<SpriteRenderer>();
        playerAudio = transform.root.GetComponent<AudioSource>();
        gui = HUD.Instance;

        originalX = leftArm.transform.localPosition.x;

        gunSprite = GetComponent<SpriteRenderer>();

        shellSpawnPoint = shotgunShell.transform.localPosition;

        gui.SetMagazineAmmoText(magazineAmmo);
        gui.SetTotalAmmoText(player.TotalAmmo);
    }

    // Update is called once per frame
    void Update()
    {
        player.UpdateAim(out castPosition);
        aimDirection = (crosshair.position - firingPoint.position).normalized;
        
        if(playerSprite.flipX)
        {
            gunSprite.sortingOrder = -1;
            leftArm.sortingOrder = 1;
        }
        else
        {
            gunSprite.sortingOrder = 1;
            leftArm.sortingOrder = -1;
        }

        float timeSinceLastShot = Time.time - lastShotTime;

        if (timeSinceLastShot >= delayBetweenShot)
        {
            canShoot = true;
        }
    }

    public void FireShot()
    {
        if (!canShoot)
            return;

        if (magazineAmmo <= 0)
        {
            if(outOfAmmoSound.audioClip != null)
                playerAudio.PlayOneShot(outOfAmmoSound.audioClip, outOfAmmoSound.volume);

            Debug.Log("Need to reload");
            return;
        }

        animator.SetTrigger("fire"); // plays shooting animation

        if (fireSound.audioClip != null)
            playerAudio.PlayOneShot(fireSound.audioClip, fireSound.volume);

        Debug.DrawRay(firingPoint.position, aimDirection * maxFiringDistance, Color.red, 3.0f);

        // raycase firing
        var raycastHit = Physics2D.Raycast(firingPoint.position, aimDirection, maxFiringDistance);

        Vector2 particleSpawnPoint;

        if (raycastHit.collider != null) 
        {
            particleSpawnPoint = raycastHit.point;

            Instantiate(shotgunParticles, raycastHit.point, Quaternion.identity);

            if (raycastHit.transform.gameObject.layer == 7) // 7 = enemy layer
            {
                // deal damage to target
                var enemy = raycastHit.transform.GetComponent<EnemyController>();

                if(enemy.Alive)
                    enemy.TakeDamage(rangedWeaponDamage);
            }
        }
        else
        {
            particleSpawnPoint = castPosition + aimDirection * maxFiringDistance;
        }

        Instantiate(shotgunParticles, particleSpawnPoint, Quaternion.identity);

        magazineAmmo--;
        gui.SetMagazineAmmoText(magazineAmmo);

        lastShotTime = Time.time;
        canShoot = true;
    }

    public void SpawnShotgunShell()
    {
        shotgunShell.transform.localPosition = shellSpawnPoint;
        shotgunShell.gameObject.SetActive(true);

        shotgunShell.AddForce(aimDirection * shotGunShellForce, ForceMode2D.Impulse);
    }

    public void ReloadGun()
    {
        player = transform.root.GetComponent<PlayerController>();
        if (player.TotalAmmo == 0)
        {
            if (outOfAmmoSound.audioClip != null)
                playerAudio.PlayOneShot(outOfAmmoSound.audioClip, outOfAmmoSound.volume);

            Debug.Log("Out of ammo");
            return;
        }

        uint ammoToAdd = maxAmmoInMagazine - magazineAmmo;

        if (player.TotalAmmo >= ammoToAdd)
        {
            player.TotalAmmo -= ammoToAdd;
            magazineAmmo += ammoToAdd;
        }
        else
        {
            magazineAmmo += player.TotalAmmo;
            player.TotalAmmo = 0;
        }

        HUD.Instance.SetMagazineAmmoText(magazineAmmo);
        HUD.Instance.SetTotalAmmoText(player.TotalAmmo);

        if (playerAudio != null && reloadSound.audioClip != null)
            playerAudio.PlayOneShot(reloadSound.audioClip, reloadSound.volume);
    }

    public void EmptyMagazine() { magazineAmmo = 0; }
}
