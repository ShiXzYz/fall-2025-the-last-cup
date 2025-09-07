using UnityEngine;

public class InteractionTesting : MonoBehaviour, IInteractable
{
    public bool CanInteract()
    {
        return true;
    }

    public bool Interact(Interactor interactor)
    {
        Debug.Log("Interacted");

        return true;
    }
}
