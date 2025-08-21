using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace SoraTools
{
    public class MaterialDuplicator : EditorWindow
{
    private static Object[] selectedMaterials;
    private string suffix = "_Copy";

    [MenuItem("Assets/材质球完整复制", false, 2000)]
    private static void ShowWindow()
    {
        selectedMaterials = Selection.GetFiltered(typeof(Material), SelectionMode.Assets);

        if (selectedMaterials == null || selectedMaterials.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", "请至少选择一个材质球 (Material)", "OK");
            return;
        }

        MaterialDuplicator window = GetWindow<MaterialDuplicator>("材质球完整复制");
        window.suffix = "_Copy";
        window.Show();
    }

    private void OnGUI()
    {
        // GUILayout.Label("复制材质球时会一并复制贴图", EditorStyles.boldLabel);
        GUILayout.Label("复制材质球时会一并复制贴图");
        suffix = EditorGUILayout.TextField("自定义后缀：", suffix);

        GUILayout.Space(10);

        if (GUILayout.Button("开始复制"))
        {
            if (string.IsNullOrEmpty(suffix)) suffix = "_Copy";
            DuplicateAll(selectedMaterials, suffix);
            Close();
        }
    }

    private static void DuplicateAll(Object[] materials, string suffix)
    {
        Dictionary<string, Texture> copiedTextures = new Dictionary<string, Texture>();

        foreach (Object obj in materials)
        {
            Material mat = obj as Material;
            if (mat != null)
            {
                DuplicateOne(mat, suffix, copiedTextures);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", $"已复制 {materials.Length} 个材质，后缀：{suffix}", "OK");
    }

    private static void DuplicateOne(Material material, string suffix, Dictionary<string, Texture> copiedTextures)
    {
        string materialPath = AssetDatabase.GetAssetPath(material);
        string directory = Path.GetDirectoryName(materialPath);
        string fileName = Path.GetFileNameWithoutExtension(materialPath);
        string newMaterialPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, fileName + suffix + ".mat"));

        Material newMat = new Material(material);
        AssetDatabase.CreateAsset(newMat, newMaterialPath);

        Shader shader = newMat.shader;
        int propertyCount = ShaderUtil.GetPropertyCount(shader);

        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                string propName = ShaderUtil.GetPropertyName(shader, i);
                Texture tex = newMat.GetTexture(propName);

                if (tex != null)
                {
                    string texPath = AssetDatabase.GetAssetPath(tex);

                    if (copiedTextures.TryGetValue(texPath, out Texture newTexCached))
                    {
                        newMat.SetTexture(propName, newTexCached);
                    }
                    else
                    {
                        string texDir = Path.GetDirectoryName(texPath);
                        string texFileName = Path.GetFileNameWithoutExtension(texPath);
                        string texExt = Path.GetExtension(texPath);
                        string newTexPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(texDir, texFileName + suffix + texExt));

                        AssetDatabase.CopyAsset(texPath, newTexPath);
                        AssetDatabase.ImportAsset(newTexPath);

                        Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(newTexPath);
                        
                        copiedTextures[texPath] = newTex;
                        newMat.SetTexture(propName, newTex);
                    }
                }
            }
        }
    }
}
}