using UnityEngine;

public class CharacterSkinDatabase : MonoBehaviour
{
    public static CharacterSkinDatabase Instance { get; private set; }

    [System.Serializable]
    public class CharacterSkin
    {
        [Header("Display")]
        public string skinName = "Character";
        public Sprite previewSprite;
        public Color previewColor = Color.white;

        [Header("Gameplay Visual")]
        public GameObject visualPrefab;

        [Header("Fallback Material")]
        public Material material;

        [Header("Optional Visual Offset")]
        public Vector3 visualLocalPosition = Vector3.zero;
        public Vector3 visualLocalEulerAngles = Vector3.zero;
        public Vector3 visualLocalScale = Vector3.one;
    }

    [Header("Character Skins")]
    [SerializeField] private CharacterSkin[] skins;

    public int SkinCount
    {
        get
        {
            if (skins == null || skins.Length == 0)
                return 4;

            return skins.Length;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public string GetSkinName(int index)
    {
        CharacterSkin skin = GetSkin(index);

        if (skin == null)
            return $"Character {index + 1}";

        if (string.IsNullOrWhiteSpace(skin.skinName))
            return $"Character {index + 1}";

        return skin.skinName;
    }

    public Sprite GetPreviewSprite(int index)
    {
        CharacterSkin skin = GetSkin(index);
        return skin != null ? skin.previewSprite : null;
    }

    public Color GetPreviewColor(int index)
    {
        CharacterSkin skin = GetSkin(index);

        if (skin == null)
            return Color.white;

        Color color = skin.previewColor;
        color.a = 1f;

        return color;
    }

    public GameObject GetVisualPrefab(int index)
    {
        CharacterSkin skin = GetSkin(index);
        return skin != null ? skin.visualPrefab : null;
    }

    public Vector3 GetVisualLocalPosition(int index)
    {
        CharacterSkin skin = GetSkin(index);
        return skin != null ? skin.visualLocalPosition : Vector3.zero;
    }

    public Quaternion GetVisualLocalRotation(int index)
    {
        CharacterSkin skin = GetSkin(index);
        return skin != null
            ? Quaternion.Euler(skin.visualLocalEulerAngles)
            : Quaternion.identity;
    }

    public Vector3 GetVisualLocalScale(int index)
    {
        CharacterSkin skin = GetSkin(index);

        if (skin == null)
            return Vector3.one;

        if (skin.visualLocalScale == Vector3.zero)
            return Vector3.one;

        return skin.visualLocalScale;
    }

    public Material GetMaterial(int index)
    {
        CharacterSkin skin = GetSkin(index);
        return skin != null ? skin.material : null;
    }

    private CharacterSkin GetSkin(int index)
    {
        if (skins == null || skins.Length == 0)
            return null;

        index = Mathf.Clamp(index, 0, skins.Length - 1);
        return skins[index];
    }
}
