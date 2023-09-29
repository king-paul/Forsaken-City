using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public PlayerControls playerControls;

    [Header("Input Events")]
    public UnityEvent onUpPressed;
    public UnityEvent onUpReleased;
    public UnityEvent onDownPressed;
    public UnityEvent onLeftPressed;
    public UnityEvent onRightPressed;
    public UnityEvent onJumpPressed;
    public UnityEvent onMeleeAttack;
    public UnityEvent onMeleeUpwardAttack;
    public UnityEvent onShoot;
    public UnityEvent onReload;
    public UnityEvent onFireGrapplingHook;
    public UnityEvent onGrapplingHookRelease;
    public UnityEvent onThrowGrenade;
    public UnityEvent onDropGrenade;
    public UnityEvent onDetonateGrenade;
    public UnityEvent onRotateAim;
    public UnityEvent onNextItemSelected;
    public UnityEvent onPreviousItemSelected;
    public UnityEvent onItemChanged;

    // Player Input
    private InputAction movement;
    private InputAction jump;
    private InputAction grapplingHookShoot;
    private InputAction grapplingHookRelease;
    private InputAction meleeAttack;
    private InputAction shoot;
    private InputAction reload;
    private InputAction grenadeThrow;
    private InputAction grenadeDrop;
    private InputAction grenadeDetonate;
    private InputAction scrollUp;
    private InputAction scrollDown;
    private InputAction aim;

    private Vector2 moveDirection;
    private Vector2 aimInput = Vector2.zero;

    private bool upPressed = false;

    // properties
    public float HorizontalInput => moveDirection.x;
    public float VerticalInput => moveDirection.y;
    public bool JumpPressed => jump.IsPressed();

    public Vector2 AimInput { get => aimInput; } 

    // Start is called before the first frame update
    void Start()
    {
        //meleeAttack.Enable();
        shoot.Disable();
        reload.Disable();
    }

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    // Update is called once per frame
    void Update()
    {             
        moveDirection = movement.ReadValue<Vector2>();

        if (moveDirection.x < 0)
            onLeftPressed.Invoke();
        if (moveDirection.x > 0)
            onRightPressed.Invoke();
        if (moveDirection.y > 0)
        {
            upPressed = true;
            onUpPressed.Invoke();
        }
        if (moveDirection.y < 0)
            onDownPressed.Invoke();

        if (upPressed && moveDirection.y <= 0)
        {
            onUpReleased.Invoke();
            upPressed = false;
        }

        //Debug.Log("Aim: " + AimInput);        
    }

    private void LateUpdate()
    {
        aimInput = Vector2.zero;
    }

    private void OnEnable()
    {
        movement = playerControls.Player.Move;
        movement.Enable();

        aim = playerControls.Player.Aim;
        aim.Enable();
        aim.performed += context => { aimInput = aim.ReadValue<Vector2>(); };

        jump = playerControls.Player.Jump;
        jump.Enable();
        jump.performed += context => { onJumpPressed.Invoke(); };

        meleeAttack = playerControls.Player.Attack;
        meleeAttack.performed += context =>  { onMeleeAttack.Invoke(); };

        shoot = playerControls.Player.Shoot;
        shoot.performed += context => { onShoot.Invoke(); };

        grapplingHookShoot = playerControls.Player.ThrowGraplingHook;
        grapplingHookShoot.performed += context => { onFireGrapplingHook.Invoke(); };

        reload = playerControls.Player.Reload;
        reload.performed += context => { onReload.Invoke(); };

        grapplingHookRelease = playerControls.Player.ReleaseGraplingHook;        
        grapplingHookRelease.performed += context => { onGrapplingHookRelease.Invoke(); };

        grenadeThrow = playerControls.Player.GrenadeThrow;       
        grenadeThrow.performed += context => { onThrowGrenade.Invoke(); };

        grenadeDrop = playerControls.Player.GrenadeDrop;        
        grenadeDrop.performed += context => { onDropGrenade.Invoke(); };

        grenadeDetonate = playerControls.Player.GrenadeDetonate;        
        grenadeDetonate.performed += context => { onDetonateGrenade.Invoke(); };

        scrollUp = playerControls.Player.PrevItem;
        scrollUp.Enable();
        scrollUp.performed += context => { onPreviousItemSelected.Invoke(); };

        scrollDown = playerControls.Player.NextItem;
        scrollDown.Enable();
        scrollDown.performed += context => { onNextItemSelected.Invoke(); };
    }

    public void SwitchItem(int number)
    {
        meleeAttack.Disable();
        shoot.Disable();
        reload.Disable();
        grapplingHookShoot.Disable();
        grapplingHookRelease.Disable();
        grenadeThrow.Disable();
        grenadeDrop.Disable();

        var controller = GetComponent<PlayerController>();

        switch (number)
        {
            case 1:
                meleeAttack.Enable();                
            break;

            case 2:
                shoot.Enable();
                reload.Enable();                

                Cursor.lockState = CursorLockMode.Locked;
            break;

            case 3:
                grenadeThrow.Enable();
                grenadeDrop.Enable();
                grenadeDetonate.Enable();                
            break;

            case 4:
                grapplingHookShoot.Enable();
                grapplingHookRelease.Enable();               

                Cursor.lockState = CursorLockMode.None;
            break;
        }

        onItemChanged.Invoke();
    }

    private void OnDisable()
    {
        movement.Disable();        
        jump.Disable();
        grapplingHookShoot.Disable();
        grapplingHookRelease.Disable();
        meleeAttack.Disable();
        shoot.Disable();
        reload.Disable();
        grenadeThrow.Disable();
        grenadeDrop.Disable();
        grenadeDetonate.Disable();
        scrollUp.Disable();
        scrollDown.Disable();
    }

}
