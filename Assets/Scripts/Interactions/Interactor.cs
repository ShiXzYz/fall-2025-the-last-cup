using UnityEngine;
using System;
using System.Linq;

public class Interactor : MonoBehaviour
{
    [Header("Cast shape & distances")]
    [SerializeField] private float _castDistance = 10f;
    [SerializeField] private float _castRadius = 5f;
    [SerializeField] private Vector3 _raycastOffset = new Vector3(0, 0.25f, 0);

    [Header("Per-input layer masks")] // These masks are needed to allow for the interaction system to differentiate between what type of item is being interacted with
                                      // If not separated, it results in collider issues when interacting with objects that are next to each other (e.g., bucket handle and zipline)
    [SerializeField] private LayerMask _equipmentMask;   // Equipment layer only
    [SerializeField] private LayerMask _scoopableMask;   // Scoopable objects and containers layer
    [SerializeField] private LayerMask _ziplineMask;     // Zipline layer only

    [Header("Player")]
    [SerializeField] public GameObject player;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // If left mouse button, scoop or descoop
        {
            TryInteract(_scoopableMask, i => i is ScoopableObject || i is Container, QueryTriggerInteraction.Collide);
            // QueryTriggerInteraction.Collide means query should register with hits with trigger colliders
        }

        if (Input.GetKeyDown(KeyCode.Q)) // If press Q, can can zipline
        {
            TryInteract(_ziplineMask, i => i is Zipline, QueryTriggerInteraction.Ignore);
            // QueryTriggerInteraction.Ignore means query never reports trigger hits
        }

        if (Input.GetKeyDown(KeyCode.E)) // If press E, then an equip/unequip
        {
            if (!TryInteract(_equipmentMask, i => i is EquipmentInteractable, QueryTriggerInteraction.Ignore))
            {
                var equipMgr = GetComponentInChildren<EquipmentManager>();
                if (equipMgr && equipMgr.EquippedOn) equipMgr.TryUnequip();
            }
        }
    }

    bool TryInteract(LayerMask mask, Predicate<IInteractable> typeFilter,
                 QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
    {
        if (FindBest(mask, typeFilter, out IInteractable interactable, qti))
        {
            Debug.Log("TryInteract success");
            return interactable.CanInteract() && interactable.Interact(this); // Can interact and action was, if true, executed
        }

        Debug.Log("TryInteract failed");
        return false;
    }

    // This function tries to utilize the layers to find the best interactable object 
    // by performing a sphere cast in the player's forward direction and scoring 
    // candidates based on distance and alignment with the player's view
    bool FindBest(LayerMask mask, Predicate<IInteractable> typeFilter, out IInteractable best, QueryTriggerInteraction qti = QueryTriggerInteraction.Ignore)
    {
        Vector3 origin = transform.position + _raycastOffset;
        Vector3 dir = transform.forward;

        var hits = Physics.SphereCastAll(new Ray(origin, dir), _castRadius, _castDistance, mask, qti); 
        if (hits.Length > 0)
        {
            // Score: distance along ray (ascending) with small penalty for off-center angle
            IInteractable candidate = null;
            float bestScore = float.PositiveInfinity;

            foreach (var h in hits)
            {
                var it = h.collider.GetComponentInParent<IInteractable>() ?? h.collider.GetComponent<IInteractable>();
                if (it == null) continue;
                if (typeFilter != null && !typeFilter(it)) continue;

                // Penalize objects far from center of view
                Vector3 to = (h.collider.bounds.center - origin);
                float forward = Mathf.Max(0f, Vector3.Dot(to.normalized, dir));
                float anglePenalty = 1f - forward; // 0 when centered, 1 when 90°
                float score = h.distance + anglePenalty * 0.5f; // tune 0.5f if needed

                if (score < bestScore) { bestScore = score; candidate = it; }
            }

            if (candidate != null) { best = candidate; return true; }
        }

        // Fallback: area overlap detection - if the sphere cast missed, check for any 
        // interactable objects within a spherical area around the midpoint of the cast 
        // distance and select the one that's nearest and most aligned with the player's 
        // forward direction
        Collider[] overlaps = Physics.OverlapSphere(origin + dir * (_castDistance * 0.5f), _castRadius, mask, qti);
        if (overlaps.Length > 0)
        {
            IInteractable candidate = null;
            float bestScore = float.PositiveInfinity;

            foreach (var c in overlaps)
            {
                var it = c.GetComponentInParent<IInteractable>() ?? c.GetComponent<IInteractable>();
                if (it == null) continue;
                if (typeFilter != null && !typeFilter(it)) continue;

                Vector3 p = c.bounds.ClosestPoint(origin);
                Vector3 v = p - origin;
                float along = Vector3.Dot(v, dir);
                if (along < 0f) continue; // behind us

                float lateral = (v - dir * along).magnitude; // Distance off the ray
                float score = along + lateral * 0.25f;       // Prefer closer + centered

                if (score < bestScore) { bestScore = score; candidate = it; }
            }

            if (candidate != null) { best = candidate; return true; }
        }

        best = null;
        return false;
    }

    // Draws raycasting to understand how it behaves in the Scene mode
    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + _raycastOffset;
        Vector3 center = origin + transform.forward * (_castDistance * 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, _castRadius);
    }
}