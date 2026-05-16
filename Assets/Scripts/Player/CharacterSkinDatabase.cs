using UnityEngine;

public class CharacterSkinDatabase : MonoBehaviour
{
    public static CharacterSkinDatabase Instance { get; private set; }

    [System.Serializable]
    public class CharacterSkin
    {
        public string skinName = "Skin";
        public Material material;
        public Color previewColor = Color.white;
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
        if (skins == null || skins.Length == 0)
            return $"Skin {index + 1}";

        index = Mathf.Clamp(index, 0, skins.Length - 1);

        if (string.IsNullOrWhiteSpace(skins[index].skinName))
            return $"Skin {index + 1}";

        return skins[index].skinName;
    }

    public Material GetMaterial(int index)
    {
        if (skins == null || skins.Length == 0)
            return null;

        index = Mathf.Clamp(index, 0, skins.Length - 1);
        return skins[index].material;
    }

    public Color GetPreviewColor(int index)
    {
        if (skins == null || skins.Length == 0)
            return Color.white;

        index = Mathf.Clamp(index, 0, skins.Length - 1);

        Color color = skins[index].previewColor;
        color.a = 1f;

        return color;
    }
}