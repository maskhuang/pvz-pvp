using UnityEngine;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;

public class ZombieManagement : MonoBehaviour
{
    public GameObject[] zombies;   // 可生成僵尸列表
    private Dictionary<string, int> zombiesName = new Dictionary<string, int>();   // 僵尸名称与对象索引的字典
    private int zombieNumAll = 0;  // 当前存活的僵尸总数
    private List<int> rowList = new List<int>();  // 可生成僵尸行列表
    private float initPos_x = 10.0f;  // 僵尸初始X坐标
    private PhotonView photonView;  // Photon视图组件
    private AudioSource audioSource;  // 音频源组件
    private DecreasingSlider flagMeter;  // 关卡进度条
    private bool isOver = false;  // 关卡是否结束

    private void Awake()
    {
        Debug.Log("[Zombie] ZombieManagement正在初始化");
        
        // 获取组件
        photonView = GetComponent<PhotonView>();
        audioSource = GetComponent<AudioSource>();
        flagMeter = GameObject.Find("FlagMeter-Slider")?.GetComponent<DecreasingSlider>();
        
        if (photonView == null)
        {
            Debug.LogError("[Zombie] 未找到PhotonView组件，请确保已添加到GameObject上");
            photonView = gameObject.AddComponent<PhotonView>();
            if (photonView != null)
            {
                Debug.Log("[Zombie] 自动添加PhotonView组件成功");
                photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
                photonView.ObservedComponents = new List<Component> { this };
            }
        }
        
        // 初始化僵尸名称字典
        if (zombies != null)
        {
            for (int i = 0; i < zombies.Length; i++)
            {
                if (zombies[i] != null)
                {
                    zombiesName.Add(zombies[i].name, i);
                    Debug.Log($"[Zombie] 添加僵尸类型: {zombies[i].name}");
                }
            }
        }
        else
        {
            Debug.LogError("[Zombie] zombies数组未设置，请在Unity编辑器中设置");
        }
    }

    public void activate()
    {
        Debug.Log("[Zombie] 开始生成僵尸");
        Debug.Log($"[Zombie] 当前玩家角色: {(PhotonNetwork.IsMasterClient ? "房主" : "访客")}");
        
        // 初始化可生成僵尸的行列表
        initRowList();
        
        // 只有房主负责生成僵尸
        if (PhotonNetwork.IsMasterClient)
        {
            // 从JSON文件加载僵尸生成数据
            TextAsset zombieDataJson = Resources.Load<TextAsset>($"Json/ZombieData/Level{GameManagement.levelData.level}");
            if (zombieDataJson != null)
            {
                Debug.Log("[Zombie] 成功加载僵尸生成数据");
                try
                {
                    TimeNodes timeNodes = JsonUtility.FromJson<TimeNodes>(zombieDataJson.text);
                    if (timeNodes != null && timeNodes.info != null && timeNodes.info.Count > 0)
                    {
                        Debug.Log($"[Zombie] 成功解析JSON数据，节点数量: {timeNodes.info.Count}");
                        StartCoroutine(SpawnZombiesCoroutine(timeNodes));
                    }
                    else
                    {
                        Debug.LogError("[Zombie] JSON数据解析失败或节点数量为0");
                        StartCoroutine(FallbackZombieSpawn());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Zombie] JSON解析出错: {e.Message}");
                    StartCoroutine(FallbackZombieSpawn());
                }
            }
            else
            {
                Debug.LogError("[Zombie] 无法加载僵尸生成数据，使用后备方案");
                StartCoroutine(FallbackZombieSpawn());
            }
        }
        else
        {
            Debug.Log("[Zombie] 非房主，等待接收僵尸生成消息");
        }
    }

    private void initRowList()
    {
        rowList.Clear();
        for (int i = 0; i < GameManagement.levelData.landRowCount; i++)
        {
            rowList.Add(i);
        }
        Debug.Log($"[Zombie] 初始化行列表，可用行数: {rowList.Count}");
    }

    private IEnumerator SpawnZombiesCoroutine(TimeNodes timeNodes)
    {
        Debug.Log($"[Zombie] 开始生成僵尸波次，总节点数: {timeNodes.info.Count}");
        int nodeIndex = 0;
        
        foreach (var node in timeNodes.info)
        {
            nodeIndex++;
            Debug.Log($"[Zombie] 第{nodeIndex}个节点: 等待 {node.deltaTime} 秒后生成 {node.number} 个 {node.zombie}");
            yield return new WaitForSeconds(node.deltaTime);

            if (node.isWave)
            {
                if (node.isFinalWave)
                {
                    Debug.Log("[Zombie] 最终波次!");
                    // TODO: 显示最终波次字幕
                }
                else
                {
                    Debug.Log("[Zombie] 大波僵尸来袭!");
                    // TODO: 显示大波僵尸字幕
                }
            }

            // 生成僵尸
            for (int i = 0; i < node.number; i++)
            {
                if (rowList.Count == 0) initRowList();
                
                int rowIndex = UnityEngine.Random.Range(0, rowList.Count);
                int row = rowList[rowIndex];
                rowList.RemoveAt(rowIndex);
                
                // 通过RPC同步僵尸生成
                photonView.RPC("SpawnZombieRPC", RpcTarget.All, row, node.zombie);
                
                yield return new WaitForSeconds(0.5f);
            }

            // 更新进度条
            if (flagMeter != null)
            {
                float progress = 1f - ((float)nodeIndex / timeNodes.info.Count);
                flagMeter.setValue(progress);
            }
        }
        
        isOver = true;
        Debug.Log("[Zombie] 所有波次生成完成");
    }

    [PunRPC]
    private void SpawnZombieRPC(int row, string zombieType)
    {
        Debug.Log($"[Zombie] 收到RPC请求：在第 {row} 行生成 {zombieType}");
        SpawnZombieInRow(row, zombieType);
    }

    public void SpawnZombieInRow(int row, string zombieType)
    {
        Debug.Log($"[Zombie] 在第 {row} 行生成 {zombieType}");
        
        try 
        {
            Vector3 position = new Vector3(initPos_x, GameManagement.levelData.zombieInitPosY[row], 0);
            // 修改预制体路径，确保从Resources文件夹加载
            string prefabPath = $"Prefabs/Zombies/{zombieType}";
            Debug.Log($"[Zombie] 尝试加载预制体: {prefabPath}");
            
            // 先检查预制体是否存在
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Zombie] 无法加载预制体，路径: {prefabPath}");
                return;
            }
            
            GameObject zombie = PhotonNetwork.Instantiate(prefabPath, position, Quaternion.Euler(0, 0, 0));
            
            if (zombie != null)
            {
                Zombie zombieComponent = zombie.GetComponent<Zombie>();
                if (zombieComponent != null)
                {
                    zombieComponent.setPosRow(row);
                    zombieComponent.cancelSleep();
                    zombieNumAll++;
                    Debug.Log($"[Zombie] 僵尸生成成功，当前存活数量: {zombieNumAll}");
                    
                    // 播放音效
                    if (audioSource != null && zombieNumAll == 1)
                    {
                        audioSource.Play();
                        InvokeRepeating("playGroanSound", 0, 5f);
                    }
                }
                else
                {
                    Debug.LogError($"[Zombie] 预制体 {zombieType} 缺少Zombie组件");
                }
            }
            else
            {
                Debug.LogError($"[Zombie] 无法实例化预制体 {zombieType}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Zombie] 生成僵尸时发生错误: {e.Message}\n{e.StackTrace}");
        }
    }

    private void playGroanSound()
    {
        if (audioSource != null)
        {
            int rand = UnityEngine.Random.Range(1, 7);
            audioSource.PlayOneShot(Resources.Load<AudioClip>($"Sounds/Zombies/groan{rand}"));
        }
    }

    public void minusZombieNumAll()
    {
        zombieNumAll--;
        Debug.Log($"[Zombie] 僵尸数量减少，当前存活数量: {zombieNumAll}");
        
        if (zombieNumAll <= 0 && isOver)
        {
            Debug.Log("[Zombie] 所有僵尸已消灭，游戏胜利");
            GameObject.Find("Game Management")?.GetComponent<GameManagement>()?.win();
        }
    }

    private IEnumerator FallbackZombieSpawn()
    {
        Debug.Log("[Zombie] 使用后备方案生成僵尸");
        for (int wave = 0; wave < 10; wave++)
        {
            int zombieCount = UnityEngine.Random.Range(1, 4);
            Debug.Log($"[Zombie] 后备方案：第{wave + 1}波生成{zombieCount}个僵尸");
            
            for (int i = 0; i < zombieCount; i++)
            {
                if (rowList.Count == 0) initRowList();
                
                int rowIndex = UnityEngine.Random.Range(0, rowList.Count);
                int row = rowList[rowIndex];
                rowList.RemoveAt(rowIndex);
                
                SpawnZombieInRow(row, "ZombieNormal");
                yield return new WaitForSeconds(0.5f);
            }
            
            yield return new WaitForSeconds(20f);
        }
        
        isOver = true;
    }

    public void generateFunc(int row, string zombieType)
    {
        Debug.Log($"[Zombie] generateFunc被调用：行={row}，僵尸类型={zombieType}");
        photonView.RPC("SpawnZombieRPC", RpcTarget.All, row, zombieType);
    }

    public void createGhost(int row)
    {
        Debug.Log($"[Zombie] createGhost被调用：行={row}");
        photonView.RPC("SpawnZombieRPC", RpcTarget.All, row, "GhostZombie");
    }

    public void createZombieByGod(int row, string zombieType)
    {
        Debug.Log($"[Zombie] createZombieByGod被调用：行={row}，僵尸类型={zombieType}");
        photonView.RPC("SpawnZombieRPC", RpcTarget.All, row, zombieType);
    }
}

[System.Serializable]
public class TimeNode
{
    public float deltaTime;  // 多长时间后开始下一波进攻
    public bool isWave;  // 是否为一波
    public bool isFinalWave;   // 是否为最后一波
    public int number;   // 僵尸数量
    public string zombie;   // 僵尸名称
}

[System.Serializable]
public class TimeNodes
{
    public List<TimeNode> info;
} 