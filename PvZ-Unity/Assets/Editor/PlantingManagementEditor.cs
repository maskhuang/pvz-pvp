using UnityEngine;
using UnityEditor;

public class PlantingManagementEditor : EditorWindow
{
    [MenuItem("Tools/PvZ/Reorder Plant Rows")]
    static void Init()
    {
        PlantingManagementEditor window = (PlantingManagementEditor)EditorWindow.GetWindow(typeof(PlantingManagementEditor));
        window.titleContent = new GUIContent("行顺序调整工具");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("种植管理器行顺序调整", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("重新排列行顺序"))
        {
            ReorderPlantRows();
        }
    }

    private void ReorderPlantRows()
    {
        // 加载预制体
        string prefabPath = "Assets/Resources/Prefabs/PlantingManagement/PlantingManagement_OriginalLawn.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到预制体", "确定");
            return;
        }

        // 创建预制体的实例
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        
        // 获取所有行组
        Transform plantGroups = instance.transform.Find("PlantGroups");
        if (plantGroups == null)
        {
            DestroyImmediate(instance);
            EditorUtility.DisplayDialog("错误", "未找到PlantGroups", "确定");
            return;
        }

        // 重新排列行的顺序
        Transform[] rows = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            rows[i] = plantGroups.Find($"PlantGroup-Row{i + 1}");
        }

        // 调整行的顺序
        for (int i = 0; i < 5; i++)
        {
            rows[i].SetSiblingIndex(4 - i);
        }

        // 应用修改到预制体
        PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
        
        // 清理
        DestroyImmediate(instance);
        
        EditorUtility.DisplayDialog("完成", "行顺序已调整", "确定");
    }
} 