using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

// https://github.com/SaigyoujiYusora/UnityMaterialFullCopy

namespace SoraTools
{
    public class MaterialDuplicator : EditorWindow
{
    private static Object[] selectedMaterials;
    private string suffix = "_Copy";

    // 记录上一次复制的资源路径
    private static List<string> lastCreatedAssets = new List<string>();

    [MenuItem("Assets/材质球完整复制", false, 2000)]
    private static void ShowWindow()
    {
        // 过滤选择的对象，只要材质
        selectedMaterials = Selection.GetFiltered(typeof(Material), SelectionMode.Assets);

        if (selectedMaterials == null || selectedMaterials.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "请至少选择一个材质球", "OK");
            return;
        }

        MaterialDuplicator window = GetWindow<MaterialDuplicator>("复制材质球");
        window.suffix = "_Copy";
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("复制材质球时会一并复制贴图", EditorStyles.boldLabel);
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
        // 清空撤销缓存
        lastCreatedAssets.Clear();

        // 材质复用缓存
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
        lastCreatedAssets.Add(newMaterialPath);

        // 获取贴图
        Shader shader = newMat.shader;
        int propertyCount = ShaderUtil.GetPropertyCount(shader);

        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv) continue;
            string propName = ShaderUtil.GetPropertyName(shader, i);
            Texture tex = newMat.GetTexture(propName);

            if (tex == null) continue;
            string texPath = AssetDatabase.GetAssetPath(tex);

            if (copiedTextures.TryGetValue(texPath, out Texture newTexCached))
            {
                // 触发复用缓存
                newMat.SetTexture(propName, newTexCached);
            }
            else
            {
                string texDir = Path.GetDirectoryName(texPath);
                string texFileName = Path.GetFileNameWithoutExtension(texPath);
                string texExt = Path.GetExtension(texPath);
                        
                string newTexPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(texDir, texFileName + suffix + texExt));

                if (!AssetDatabase.CopyAsset(texPath, newTexPath)) continue;
                AssetDatabase.ImportAsset(newTexPath);
                Texture newTex = AssetDatabase.LoadAssetAtPath<Texture>(newTexPath);
                            
                copiedTextures[texPath] = newTex;
                            
                newMat.SetTexture(propName, newTex);
                            
                lastCreatedAssets.Add(newTexPath);
            }
        }
    }

    [MenuItem("Assets/撤销上一次完整复制", false, 2001)]
    private static void UndoLastDuplication()
    {
        if (lastCreatedAssets.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "没有可撤销的复制操作。", "OK");
            return;
        }

        foreach (string path in lastCreatedAssets)
        {
            AssetDatabase.DeleteAsset(path);
        }

        lastCreatedAssets.Clear();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("撤销完成", "已删除上一次复制生成的材质和贴图。", "OK");
    }
}
}
