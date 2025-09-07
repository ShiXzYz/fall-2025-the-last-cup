using UnityEngine;

public class SquirtImpactable : MonoBehaviour
{
    public float maxHP = 50f;
    public float currentHP = 50f;

    [Tooltip("When HP <= threshold, scale *= factor once.")]
    public float[] thresholds = new float[] { 75f, 50f, 25f };

    [Tooltip("Scale multiplier applied each threshold.")]
    public float thresholdScaleFactor = 0.75f;

    bool[] used;

    void Awake()
    {
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        used = new bool[thresholds.Length];
    }

    public void ApplySquirtHit(float damage)
    {
        if (currentHP <= 0f) return;

        float prev = currentHP;
        currentHP = Mathf.Max(0f, currentHP - Mathf.Max(0f, damage));

        for (int i = 0; i < thresholds.Length; i++)
        {
            if (!used[i] && prev > thresholds[i] && currentHP <= thresholds[i])
            {
                used[i] = true;
                transform.localScale *= thresholdScaleFactor;
            }
        }

        if (currentHP <= 0f)
        {
            // Destroy on "death"
            Destroy(gameObject);
        }
    }
}