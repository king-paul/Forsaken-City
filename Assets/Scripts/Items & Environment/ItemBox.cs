using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBox : MonoBehaviour
{
    [SerializeField] ItemType upgradeInBox;
    [SerializeField] protected Sprite itemImage;
    [SerializeField] protected string itemName;
    [SerializeField] protected float messageDisplayTime = 3;

    [Header("Rising Image")]
    [SerializeField] protected float imageRizeSpeed = 1;
    [SerializeField] protected float imageRiseDistance = 1;

    protected PlayerController player;
    protected HUD gui;

    private Transform item;
    private Vector2 origin;
    private bool boxOpen = false;

    // Start is called before the first frame update
    protected void Start()
    {
        item = transform.GetChild(0);
        origin = item.position;
        item.GetComponent<SpriteRenderer>().sprite = itemImage;

        player = GameObject.FindObjectOfType<PlayerController>();
        gui = HUD.Instance;
    }

    // Update is called once per frame
    protected void Update()
    {
        if(boxOpen)
        {
            float distanceTravelled = Vector2.Distance(origin, item.position);

            if (distanceTravelled < imageRiseDistance)
            {
                item.Translate(Vector2.up * imageRizeSpeed * Time.deltaTime);
            }
            else
            {
                ShowMessage();
                Invoke("GiveItem", messageDisplayTime);
                boxOpen = false;
            }
        }
    }

    protected virtual void ShowMessage()
    {
        gui.ShowMessage("You obtained a " + itemName);
    }

    protected virtual void GiveItem()
    {
        gui.CloseMessageBox();
        player.GiveUpgrade(upgradeInBox);

        GameObject.Destroy(gameObject);
    }

    public void OpenBox()
    {
        boxOpen = true;
    }
}
