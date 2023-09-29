using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestoreBox : ItemBox
{
    enum RestoreType
    {
        Health, Ammo, Grenades
    }

    [Header("Stat Upgrade/Restoration")]
    [SerializeField] RestoreType statToRestore;    
    [SerializeField] uint upgradeAmount;    
    [SerializeField] bool restoreToMaximum = true;
    [SerializeField] uint amountToGive;
   
    new void Start()
    {
        base.Start();
    }    
    new void Update()
    {
        base.Update();
    }

    protected override void GiveItem()
    {
        gui.CloseMessageBox();
        GameObject.Destroy(gameObject);

        switch (statToRestore)
        {
            case RestoreType.Health:                
                if(upgradeAmount > 0)
                    player.IncreaseMaxHealth(upgradeAmount, restoreToMaximum);
                else if (restoreToMaximum)
                    player.RestoreHealth();
                else
                    player.RestoreHealth(amountToGive);
            break;

            case RestoreType.Ammo:
                if (upgradeAmount > 0)
                    player.IncreaseMaxAmmo(upgradeAmount, restoreToMaximum);
                else if(restoreToMaximum)
                    player.RestoreAmmo();
                else                
                    player.RestoreAmmo(amountToGive);
            break;

            case RestoreType.Grenades:

                var grenades = GameObject.FindObjectOfType<GrenadeController>();

                if (grenades == null)
                {
                    player.GiveUpgrade(ItemType.Grenade);
                }
                else
                {

                    if (upgradeAmount > 0)
                        grenades.IncreaseMaxGrenades(upgradeAmount, restoreToMaximum);
                    else if (restoreToMaximum)
                        grenades.RestoreGrenades();
                    else
                        grenades.RestoreGrenades(amountToGive);
                }
            break;
        }

        player.GetComponent<PlayerMovement>().enabled = true;
    }

    protected override void ShowMessage()
    {
        if (upgradeAmount > 0)
            gui.ShowMessage("Your maximum " + itemName + " capacity has been increased");
        else if (restoreToMaximum)
            gui.ShowMessage("Your " + itemName + " has been restored");
        else
            gui.ShowMessage("You collected " + amountToGive + " " + itemName);
    }

}
