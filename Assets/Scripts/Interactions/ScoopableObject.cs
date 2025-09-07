using UnityEngine;

public class ScoopableObject : MonoBehaviour, IInteractable
{
    public enum ScoopType { Dirt, Water, PouringWater, Object, Container, DirtCup }
    public ScoopType interactionType;
    public Material objectMaterial;

    public bool CanInteract() => true;

    public bool Interact(Interactor interactor)
    {
        var cup = interactor.GetComponentInChildren<CupController>();
        if (cup == null || cup.IsFull || cup.IsInCooldown) return false;

        if (interactionType != ScoopType.Container)
        {
            cup.Scoop(interactionType, objectMaterial, gameObject);
            return true;
        }

        var container = GetComponent<Container>();
        if (container != null && container.TryRemove(out var mat, out var type))
        {
            cup.Scoop(type, mat, null);
            return true;
        }

        return false;
    }
}