using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class EquipmentInteractable : MonoBehaviour, IInteractable
{
    public EquipmentType type;

    public bool CanInteract() => true;

    public bool Interact(Interactor interactor)
    {
        if (Input.GetKeyDown(KeyCode.E) )
        {
            var mgr = interactor.GetComponentInChildren<EquipmentManager>();
            if (!mgr) return false;

            // If nothing equipped, equip this; otherwise unequip current item
            if (!mgr.EquippedOn) return mgr.TryEquip(this);
            return mgr.TryUnequip();
        }

        return false;
    }
}