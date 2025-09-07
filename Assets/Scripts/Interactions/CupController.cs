using UnityEngine;
using UnityEngine.EventSystems;

public class CupController : MonoBehaviour
{
    [Header("Cup Components")]
    public SkinnedMeshRenderer cupBodyRenderer;
    public Material defaultMaterial;
    public Transform spawnPoint;
    public GameObject dirtCupPrefab;

    [Header("Cup Properties")]
    public float cooldownDuration = 0.2f;

    [Header("Equipment State")]
    [SerializeField] private bool _hasStraw = false;

    [Header("AnimationManager")]
    public AnimationManager animationManager;

    // Cup state properties
    public bool IsFull { get; private set; }
    public bool IsInCooldown => cooldownTimer > 0f;
    public bool HasStraw => _hasStraw;

    // Private fields
    private ScoopableObject.ScoopType heldType;
    private Material heldMaterial;
    private GameObject heldObject;
    private float cooldownTimer = 0f;

    protected virtual void Awake()
    {
        // Base initialization
    }

    protected virtual void Update()
    {
        // Handle pause menu
        if (Time.timeScale == 0f) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Update cooldown
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        // Handle base interactions
        if (Input.GetMouseButtonDown(0) && IsFull && !IsInCooldown)
            TryDescoop();
        else if (Input.GetKeyDown(KeyCode.R))
        {
            animationManager.Spin();
        }
    }

    public virtual void SetStrawEquipped(bool on)
    {
        _hasStraw = on;
    }

    public virtual void Scoop(ScoopableObject.ScoopType type, Material mat, GameObject sourceObject)
    {
        heldType = type;
        heldMaterial = mat;
        heldObject = (type == ScoopableObject.ScoopType.Object || type == ScoopableObject.ScoopType.DirtCup) ? sourceObject : null;

        var mats = cupBodyRenderer.materials;
        if (mats.Length > 1)
        {
            mats[1] = mat;
            cupBodyRenderer.materials = mats;
        }

        if (type != ScoopableObject.ScoopType.PouringWater && IsFull == false)
        {
            animationManager.Scoop();
        }

        if (heldObject != null)
        {
            heldObject.SetActive(false);
        }

        IsFull = true;
        cooldownTimer = cooldownDuration;
    }

    protected virtual void TryDescoop()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out var hit, 3f))
        {
            var container = hit.collider.GetComponent<Container>();
            if (container != null && heldType != ScoopableObject.ScoopType.Object)
            {
                if (container.TryAdd(heldType, heldMaterial))
                {
                    if (IsFull)
                    {
                        animationManager.Descoop();
                        EmptyCup();
                    }
                    cooldownTimer = cooldownDuration;
                    return;
                }
            }
        }

        // Handle object descooping
        if (heldType == ScoopableObject.ScoopType.Object || heldType == ScoopableObject.ScoopType.DirtCup)
        {
            if (heldObject != null)
            {
                heldObject.transform.position = spawnPoint.position;
                heldObject.SetActive(true);
            }
            else if (dirtCupPrefab != null)
            {
                GameObject spawned = GameObject.Instantiate(dirtCupPrefab);
                spawned.transform.position = spawnPoint.position;
            }
        }
        else if (heldType == ScoopableObject.ScoopType.Dirt)
        {
            if (dirtCupPrefab != null)
            {
                GameObject spawned = GameObject.Instantiate(dirtCupPrefab);
                spawned.transform.position = spawnPoint.position;
            }
        }

        if (IsFull)
        {
            animationManager.Descoop();
            EmptyCup();
        }
        cooldownTimer = cooldownDuration;
    }

    public void EmptyCup()
    {
        var mats = cupBodyRenderer.materials;
        if (mats.Length > 1)
        {
            mats[1] = defaultMaterial;
            cupBodyRenderer.materials = mats;
        }

        IsFull = false;
        heldObject = null;
        heldMaterial = null;
    }

    // Protected accessors for derived classes
    public ScoopableObject.ScoopType HeldType => heldType;
    protected Material HeldMaterial => heldMaterial;
    protected GameObject HeldObject => heldObject;
}