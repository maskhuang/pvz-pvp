using UnityEngine;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using static System.Math;
using Random = UnityEngine.Random;
public class ZombieManagement : MonoBehaviour
{
    public GameObject[] zombies;   // 可生成僵尸列表
    private Dictionary<string, int> zombiesName = new Dictionary<string, int>();   // 僵尸名称与对象索引的字典
    private int zombieNumAll = 0;  // 当前存活的僵尸总数
    private List<int> rowList = new List<int>();  // 可生成僵尸行列表
    private float initPos_x = 5.0f;  // 僵尸初始X坐标
    private PhotonView photonView;  // Photon视图组件
    private AudioSource audioSource;  // 音频源组件
    private DecreasingSlider flagMeter;  // 关卡进度条
    private bool isOver = false;  // 关卡是否结束

    private int waveCount = 0;  // 当前波次

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
            Debug.Log("[Zombie] 房主开始生成僵尸");
            StartCoroutine(StartZombieGenerationCoroutine());
            
        }
        else
        {
            Debug.Log("[Zombie] 非房主，等待房主的生成指令");
        }
    }
    private IEnumerator StartZombieGenerationCoroutine()
    {
        Debug.Log("[Zombie] 开始僵尸生成循环");
        
        // 初始等待一段时间，让玩家准备
        yield return new WaitForSeconds(30f);
        
        while(!isOver)
        {
            photonView.RPC("StartZombieGeneration", RpcTarget.All);
            yield return new WaitForSeconds(20f);
        }
    }

    [PunRPC]
    private void StartZombieGeneration()
    {
        Debug.Log("[Zombie] 收到房主的生成指令，开始生成僵尸");
        SpawnZombiesCoroutine();
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

    private void SpawnZombiesCoroutine()
    {
        Debug.Log($"[Zombie] 开始生成僵尸波次，当前波次：{waveCount}");
        
        
        int zombieNumber = (int)(1+waveCount/3);
        Debug.Log($"[Zombie] 第{waveCount}波开始，将生成{zombieNumber}个僵尸");

        // 生成本波次的僵尸
        while (zombieNumber > 0)
        {
            // 随机选择僵尸类型
            int index = Random.Range(0, zombies.Length);
            string zombieType = zombies[index].name;
                
            // 随机选择行
            int row = Random.Range(0, GameManagement.levelData.landRowCount);
                
            // 生成僵尸
            SpawnZombieInRow(row, zombieType);
            zombieNumber--;
        }
        waveCount ++;
    }

    public void SpawnZombieInRow(int row, string zombieType)
    {
        Debug.Log($"[Zombie] 在第 {row} 行生成 {zombieType}");
        
        try 
        {
            Vector3 position = new Vector3(initPos_x, GameManagement.levelData.zombieInitPosY[row], 0);
            
            string prefabPath = $"Prefabs/Zombies/{zombieType}";
            Debug.Log($"[Zombie] 尝试加载预制体: {prefabPath}");
            
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Zombie] 无法加载预制体，路径: {prefabPath}");
                return;
            }
            
            // 直接使用普通的Instantiate生成僵尸
            GameObject zombie = Instantiate(prefab, position, Quaternion.Euler(0, 0, 0));
            
            if (zombie != null)
            {
                Zombie zombieComponent = zombie.GetComponent<Zombie>();
                if (zombieComponent != null)
                {
                    // 先设置行号，这会影响渲染顺序
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
            
            yield return new WaitForSeconds(10f);
        }
        
        isOver = true;
    }

    public void generateFunc(int row, string zombieType)
    {
        Debug.Log($"[Zombie] generateFunc被调用：行={row}，僵尸类型={zombieType}");
        // 使用PhotonNetwork.Instantiate生成需要同步的僵尸
        string prefabPath = $"Prefabs/Zombies/{zombieType}";
        Vector3 position = new Vector3(initPos_x, GameManagement.levelData.zombieInitPosY[row], 0);
        GameObject zombie = PhotonNetwork.Instantiate(prefabPath, position, Quaternion.Euler(0, 0, 0));
        
        if (zombie != null)
        {
            Zombie zombieComponent = zombie.GetComponent<Zombie>();
            if (zombieComponent != null)
            {
                zombieComponent.setPosRow(row);
                zombieComponent.cancelSleep();
                zombieNumAll++;
            }
        }
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

    // 用于在僵尸死亡时通知对方生成僵尸
    public void OnZombieDeath(int row, string zombieType)
    {
        Debug.Log($"[Zombie] 僵尸死亡事件触发：行={row}，类型={zombieType}，IsMine={photonView.IsMine}");
                    // 通知对方在对应行生成僵尸
        photonView.RPC("SpawnZombieOnDeath", RpcTarget.Others, row, zombieType);
    }

    [PunRPC]
    private void SpawnZombieOnDeath(int row, string zombieType)
    {
        Debug.Log($"[Zombie] 收到僵尸死亡RPC：行={row}，类型={zombieType}");
        // 在对方场地生成僵尸
        SpawnZombieInRow(row, zombieType);
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