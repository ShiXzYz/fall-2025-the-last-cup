using UnityEngine;

public class SquirtMechanic : MonoBehaviour
{
    [Header("Squirting Setup")]
    public WaterProjectileConfig projectileConfig;
    public Transform strawTip;

    // Reference to CupController on the same GameObject
    private CupController cupController;

    // Squirting-specific state
    public bool squirtOn = false;
    private float currentWater = 100f;
    private float fireTimer = 0f;
    private Collider[] selfColliders;

    [Header("AnimationManager")]
    public AnimationManager animationManager;

    void Awake()
    {
        // Get reference to CupController on the same GameObject
        cupController = GetComponent<CupController>();

        if (cupController == null)
        {
            Debug.LogError("SquirtMechanic requires a CupController component on the same GameObject!");
            enabled = false;
            return;
        }

        selfColliders = GetComponentsInChildren<Collider>();

        // Setup projectile layer collision rules
        if (projectileConfig != null)
        {
            int projLayer = LayerMask.NameToLayer(projectileConfig.projectileLayerName);
            if (projLayer != -1)
            {
                Physics.IgnoreLayerCollision(projLayer, projLayer, true);
            }
        }
    }

    void Update()
    {
        // Handle pause menu (same logic as CupController)
        if (Time.timeScale == 0f) return;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        // Refills if previously empty
        if (currentWater == 0 && HasWater())
        {
            currentWater = 100f;
        }

        // Handle squirting input
        if (Input.GetKeyDown(KeyCode.Mouse1) && cupController.HasStraw)
        {
            animationManager.Squirt();
            squirtOn = true;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1) && squirtOn == true)
        {
            animationManager.Unsquirt();
            squirtOn = false;
            fireTimer = 0f;
        }

        // Handle continuous squirting
        if (cupController.HasStraw && Input.GetKey(KeyCode.Mouse1) && currentWater > 0)
        {
            ProcessSquirting();
        }
    }

    // This method gets called by CupController when straw equipment changes
    public void OnStrawEquipmentChanged(bool hasStraw)
    {
        // Stop ongoing squirt if straw is removed
        if (!hasStraw && squirtOn)
        {
            animationManager.Unsquirt();
            squirtOn = false;
            fireTimer = 0f;
        }
    }

    // This method gets called by CupController when scooping water
    public void OnWaterScooped(ScoopableObject.ScoopType type)
    {
        // Fill water capacity for water types
        if (type == ScoopableObject.ScoopType.Water || type == ScoopableObject.ScoopType.PouringWater)
        {
            currentWater = projectileConfig != null ? projectileConfig.maxWater : 100f;
        }
    }

    // This method gets called by CupController when cup is emptied
    public void OnCupEmptied()
    {
        currentWater = 0f;
    }

    private bool HasWater()
    {
        // Access CupController's properties directly
        return cupController.IsFull
            && (cupController.HeldType == ScoopableObject.ScoopType.Water ||
                cupController.HeldType == ScoopableObject.ScoopType.PouringWater);
    }

    private void ProcessSquirting()
    {
        if (projectileConfig == null) return;

        // Consume water continuously
        float consume = projectileConfig.squirtRate * Time.deltaTime;
        currentWater = Mathf.Max(0f, currentWater - consume);

        if (currentWater == 0f)
        {
            // Tell CupController to empty the cup
            cupController.EmptyCup();
            return;
        }

        // Emit droplets at fire rate
        fireTimer += Time.deltaTime;
        float interval = 1f / Mathf.Max(1f, projectileConfig.fireRate);

        while (fireTimer >= interval && cupController.movementController._isAimingActive)
        {
            fireTimer -= interval;
            SpawnWaterDroplet();
        }
    }

    private void SpawnWaterDroplet()
    {
        if (strawTip == null || projectileConfig == null) return;

        // Calculate spawn position
        const float spawnOffset = 0.06f;
        Vector3 spawnPos = strawTip.position + strawTip.forward * spawnOffset;
        Quaternion rotation = Quaternion.Euler(cupController.movementController._cinemachineTargetPitch, cupController.movementController._cinemachineTargetYaw, 0f);

        // Multiply the rotation by Vector3.forward to get the resulting forward vector.
        Vector3 dir = rotation * Vector3.forward;

        Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, dir);

        // Create projectile
        GameObject go = CreateProjectile(spawnPos, spawnRot);
        if (go == null) return;

        // Setup physics
        SetupProjectilePhysics(go, dir);

        // Initialize WaterProjectile component
        InitializeWaterProjectile(go);
    }

    private GameObject CreateProjectile(Vector3 position, Quaternion rotation)
    {
        GameObject go;

        if (projectileConfig.waterProjectilePrefab != null)
        {
            go = Instantiate(projectileConfig.waterProjectilePrefab, position, rotation);
        }
        else
        {
            // Fallback: create sphere
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one * 0.1f;

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && projectileConfig.waterMaterial != null)
                mr.material = projectileConfig.waterMaterial;
        }

        // Set layer
        int projLayer = LayerMask.NameToLayer(projectileConfig.projectileLayerName);
        if (projLayer != -1) go.layer = projLayer;

        return go;
    }

    private void SetupProjectilePhysics(GameObject go, Vector3 direction)
    {
        // Ensure components exist
        var col = go.GetComponent<Collider>();
        if (col == null) col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Apply force
        rb.AddForce(direction * projectileConfig.muzzleSpeed, ForceMode.VelocityChange);
    }

    private void InitializeWaterProjectile(GameObject go)
    {
        var proj = go.GetComponent<WaterProjectile>();
        if (proj == null) proj = go.AddComponent<WaterProjectile>();

        // Filter out null/disabled colliders
        var validColliders = new System.Collections.Generic.List<Collider>();
        if (selfColliders != null)
        {
            foreach (var selfCol in selfColliders)
            {
                if (selfCol != null && selfCol.enabled)
                {
                    validColliders.Add(selfCol);
                }
            }
        }

        if (validColliders.Count > 0)
        {
            proj.Init(projectileConfig.damage, projectileConfig.lifetime, validColliders.ToArray());
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (strawTip)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(strawTip.position, 0.02f);
            Gizmos.DrawRay(strawTip.position, strawTip.forward * 0.5f);
        }
    }
}