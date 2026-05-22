using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerRoleLabel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RoleHandler roleHandler;
    [SerializeField] private TMP_Text roleText;

    [Header("Billboard")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private Transform billboardRoot;

    private RoleHandler.PlayerRole lastRole = (RoleHandler.PlayerRole)(-999);
    private Camera cachedCamera;

    private void Awake()
    {
        CacheReferences();
        ShowRole(RoleHandler.PlayerRole.None, true);
    }

    private void OnEnable()
    {
        CacheReferences();
        ShowRole(RoleHandler.PlayerRole.None, true);
    }

    private void Update()
    {
        CacheReferences();

        if (roleHandler == null || roleText == null)
            return;

        if (!roleHandler.TryGetRole(out RoleHandler.PlayerRole role))
        {
            ShowRole(RoleHandler.PlayerRole.None, false);
            return;
        }

        ShowRole(role, false);
    }

    private void LateUpdate()
    {
        if (faceCamera)
            FaceCamera();
    }

    private void CacheReferences()
    {
        if (roleHandler == null)
            roleHandler = GetComponentInParent<RoleHandler>();

        if (roleText == null)
            roleText = GetComponentInChildren<TMP_Text>(true);

        if (billboardRoot == null && roleText != null)
            billboardRoot = roleText.transform.parent != null ? roleText.transform.parent : roleText.transform;
    }

    private void ShowRole(RoleHandler.PlayerRole role, bool force)
    {
        if (roleText == null)
            return;

        if (!force && lastRole == role)
            return;

        lastRole = role;

        roleText.gameObject.SetActive(true);
        roleText.text = RoleHandler.GetRoleDisplayName(role);
        roleText.color = RoleHandler.GetRoleColor(role);
    }

    private void FaceCamera()
    {
        if (billboardRoot == null)
            return;

        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        billboardRoot.rotation = cachedCamera.transform.rotation;
    }
}