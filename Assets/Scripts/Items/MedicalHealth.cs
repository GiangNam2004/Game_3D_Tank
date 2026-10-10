using UnityEngine;

public class MedicalHealth : MonoBehaviour
{
    public float startingHealth = 60f;
    public float dropChance = 0.75f;

    private float currentHealth;
    private bool destroyed;

    private void Awake()
    {
        ItemCollisionUtility.ConfigureSolidObstacle(gameObject);
    }

    private void OnEnable()
    {
        currentHealth = startingHealth;
        destroyed = false;
    }

    public void TakeDamage(float amount)
    {
        if (destroyed || amount <= 0f)
            return;

        currentHealth -= amount;
        if (currentHealth <= 0f)
            DestroyMedical();
    }

    private void DestroyMedical()
    {
        if (destroyed)
            return;

        destroyed = true;
        ItemDropSystem.TryDrop(transform.position, dropChance);
        Destroy(gameObject);
    }
}
