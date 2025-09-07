using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WaterProjectile : MonoBehaviour
{
    public float damage = 1f;
    public float life = 3f;

    private Collider myCol;
    private readonly List<(Collider a, Collider b)> ignoredPairs = new();
    private float timer;

    void Awake()
    {
        InitializeCollider();
    }

    private void InitializeCollider()
    {
        if (myCol == null)
        {
            myCol = GetComponent<Collider>();
            myCol.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
        }
    }

    public void Init(float dmg, float lifetime, Collider[] ignoreThese)
    {
        damage = dmg;
        life = lifetime;

        InitializeCollider();

        if (ignoreThese != null)
        {
            foreach (var c in ignoreThese)
            {
                if (c == null || c == myCol || !c.enabled) continue;

                Physics.IgnoreCollision(myCol, c, true);
                ignoredPairs.Add((myCol, c));
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= life) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we should ignore this collider
        foreach (var pair in ignoredPairs)
            if (other == pair.b) return;

        // Look for SquirtImpactable component
        SquirtImpactable impact = other.GetComponent<SquirtImpactable>();

        if (impact != null)
        {
            impact.ApplySquirtHit(damage);
            Destroy(gameObject);
            return;
        }

        // Hit something without SquirtImpactable
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        foreach (var pair in ignoredPairs)
            if (pair.a && pair.b) Physics.IgnoreCollision(pair.a, pair.b, false);
        ignoredPairs.Clear();
    }
}