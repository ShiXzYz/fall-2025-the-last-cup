using UnityEngine;

public class WaterProjectileConfig : MonoBehaviour
{
    [Header("Projectile Properties")]
    public float damage = 1f;
    public float lifetime = 3f;
    public float muzzleSpeed = 12f;
    public float fireRate = 5f;

    [Header("Water Consumption")]
    public float squirtRate = 10f;
    public float maxWater = 100f;

    [Header("Visuals")]
    public Material waterMaterial;
    public GameObject waterProjectilePrefab;

    [Header("Layer")]
    public string projectileLayerName = "WaterProjectile";
}