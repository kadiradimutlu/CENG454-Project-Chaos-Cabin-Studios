using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyGameplayGroundMaterial
{
    [MenuItem("Tools/Apply Dark Gameplay Ground Material")]
    public static void Apply()
    {
        const string folder = "Assets/Materials";
        const string materialPath = "Assets/Materials/Ground_Dark_Navy.mat";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        Color groundColor = new Color(0.025f, 0.075f, 0.16f, 1f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", groundColor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", groundColor);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.18f);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        GameObject plane = GameObject.Find("Plane");

        if (plane == null)
        {
            Debug.LogError("Could not find Plane in the active scene.");
            return;
        }

        Renderer renderer = plane.GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogError("Plane has no Renderer.");
            return;
        }

        renderer.sharedMaterial = material;

        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(renderer);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("Applied Ground_Dark_Navy material to Plane.");
    }
}
