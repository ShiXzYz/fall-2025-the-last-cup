using UnityEngine;

public enum EquipmentType { None, Straw, BucketHandle }

[DisallowMultipleComponent]
public class EquipmentManager : MonoBehaviour
{
    [Header("Cup reference")]
    public CupController cup;                       // Uses cup.spawnPoint to drop on unequip

    [Header("Cup visuals (inactive by default)")]
    public GameObject strawVisual;                  // Child on the cup
    public GameObject bucketHandleVisual;           // Child on the cup

    [Header("Equipment Properties")]
    public EquipmentType CurrentType { get; private set; } = EquipmentType.None;
    public bool EquippedOn => CurrentType != EquipmentType.None;

    private EquipmentInteractable currentPickup;   // World pickup that was taken

    [Header("AnimationManager")]
    public AnimationManager animationManager;

    void Awake()
    {
        if (!cup) cup = GetComponentInChildren<CupController>();
        SetVisuals(false, false);
    }

    private void SetVisuals(bool strawOn, bool handleOn)
    {
        if (strawVisual) strawVisual.SetActive(strawOn);
        if (bucketHandleVisual) bucketHandleVisual.SetActive(handleOn);

        // Gate squirting on the cup (expects small helper in CupController)
        if (cup) cup.SetStrawEquipped(strawOn);
    }

    private bool IsZiplining()
    {
        // Returns active GameObjects with Zipline tag
        // Fine for single-player, would need to be changed for multi-player
        var tagged = GameObject.FindGameObjectsWithTag("Zipline");
        foreach (var go in tagged)
        {
            var z = go.GetComponent<Zipline>();
            if (z && z.isActiveAndEnabled && z.zipping)
                return true;
        }
        return false;
    }

    public bool TryEquip(EquipmentInteractable pickup)
    {
        if (EquippedOn || pickup == null) return false;

        switch (pickup.type)
        {
            case EquipmentType.Straw:
                SetVisuals(true, false);
                CurrentType = EquipmentType.Straw;
                break;
            case EquipmentType.BucketHandle:
                SetVisuals(false, true);
                CurrentType = EquipmentType.BucketHandle;
                break;
            default:
                return false;
        }

        currentPickup = pickup;

        // Hide world pickup while equipped (no physics changes)
        pickup.gameObject.SetActive(false);

        // Play scoop animation
        animationManager.Scoop();

        return true;
    }

    public bool TryUnequip()
    {
        if (!EquippedOn) return false;

        if (IsZiplining())
        {
            Debug.LogWarning("[EquipmentManager] Cannot unequip while ziplining.");
            return false;
        }

        if (!cup || !cup.spawnPoint)
        {
            Debug.LogWarning("[EquipmentManager] Cup or spawnPoint missing; cannot drop equipment.");
            return false;
        }

        if (currentPickup)
        {
            // Translate to pour spawn point and reactivate
            Transform pour = cup.spawnPoint;
            pour.rotation = Quaternion.Euler(pour.eulerAngles.x, pour.eulerAngles.y, currentPickup.transform.eulerAngles.z);
            currentPickup.transform.SetPositionAndRotation(pour.position, pour.rotation);
            currentPickup.gameObject.SetActive(true);
        }

        // Play descoop animation
        animationManager.Descoop();

        currentPickup = null;
        CurrentType = EquipmentType.None;
        SetVisuals(false, false);

        return true;
    }
}