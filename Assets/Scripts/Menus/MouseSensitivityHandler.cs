using UnityEngine;
using UnityEngine.SceneManagement;

public class MouseSensitivityHandler : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    public float sensitivity = 5f;
    public bool invertY = false;

    [Header("Camera Control")]
    [SerializeField] private Transform playerBody;  // Horizontal rotation (Y-axis)
    [SerializeField] private Transform cameraPivot; // Vertical rotation (X-axis)

    [Header("Clamping")]
    public float minY = 0f;
    public float maxY = 10f;

    private float xRotation = 0f;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != "TitleScreen")
        {
            // Load saved sensitivity or default to 0.5
            sensitivity = PlayerPrefs.GetFloat("sensitivity", 0.5f);
        }
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        if (invertY)
            mouseY = -mouseY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void SetSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
        AdjustSpeed(newSensitivity);
        PlayerPrefs.SetFloat("sensitivity", newSensitivity);
    }

    public void AdjustSpeed(float newSpeed)
    {
        sensitivity = newSpeed * 10f; // Match reference behavior
        PlayerPrefs.SetFloat("sensitivity", sensitivity);
        Debug.Log($"[MouseHandler] Adjusted sensitivity to {sensitivity}");
    }
}