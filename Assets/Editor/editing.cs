using UnityEngine;
using UnityEditor;

public class BulkShaderSwap
{
    // Change this to the folder you want to target
    private static readonly string targetFolder = "Assets/Morgue Room PBR";

    [MenuItem("Tools/Swap Materials In Folder To Complex Lit")]
    static void SwapShaders()
    {
        Shader target = Shader.Find("Universal Render Pipeline/Complex Lit");
        if (target == null)
        {
            Debug.LogError("Shader not found — check the exact shader name/path.");
            return;
        }

        // "t:Material" filters to materials only; the folder array scopes the search recursively
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { targetFolder });

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && mat.shader != target)
            {
                Undo.RecordObject(mat, "Swap Shader to Complex Lit");
                mat.shader = target;
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"Swapped: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Done. Swapped {count} material(s) in {targetFolder}.");
    }
}