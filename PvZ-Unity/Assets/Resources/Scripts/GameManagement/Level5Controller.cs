using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class Level5Controller : LevelController
{
    private bool gameStarted = false;

    protected override void Start()
    {
        Debug.Log("[Level5] 开始初始化");
        // 连接到Photon服务器
        if (PhotonNetworkManager.Instance == null)
        {
            Debug.LogError("[Level5] PhotonNetworkManager实例未找到");
            return;
        }

        PhotonNetworkManager.Instance.OnGameStart += OnNetworkGameStart;
        Debug.Log("[Level5] 注册了游戏开始事件");
        PhotonNetworkManager.Instance.Connect();
        Debug.Log("[Level5] 开始连接到服务器");
    }

    private void OnNetworkGameStart()
    {
        Debug.Log("[Level5] 收到游戏开始事件");
        if (!gameStarted)
        {
            gameStarted = true;
            Debug.Log("[Level5] 准备开始游戏");
            // 直接开始游戏
            Invoke("startGame", 0.1f);
        }
    }

    private void startGame()
    {
        Debug.Log("[Level5] 正在启动游戏");
        GameObject gameManagement = GameObject.Find("Game Management");
        if (gameManagement != null)
        {
            Debug.Log("[Level5] 找到Game Management对象，调用awakeAll");
            gameManagement.GetComponent<GameManagement>().awakeAll();
        }
        else
        {
            Debug.LogError("[Level5] 未找到Game Management对象");
        }
    }

    public override void init()
    {
        Debug.Log("[Level5] 初始化关卡数据");
        GameManagement.levelData = new LevelData()
        {
            level = 5,   //关卡编号
            levelName = "阳光普照",   //关卡名称

            mapSuffix = "_Day", //地图图片后缀
            rowCount = 5,       //总共行数
            landRowCount = 5,   //陆地行数
            isDay = true,       //是否白天
            plantingManagementSuffix = "_OriginalLawn",   //对应的种植管理器后缀
            backgroundSuffix = "_Day",   //对应的背景音乐后缀

            //各行僵尸初始Y轴位置
            zombieInitPosY = new List<float> { -2.3f, -1.25f, -0.35f, 0.7f, 1.7f },

            //可用植物卡片列表
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash"
            }
        };
        Debug.Log("[Level5] 关卡数据初始化完成");

        // 设置初始阳光值为50
        GameObject.Find("Sun Text").GetComponent<Text>().text = "50";
    }

    public override void activate()
    {
        // 不在这里启动游戏，避免循环调用
        Debug.Log("[Level5] activate被调用，但不执行任何操作以避免循环调用");
    }

    private void OnDestroy()
    {
        Debug.Log("[Level5] 控制器被销毁");
        if (PhotonNetworkManager.Instance != null)
        {
            PhotonNetworkManager.Instance.OnGameStart -= OnNetworkGameStart;
            Debug.Log("[Level5] 取消注册游戏开始事件");
        }
    }
} 