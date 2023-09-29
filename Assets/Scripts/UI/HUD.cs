using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [Header("Gun Ammo")]
    public GameObject ammoDisplay;
    public TextMeshProUGUI magazineAmmoText;
    public TextMeshProUGUI totalAmmoText;

    [Header("Selected Item")]
    public TextMeshProUGUI itemText;
    public TextMeshProUGUI grenadeTotalText;
    public Image itemItcon;

    [Header("Icons")]
    [SerializeField] GameObject maskIcon;
    [SerializeField] Sprite meleeIcon;
    [SerializeField] Sprite gunIcon;
    [SerializeField] Sprite grenadeIcon;
    [SerializeField] Sprite grapplingHookIcon;

    [Header("Heart bar")]
    [SerializeField] Sprite emptyHeart;
    [SerializeField] Sprite quarterFilledHeart;
    [SerializeField] Sprite halfFilledHeart;
    [SerializeField] Sprite threeQuarterFilledHeart;
    [SerializeField] Sprite filledHeart;
    [SerializeField] Image[] heartIcons;

    [Header("Messaages")]
    [SerializeField] GameObject messageBox;
    [SerializeField] TextMeshProUGUI messageBoxText;

    private PlayerController player;

    public static HUD Instance { get; private set; } // singleton

    private void Awake()
    {
        // If there is an instance, and it is not this one, delete it.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }

        Instance = this;
    }

    void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<PlayerController>();

        SetHealthDisplay(player.MaxHealth, player.MaxHealth);
    }

    public void SetMaskIconVisible(bool visible)
    {
        maskIcon.SetActive(visible);
    }

    public void SetAmmoVisible(bool visible)
    {
        ammoDisplay.SetActive(visible);
    }

    public void SetTotalAmmoText(uint ammo)
    {
        totalAmmoText.text = ammo.ToString();
    }

    public void SetMagazineAmmoText(uint ammo)
    {
        magazineAmmoText.text = ammo.ToString();
    }

    public void SetGrenadeTotal(int total)
    {
        grenadeTotalText.text = "x"+total.ToString();
    }

    public void SetHealthDisplay(int health, int maxHealth)
    {
        int filledHearts = (int) Mathf.Floor(health / 4);
        int maxHearts = (int)(maxHealth / 4);

        if (maxHearts == 0)
            maxHearts = 1;

        // show filled and empty hearts
        for(int i=0; i < maxHearts; i++)
        {
            heartIcons[i].gameObject.SetActive(true);

            if (i < filledHearts)
                heartIcons[i].sprite = filledHeart;
            else
                heartIcons[i].sprite = emptyHeart;
        }

        // set sprite of last heart
        int remainder = health % 4;

        if (remainder == 1)
            heartIcons[filledHearts].sprite = quarterFilledHeart;
        if (remainder == 2)
            heartIcons[filledHearts].sprite = halfFilledHeart;
        if (remainder == 3)
            heartIcons[filledHearts].sprite = threeQuarterFilledHeart;
    }

    public void SetItem(ItemType item)
    {
        // if the player is holding an item turn on the item icon
        if(!itemItcon.gameObject.activeInHierarchy && item != ItemType.None)
            itemItcon.gameObject.SetActive(true);

        switch (item)
        {
            case ItemType.MeleeWeapon:
                itemText.text = "Melee";
                itemItcon.sprite = meleeIcon;
            break;

            case ItemType.Gun:
                itemText.text = "Shotgun";
                itemItcon.sprite = gunIcon;
            break;

            case ItemType.Grenade:
                itemText.text = "Grenade";
                itemItcon.sprite = grenadeIcon;
            break;

            case ItemType.GrapplingHook:
                itemText.text = "Grappling Hook";
                itemItcon.sprite = grapplingHookIcon;
            break;

            default:
                itemItcon.gameObject.SetActive(false);
                itemText.text = "None";
            break;
        }

        // turn on the grenade total if grenades are selected
        grenadeTotalText.gameObject.SetActive(item == ItemType.Grenade);
    }

    public void ShowMessage(string message)
    {
        messageBox.SetActive(true);
        messageBoxText.text = message;
    }

    public void CloseMessageBox()
    {
        messageBox.SetActive(false);
    }

}
