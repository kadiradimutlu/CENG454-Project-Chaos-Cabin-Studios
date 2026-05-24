#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class FixBrightMaterials
{
    [MenuItem("Tools/Fix Selected Bright Materials")]
    public static void FixSelectedBrightMaterials()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("FixBrightMaterials: Önce sahnede map objelerini seç.");
            return;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogError("FixBrightMaterials: Universal Render Pipeline/Lit shader bulunamadı.");
            return;
        }

        int fixedCount = 0;

        foreach (GameObject selectedObject in selectedObjects)
        {
            Renderer[] renderers = selectedObject.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;

                foreach (Material material in materials)
                {
                    if (material == null)
                        continue;

                    Undo.RecordObject(material, "Fix Bright Material");

                    material.shader = urpLit;

                    if (material.HasProperty("_BaseColor"))
                    {
                        Color baseColor = material.GetColor("_BaseColor");

                        baseColor.r = Mathf.Min(baseColor.r, 0.75f);
                        baseColor.g = Mathf.Min(baseColor.g, 0.75f);
                        baseColor.b = Mathf.Min(baseColor.b, 0.75f);
                        baseColor.a = 1f;

                        material.SetColor("_BaseColor", baseColor);
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", Color.black);
                    }

                    material.DisableKeyword("_EMISSION");

                    if (material.HasProperty("_Metallic"))
                        material.SetFloat("_Metallic", 0f);

                    if (material.HasProperty("_Smoothness"))
                        material.SetFloat("_Smoothness", 0.2f);

                    EditorUtility.SetDirty(material);
                    fixedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"FixBrightMaterials: {fixedCount} material düzeltildi.");
    }
}

#endif