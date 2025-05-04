using UnityEngine;
using UnityEditor;
using System.IO; // Required for Path class
using System.Collections.Generic; // 用于 List

public class SetSpritePivotBottomCenterAccurate
{
    // --- 配置 ---
    // !!! 修改这里为你需要处理的 Sprite 所在的文件夹路径 !!!
    private const string targetFolderPath = "Assets/Temp/png_sequence"; 
    // -----------

    [MenuItem("Tools/Sprites/Set Pivot to Bottom Center (Accurate)")]
    public static void SetPivotForSpritesInFolderAccurate()
    {
        if (!Directory.Exists(targetFolderPath))
        {
            Debug.LogError($"错误：指定的文件夹不存在 '{targetFolderPath}'");
            return;
        }

        Debug.Log($"开始精确处理文件夹: {targetFolderPath}");
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { targetFolderPath });
        int processedCount = 0;
        int modifiedCount = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                if (textureImporter != null && textureImporter.textureType == TextureImporterType.Sprite)
                {
                    processedCount++;
                    bool needsReimport = false;

                    // 获取当前的 spritesheet 数据
                    List<SpriteMetaData> currentSpritesheet = new List<SpriteMetaData>(textureImporter.spritesheet);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); // 获取纹理尺寸需要

                    // 情况1: Single 模式且 spritesheet 为空，需要手动创建
                    if (textureImporter.spriteImportMode == SpriteImportMode.Single && currentSpritesheet.Count == 0 && texture != null)
                    {
                        Debug.Log($"处理 Single Sprite (无现有 MetaData): {path}");
                        SpriteMetaData smd = new SpriteMetaData();
                        smd.name = Path.GetFileNameWithoutExtension(path); // 使用文件名作为默认名
                        smd.rect = new Rect(0, 0, texture.width, texture.height); // 覆盖整个纹理
                        smd.alignment = (int)SpriteAlignment.Custom;
                        smd.pivot = new Vector2(0.5f, 0f);
                        currentSpritesheet.Add(smd);
                        needsReimport = true;
                    }
                    // 情况2: spritesheet 不为空 (无论是 Single 还是 Multiple)
                    else if (currentSpritesheet.Count > 0)
                    {
                         Debug.Log($"处理已有 MetaData ({currentSpritesheet.Count} 个): {path}");
                        // 遍历并修改现有的 MetaData
                        for (int i = 0; i < currentSpritesheet.Count; i++)
                        {
                            SpriteMetaData smd = currentSpritesheet[i]; // 获取副本 (因为是结构体)
                            // 检查是否需要修改
                            if (smd.alignment != (int)SpriteAlignment.Custom || smd.pivot != new Vector2(0.5f, 0f))
                            {
                                smd.alignment = (int)SpriteAlignment.Custom;
                                smd.pivot = new Vector2(0.5f, 0f);
                                currentSpritesheet[i] = smd; // 将修改后的副本写回 List
                                needsReimport = true;
                            }
                        }
                    }

                    // 如果进行了修改，则应用更改
                    if (needsReimport)
                    {
                        // 将修改后的 List 转换回数组并赋值
                        textureImporter.spritesheet = currentSpritesheet.ToArray(); 
                        
                        // 确保 spriteMode 是 Single 或 Multiple
                         if (textureImporter.spriteImportMode == SpriteImportMode.None) {
                             textureImporter.spriteImportMode = SpriteImportMode.Single;
                         }

                        // 使用之前的保存方法
                        EditorUtility.SetDirty(textureImporter);
                        textureImporter.SaveAndReimport();
                        // 或者尝试 AssetDatabase.ImportAsset(...)
                        modifiedCount++;
                        Debug.Log($"--> 已修改并重新导入: {path}");
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"精确处理完成！共检查了 {processedCount} 个 Sprite 文件，修改并重新导入了 {modifiedCount} 个。文件夹: '{targetFolderPath}'");
        AssetDatabase.Refresh();
    }
}
