using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour, IInteractable
{
    [Serializable]
    public struct PreloadItem
    {
        public ScoopableObject.ScoopType type;
        public Material material;
    }

    [Header("Visual levels ordered bottom → top")]
    public List<GameObject> containerLevels;

    [Header("Initial Contents (bottom → top)")]
    [Tooltip("Optional: pre-fill the container. The first entry is the bottom layer, the last is the top.")]
    public List<PreloadItem> initialContents = new();

    [Header("Respawn setup for 'water > 2' rule")]
    public GameObject PlayerCapsule;
    public GameObject RespawnPoint;

    // Runtime stacks
    private readonly List<Material> stackedMaterials = new();
    private readonly List<ScoopableObject.ScoopType> stackedTypes = new();

    public bool CanInteract() => stackedMaterials.Count > 0;

    public bool Interact(Interactor interactor)
    {
        var cup = interactor.GetComponentInChildren<CupController>();
        if (cup == null || cup.IsFull || cup.IsInCooldown)
            return false;

        if (TryRemove(out var mat, out var type))
        {
            cup.Scoop(type, mat, null);
            return true;
        }

        return false;
    }

    public bool TryAdd(ScoopableObject.ScoopType type, Material material)
    {
        if (containerLevels.Count == 0 || stackedMaterials.Count >= containerLevels.Count)
            return false;

        type = NormalizeForContainer(type);
        int index = stackedMaterials.Count;
        stackedMaterials.Add(material);
        stackedTypes.Add(type);

        var level = containerLevels[index];
        ApplyLevelVisual(level, material, true);
        SetLevelColliderTrigger(level, type == ScoopableObject.ScoopType.Water); // Simplified!
        UpdateWaterRules();
        return true;
    }

    public bool TryRemove(out Material mat, out ScoopableObject.ScoopType type)
    {
        mat = null;
        type = default;
        if (stackedMaterials.Count == 0) return false;

        int topIndex = stackedMaterials.Count - 1;
        mat = stackedMaterials[topIndex];
        type = stackedTypes[topIndex];

        // Use the normalize function instead of manual conversion
        type = NormalizeForContainer(type);

        stackedMaterials.RemoveAt(topIndex);
        stackedTypes.RemoveAt(topIndex);

        var level = containerLevels[topIndex];
        ApplyLevelVisual(level, null, false);
        SetLevelColliderTrigger(level, false);
        UpdateWaterRules();
        return true;
    }

    // Prefill logic
    private void Start()
    {
        ApplyInitialContents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (!Application.isPlaying)
        {
            ApplyInitialContents();
        }
    }
#endif

    private void ApplyInitialContents()
    {
        if (containerLevels == null) return;

        // Clear visuals for all levels
        for (int i = 0; i < containerLevels.Count; i++)
        {
            var level = containerLevels[i];
            if (level == null) continue;
            ApplyLevelVisual(level, null, false);
            SetLevelColliderTrigger(level, false);
        }

        // Reset stacks
        stackedMaterials.Clear();
        stackedTypes.Clear();

        if (initialContents == null || initialContents.Count == 0)
        {
            UpdateWaterRules();
            return;
        }

        // Clamp to capacity
        int maxLayers = Mathf.Min(initialContents.Count, containerLevels.Count);

        // Fill bottom to top
        for (int i = 0; i < maxLayers; i++)
        {
            var item = initialContents[i];
            if (item.material == null) continue;

            // Normalize the type before storing
            var normalizedType = NormalizeForContainer(item.type);
            stackedMaterials.Add(item.material);
            stackedTypes.Add(normalizedType);

            var level = containerLevels[i];
            ApplyLevelVisual(level, item.material, true);
            SetLevelColliderTrigger(level, normalizedType == ScoopableObject.ScoopType.Water);
        }

        UpdateWaterRules();
    }

    private static ScoopableObject.ScoopType NormalizeForContainer(ScoopableObject.ScoopType t)
    {
        return t switch
        {
            ScoopableObject.ScoopType.DirtCup => ScoopableObject.ScoopType.Dirt,
            ScoopableObject.ScoopType.PouringWater => ScoopableObject.ScoopType.Water,
            _ => t
        };
    }

    private void ApplyLevelVisual(GameObject level, Material mat, bool active)
    {
        if (level == null) return;
        var mr = level.GetComponent<MeshRenderer>();
        if (mr != null) mr.material = mat;
        level.SetActive(active);
    }

    private void SetLevelColliderTrigger(GameObject level, bool isTrigger)
    {
        if (level == null) return;
        var box = level.GetComponentInChildren<BoxCollider>(true);
        if (box != null) box.isTrigger = isTrigger;
    }

    private void UpdateWaterRules()
    {
        // Count water types - simplified since normalization ensures only Water exists
        int waterCount = 0;
        for (int i = 0; i < stackedTypes.Count; i++)
            if (stackedTypes[i] == ScoopableObject.ScoopType.Water) waterCount++;

        if (containerLevels == null || containerLevels.Count == 0) return;

        var bottom = containerLevels[0];
        if (bottom == null) return;

        var existing = bottom.GetComponent<RespawnScript>();

        if (waterCount > 2)
        {
            if (existing == null)
                existing = bottom.AddComponent<RespawnScript>();

            if (existing != null)
            {
                existing.player = PlayerCapsule;
                existing.respawnPoint = RespawnPoint;
            }
        }
        else
        {
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }
        }
    }
}