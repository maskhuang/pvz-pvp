using UnityEngine;
using UnityEditor;
using Photon.Pun;
using System.Collections.Generic;

public class PhotonViewAdder : EditorWindow
{
    private DefaultAsset targetFolder;
    private bool includeSubfolders = true;
    private bool onlyZombiePrefabs = true;
    private Vector2 scrollPosition;
    private List<GameObject> modifiedPrefabs = new List<GameObject>();

    [MenuItem("Tools/Photon/Add PhotonView to Prefabs")]
    static void Init()
    {
        PhotonViewAdder window = (PhotonViewAdder)EditorWindow.GetWindow(typeof(PhotonViewAdder));
        window.titleContent = new GUIContent("PhotonView添加工具");
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("批量添加PhotonView组件", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "目标文件夹", targetFolder, typeof(DefaultAsset), false);
        
        includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);
        onlyZombiePrefabs = EditorGUILayout.Toggle("仅处理僵尸预制体", onlyZombiePrefabs);

        EditorGUILayout.Space();

        if (GUILayout.Button("添加PhotonView组件"))
        {
            if (targetFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(targetFolder);
                ProcessPrefabsInFolder(folderPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "请选择目标文件夹", "确定");
            }
        }

        EditorGUILayout.Space();
        
        // 显示处理结果
        if (modifiedPrefabs.Count > 0)
        {
            GUILayout.Label("已处理的预制体：", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var prefab in modifiedPrefabs)
            {
                EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            }
            
            EditorGUILayout.EndScrollView();
        }
    }

    private void ProcessPrefabsInFolder(string folderPath)
    {
        modifiedPrefabs.Clear();
        string[] prefabGuids;
        
        if (includeSubfolders)
        {
            prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        }
        else
        {
            prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            // 过滤掉子文件夹中的预制体
            prefabGuids = System.Array.FindAll(prefabGuids, guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return System.IO.Path.GetDirectoryName(path) == folderPath;
            });
        }

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                // 如果只处理僵尸预制体，检查是否包含Zombie组件
                if (onlyZombiePrefabs && !prefab.GetComponent<Zombie>())
                {
                    continue;
                }

                // 检查是否已经有PhotonView组件
                if (!prefab.GetComponent<PhotonView>())
                {
                    // 创建预制体的实例
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    
                    // 添加PhotonView组件
                    PhotonView photonView = instance.AddComponent<PhotonView>();
                    photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
                    photonView.ObservedComponents = new List<Component> { instance.GetComponent<Zombie>() };
                    
                    // 应用修改到预制体
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                    
                    // 销毁实例
                    DestroyImmediate(instance);
                    
                    modifiedPrefabs.Add(prefab);
                }
            }
        }

        if (modifiedPrefabs.Count > 0)
        {
            EditorUtility.DisplayDialog("完成", 
                $"成功添加PhotonView组件到{modifiedPrefabs.Count}个预制体", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("完成", 
                "没有找到需要处理的预制体", "确定");
        }
    }
} 