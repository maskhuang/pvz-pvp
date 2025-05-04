using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SetSpriteRectSize
{
    // --- 配置 ---
    private const string targetFolderPath = "Assets/Temp/png_sequence"; // 确认这是正确的路径
    private const float targetWidth = 187f;
    private const float targetHeight = 179f;
    // -----------

    [MenuItem("Tools/Sprites/Set Rect Size (187x179) for Folder")]
    public static void SetRectSizeForSpritesInFolder()
    {
        if (!Directory.Exists(targetFolderPath))
        {
            Debug.LogError($"错误：指定的文件夹不存在 '{targetFolderPath}'");
            return;
        }

        Debug.Log($"开始为文件夹设置固定尺寸 (W:{targetWidth}, H:{targetHeight}): {targetFolderPath}");
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

                    List<SpriteMetaData> currentSpritesheet = new List<SpriteMetaData>(textureImporter.spritesheet);
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); // 仍然需要，以防万一

                    // 情况1: Single 模式且 spritesheet 为空，需要手动创建
                    if (textureImporter.spriteImportMode == SpriteImportMode.Single && currentSpritesheet.Count == 0 && texture != null)
                    {
                        Debug.Log($"处理 Single Sprite (无现有 MetaData)，设置尺寸: {path}");
                        SpriteMetaData smd = new SpriteMetaData();
                        smd.name = Path.GetFileNameWithoutExtension(path);
                        // **重要: 创建时即使用目标尺寸，原点设为 (0,0)**
                        // 注意：如果原始纹理小于目标尺寸，这仍可能导致问题
                        // 你可能需要根据实际情况调整原点 x, y
                        smd.rect = new Rect(0, 0, targetWidth, targetHeight); 
                        // 保留之前的 Pivot 设置逻辑（虽然你说Pivot没问题，但保持一致性）
                        smd.alignment = (int)SpriteAlignment.Custom; // 使用 (int) 转换以兼容旧版本
                        smd.pivot = new Vector2(0.5f, 0f); // Bottom Center

                        currentSpritesheet.Add(smd);
                        needsReimport = true;
                    }
                    // 情况2: spritesheet 不为空
                    else if (currentSpritesheet.Count > 0)
                    {
                         Debug.Log($"处理已有 MetaData ({currentSpritesheet.Count} 个)，检查尺寸: {path}");
                        for (int i = 0; i < currentSpritesheet.Count; i++)
                        {
                            SpriteMetaData smd = currentSpritesheet[i];
                            Rect originalRect = smd.rect;

                            // 检查尺寸是否需要修改
                            if (originalRect.width != targetWidth || originalRect.height != targetHeight)
                            {
                                // 将 x, y 设置为 0，并修改 width 和 height
                                smd.rect = new Rect(0f, 0f, targetWidth, targetHeight); 
                                
                                // 如果需要，也可以在此处强制重置 Pivot 和 Alignment
                                // smd.alignment = (int)SpriteAlignment.Custom;
                                // smd.pivot = new Vector2(0.5f, 0f);

                                currentSpritesheet[i] = smd;
                                needsReimport = true;
                            }
                        }
                    }

                    // 如果进行了修改，则应用更改
                    if (needsReimport)
                    {
                        textureImporter.spritesheet = currentSpritesheet.ToArray();
                        
                         if (textureImporter.spriteImportMode == SpriteImportMode.None) {
                             textureImporter.spriteImportMode = SpriteImportMode.Single;
                         }

                        EditorUtility.SetDirty(textureImporter);
                        textureImporter.SaveAndReimport();
                        modifiedCount++;
                        Debug.Log($"--> 已修改尺寸并重新导入: {path}");
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"尺寸设置完成！共检查了 {processedCount} 个 Sprite 文件，修改并重新导入了 {modifiedCount} 个。文件夹: '{targetFolderPath}'");
        AssetDatabase.Refresh();
    }
}
