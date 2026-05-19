using System.Collections;
using Fusion;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [Header("Local Camera")]
    [SerializeField] private GameObject playerCameraObject;
    [SerializeField] private CameraFollowRig cameraFollowRig;
    [SerializeField] private Transform cameraTarget;

    private Camera playerCamera;
    private AudioListener audioListener;
    private bool detachedLocalCamera;

    private void Awake()
    {
        CacheCameraReferences();
        DisableLocalCamera();
    }

    public override void Spawned()
    {
        CacheCameraReferences();

        if (playerCameraObject == null)
        {
            Debug.LogWarning("PlayerCameraSetup: Player Camera Object atanmadı.");
            return;
        }

        if (cameraFollowRig == null)
        {
            Debug.LogWarning("PlayerCameraSetup: Camera Follow Rig atanmadı.");
            return;
        }

        if (!HasInputAuthority)
        {
            DisableLocalCamera();
            return;
        }

        EnableLocalCamera();

        if (playerCameraObject.transform.parent != null)
        {
            playerCameraObject.transform.SetParent(null, true);
            detachedLocalCamera = true;
        }

        Transform targetToUse = cameraTarget != null ? cameraTarget : transform;
        cameraFollowRig.SetTarget(targetToUse, true);

        StartCoroutine(DisableMenuCameraNextFrame());
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (detachedLocalCamera && playerCameraObject != null)
            Destroy(playerCameraObject);
    }

    private void CacheCameraReferences()
    {
        if (playerCameraObject == null)
            return;

        if (playerCamera == null)
            playerCamera = playerCameraObject.GetComponentInChildren<Camera>(true);

        if (audioListener == null)
            audioListener = playerCameraObject.GetComponentInChildren<AudioListener>(true);
    }

    private void EnableLocalCamera()
    {
        if (playerCameraObject != null)
            playerCameraObject.SetActive(true);

        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.tag = "MainCamera";
        }

        if (audioListener != null)
            audioListener.enabled = true;
    }

    private void DisableLocalCamera()
    {
        if (playerCamera != null)
            playerCamera.enabled = false;

        if (audioListener != null)
            audioListener.enabled = false;

        if (playerCameraObject != null)
            playerCameraObject.SetActive(false);
    }

    private IEnumerator DisableMenuCameraNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        MainMenuManager menuManager = FindObjectOfType<MainMenuManager>();

        if (menuManager != null)
            menuManager.DisableMenuCameraForGameplay();
        else
            Debug.LogWarning("PlayerCameraSetup: MainMenuManager bulunamadı, menu camera kapatılamadı.");
    }
}
