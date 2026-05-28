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

    [Header("Fallback Label")]
    [SerializeField] private Vector3 labelOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float labelFontSize = 3.2f;

    private RoleHandler.PlayerRole lastRole = (RoleHandler.PlayerRole)(-999);
    private Camera cachedCamera;

    private void Awake()
    {
        CacheReferences();
        EnsureLabelExists();
        ShowRole(RoleHandler.PlayerRole.None, true);
    }

    private void OnEnable()
    {
        CacheReferences();
        EnsureLabelExists();
        ShowRole(RoleHandler.PlayerRole.None, true);
    }

    private void Update()
    {
        CacheReferences();
        EnsureLabelExists();

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
            billboardRoot = roleText.transform;
    }

    private void EnsureLabelExists()
    {
        if (roleText != null)
            return;

        GameObject labelObject = new GameObject("RoleLabelText");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = labelOffset;
        labelObject.transform.localRotation = Quaternion.identity;
        labelObject.transform.localScale = Vector3.one;

        TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = labelFontSize;
        text.text = "";
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.sizeDelta = new Vector2(5f, 1f);

        roleText = text;
        billboardRoot = labelObject.transform;
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
