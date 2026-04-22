using System.Collections;
using Fusion;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [SerializeField] private GameObject playerCameraObject;
    [SerializeField] private CameraFollowRig cameraFollowRig;
    [SerializeField] private Transform cameraTarget;

    private Camera _playerCamera;
    private AudioListener _audioListener;

    private void Awake()
    {
        if (playerCameraObject != null)
        {
            _playerCamera = playerCameraObject.GetComponentInChildren<Camera>(true);
            _audioListener = playerCameraObject.GetComponentInChildren<AudioListener>(true);
        }
    }

    public override void Spawned()
    {
        Debug.Log(
            $"PlayerCameraSetup Spawned | Object={name} | " +
            $"HasInputAuthority={HasInputAuthority} | " +
            $"InputAuthority={(Object != null ? Object.InputAuthority.ToString() : "NULL")}"
        );

        if (playerCameraObject == null)
        {
            Debug.LogWarning("PlayerCameraSetup: playerCameraObject is not assigned.");
            return;
        }

        if (cameraFollowRig == null)
        {
            Debug.LogWarning("PlayerCameraSetup: cameraFollowRig is not assigned.");
            return;
        }

        if (!HasInputAuthority)
        {
            playerCameraObject.SetActive(false);
            Debug.Log($"PlayerCameraSetup: Camera disabled for remote player {name}");
            return;
        }

        Transform targetToUse = cameraTarget != null ? cameraTarget : transform;

        playerCameraObject.SetActive(true);

        if (_playerCamera == null)
            _playerCamera = playerCameraObject.GetComponentInChildren<Camera>(true);

        if (_audioListener == null)
            _audioListener = playerCameraObject.GetComponentInChildren<AudioListener>(true);

        if (_playerCamera != null)
            _playerCamera.enabled = true;

        if (_audioListener != null)
            _audioListener.enabled = true;

        cameraFollowRig.SetTarget(targetToUse, true);

        Debug.Log($"PlayerCameraSetup: Local player camera enabled for {name}");

        StartCoroutine(DisableMenuCameraNextFrame());
    }

    private IEnumerator DisableMenuCameraNextFrame()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        MainMenuManager menuManager = FindObjectOfType<MainMenuManager>();
        if (menuManager != null)
        {
            menuManager.DisableMenuCameraForGameplay();
        }
        else
        {
            Debug.LogWarning("PlayerCameraSetup: MainMenuManager not found, menu camera could not be disabled.");
        }
    }
}