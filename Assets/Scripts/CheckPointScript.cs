using UnityEngine;

public class CheckPointScript : MonoBehaviour
{
    private RespawnScript[] allRespawnScripts;
    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();

        // Use FindObjectsByType for multiple objects; FindObjectsSortMode.None gets all objects with RespawnScript in no particular order (faster)
        allRespawnScripts = FindObjectsByType<RespawnScript>(FindObjectsSortMode.None);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // Debug.Log("Checkpoint achieved: " + this.gameObject.transform.position);

            // Update all RespawnScript components
            foreach (RespawnScript respawn in allRespawnScripts)
            {
                respawn.respawnPoint = this.gameObject;
            }

            boxCollider.enabled = false;
        }
    }
}