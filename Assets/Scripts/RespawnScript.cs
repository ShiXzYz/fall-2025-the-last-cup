using UnityEngine;

public class RespawnScript : MonoBehaviour
{
    public GameObject player;
    public GameObject respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // Debug.Log("Respawning player to: " + respawnPoint.transform.position);

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false; // Disable character controller temporarily to disable physics and enable position transformation
                player.transform.position = respawnPoint.transform.position;
                controller.enabled = true;
            }
        }
    }
}
