using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class Level6Controller : LevelController
{

    protected override void Start()
    {
        Debug.Log("[Level6] 开始初始化");
        startGame();

    }


    private void startGame()
    {
        Debug.Log("[Level6] 正在启动游戏");
        GameObject gameManagement = GameObject.Find("Game Management");
        if (gameManagement != null)
        {
            Debug.Log("[Level6] 找到Game Management对象");
            

            gameManagement.GetComponent<GameManagement>().awakeAll();
        }
        else
        {
            Debug.LogError("[Level6] 未找到Game Management对象");
        }
    }


    public override void init()
    {
        Debug.Log("[Level6] 初始化关卡数据");
        GameManagement.levelData = new LevelData()
        {
            level = 6,   //关卡编号
            levelName = "Lawn_PVP",   //关卡名称

            mapSuffix = "_Day", //地图图片后缀
            rowCount = 5,       //总共行数
            landRowCount = 5,   //陆地行数
            isDay = true,       //是否白天
            plantingManagementSuffix = "_OriginalLawn",   //对应的种植管理器后缀
            backgroundSuffix = "_Day",   //对应的背景音乐后缀

            //各行僵尸初始Y轴位置
            zombieInitPosY = new List<float> {1.7f, 0.7f, -0.35f, -1.25f, -2.3f },
            //可用植物卡片列表
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash",
                "TorchWood"
            }
        };
        Debug.Log("[Level6] 关卡数据初始化完成");

        // 设置初始阳光值为50
        GameObject.Find("Sun Text").GetComponent<Text>().text = "9999";

        // 初始化僵尸管理器的可生成僵尸列表
        InitializeZombieList();
    }

    private void InitializeZombieList()
    {
        Debug.Log("[Level6] 初始化可生成僵尸列表");
        
        // 获取僵尸管理器
        ZombieManagement zombieManagement = GameObject.Find("Zombie Management")?.GetComponent<ZombieManagement>();
        if (zombieManagement != null)
        {
            // 加载所有可用的僵尸预制体
            List<GameObject> zombieList = new List<GameObject>();
            
            // 添加普通僵尸
            GameObject normalZombie = Resources.Load<GameObject>("Prefabs/Zombies/ZombieNormal");
            if (normalZombie != null)
            {
                zombieList.Add(normalZombie);
                Debug.Log("[Level6] 添加普通僵尸到可生成列表");
            }
            
            // 设置僵尸列表
            zombieManagement.zombies = zombieList.ToArray();
            Debug.Log($"[Level6] 完成僵尸列表初始化，共{zombieList.Count}种僵尸");
        }
        else
        {
            Debug.LogError("[Level6] 未找到僵尸管理器");
        }
    }

    public override void activate()
    {
        // 不在这里启动游戏，避免循环调用
        Debug.Log("[Level6] activate被调用，但不执行任何操作以避免循环调用");
    }

    private void OnDestroy()
    {
        Debug.Log("[Level6] 控制器被销毁");
    }

    // 添加Update方法检测键盘输入
    private void Update()
    {
        // 调用僵尸生成方法
        GenerateZombies();
    }

    private void GenerateZombies()
    {
        ZombieManagement zombieManagement = GameObject.Find("Zombie Management")?.GetComponent<ZombieManagement>();
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            zombieManagement.SpawnZombieInRow(0, "ZombieNormal");
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            zombieManagement.SpawnZombieInRow(1, "ZombieNormal");
        }
        if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            zombieManagement.SpawnZombieInRow(2, "ZombieNormal");   
        }
        if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            zombieManagement.SpawnZombieInRow(3, "ZombieNormal");
        }   
        if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            zombieManagement.SpawnZombieInRow(4, "ZombieNormal");
        }
    }
} 