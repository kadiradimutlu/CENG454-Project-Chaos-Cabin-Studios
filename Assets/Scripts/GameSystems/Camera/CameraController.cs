using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private MonoBehaviour inputReaderBehaviour;
    [SerializeField] private float mouseSensitivity = 200f;
    [SerializeField] private float minVerticalAngle = -45f;
    [SerializeField] private float maxVerticalAngle = 45f;
    [SerializeField] private bool lockCursorOnStart = true;

    private ICameraInputReader inputReader;
    private float xRotation;

    private void Awake()
    {
        inputReader = inputReaderBehaviour as ICameraInputReader;

        if (inputReader == null)
        {
            inputReader = GetComponent<ICameraInputReader>();
        }
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (playerBody == null || inputReader == null)
        {
            return;
        }

        Vector2 lookInput = inputReader.ReadLookInput() * mouseSensitivity * Time.deltaTime;

        xRotation -= lookInput.y;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * lookInput.x);
    }
}
