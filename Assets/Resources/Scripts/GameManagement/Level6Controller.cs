using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System; // *** Add System namespace for System.Random ***

public class Level6Controller : LevelController
{
    // === 新增：摄像头动画参数 ===
    public float cameraPanDuration = 1.0f; // 摄像头平移动画时间
    public float cameraPannedXPosition = 8.0f; // 摄像头平移后的X坐标 (根据场景调整)
    private Vector3 initialCameraPosition; // 存储初始摄像头位置
    private Camera mainCamera;
    // === 新增：UIManagement 引用 ===
    private UIManagement uiManagement;
    public List<string> plantCards;
    // === 新增：选卡预览僵尸参数 ===
    public int previewZombieCount = 5; // 预览僵尸数量
    public GameObject previewZombiePrefab; // 用于预览的僵尸预制体 (在Inspector中指定 ZombieNormal)
    public Transform previewSpawnAreaObject; // *** 新增：指定生成区域的GameObject ***
    private List<GameObject> decorativeZombies = new List<GameObject>(); // 存储生成的预览僵尸
    public float previewZombieSpawnDelay = 0.2f; // 每个预览僵尸生成间隔
    // ============================

    // === 新增：画中画相关 ===
    public GameObject opponentViewDisplayObject; // 包含RawImage的UI对象
    public Camera opponentViewCamera;         // 用于观察对手的摄像头
    private bool isOpponentViewActive = false;
    // =======================

    protected override void Start()
    {
        Debug.Log("[Level6] 开始初始化");
        // 查找主摄像头
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            initialCameraPosition = mainCamera.transform.position;
            Debug.Log($"[Level6] 找到主摄像头，初始位置: {initialCameraPosition}");
        }
        else
        {
            Debug.LogError("[Level6] 未找到主摄像头!");
            return; // 如果没有摄像头，后续无法进行
        }

        // === 新增：获取 UIManagement ===
        GameObject uiManagementObject = GameObject.Find("UI Management"); // 假设UI管理器的名称是 "UI Management"
        if (uiManagementObject != null) {
            uiManagement = uiManagementObject.GetComponent<UIManagement>();
        }
        if (uiManagement == null) { // 添加二次检查
            Debug.LogError("[Level6] 未找到 UIManagement 组件!");
        }
        // =============================

        // === 初始化画中画状态 (动态查找) ===
        // 1. 查找 UI 显示对象
        if (opponentViewDisplayObject == null) // 如果 Inspector 未设置
        {
            Debug.Log("[Level6] opponentViewDisplayObject 未在 Inspector 中设置，尝试通过名称 'OpponentViewDisplay' 查找...");
            GameObject foundDisplayObject = GameObject.Find("OpponentViewDisplay"); // 假设 UI 对象名为 "OpponentViewDisplay"
            if (foundDisplayObject != null)
            {
                opponentViewDisplayObject = foundDisplayObject;
                Debug.Log("[Level6] 成功找到 'OpponentViewDisplay'");
            }
            else
            {
                Debug.LogError("[Level6] 未能找到名为 'OpponentViewDisplay' 的 GameObject!");
            }
        }

        // 2. 查找观察摄像头
        if (opponentViewCamera == null) // 如果 Inspector 未设置
        {
             Debug.Log("[Level6] opponentViewCamera 未在 Inspector 中设置，尝试通过名称 'OpponentViewCamera' 查找...");
             GameObject foundCameraObject = GameObject.Find("OpponentViewCamera"); // 假设摄像头对象名为 "OpponentViewCamera"
             if (foundCameraObject != null)
             {
                 opponentViewCamera = foundCameraObject.GetComponent<Camera>();
                 if (opponentViewCamera != null)
                 {
                     Debug.Log("[Level6] 成功找到 'OpponentViewCamera' 并获取其 Camera 组件");
                 }
                 else
                 {
                      Debug.LogError("[Level6] 找到名为 'OpponentViewCamera' 的 GameObject，但其上没有 Camera 组件!");
                 }
             }
             else
             {
                 Debug.LogError("[Level6] 未能找到名为 'OpponentViewCamera' 的 GameObject!");
             }
        }

        // 3. 设置初始状态 (只有在成功找到对象后才执行)
        if (opponentViewDisplayObject != null)
        {
            opponentViewDisplayObject.SetActive(false); // 默认隐藏
            isOpponentViewActive = false;
            Debug.Log("[Level6] 初始化画中画UI为隐藏状态");
        }
        // else // 如果找不到对象，之前的错误日志已经提示

        if (opponentViewCamera != null)
        {
            opponentViewCamera.enabled = false; // 默认禁用渲染
            // 确保禁用 Audio Listener
            AudioListener listener = opponentViewCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }
            Debug.Log("[Level6] 初始化观察摄像头为禁用状态 (并确保AudioListener禁用)");
        }
        // else // 如果找不到对象或组件，之前的错误日志已经提示
        // =============================

        // 启动准备阶段协程
        StartCoroutine(SetupPreGamePhase());
        //startGame(); // 不再直接调用startGame
    }


    private void startGame()
    {
        // 这个方法现在由 SetupPreGamePhase 接管实际的游戏启动逻辑
        Debug.Log("[Level6] startGame 被调用，但实际启动由 SetupPreGamePhase 控制");
    }

    // === 新增：准备阶段协程 ===
    private IEnumerator SetupPreGamePhase()
    {
        Debug.Log("[Level6] === 开始准备阶段 ===");

        // 1. 初始化关卡数据 (这会设置 LevelData)
        init();

        // 2. (未来步骤) 可能需要调用 GameManagement 的部分初始化，加载 SeedBank

        // 3. 播放摄像头向右平移动画
        Debug.Log("[Level6] 开始摄像头向右平移");
        yield return StartCoroutine(AnimateCameraPan(true)); // true表示向右平移
        Debug.Log("[Level6] 摄像头向右平移完成");

        // === 新增：触发 SeedBank 入场动画 ===
        if (uiManagement != null) {
            Debug.Log("[Level6] 触发 SeedBank 入场动画");
            uiManagement.AnimateSeedBankEntrance();
        } else {
             Debug.LogError("[Level6] 无法触发 SeedBank 动画，uiManagement 为空!");
        }
        // =====================================

        // === 新增：生成预览僵尸 ===
        Debug.Log("[Level6] 开始生成预览僵尸");
        yield return StartCoroutine(SpawnIdleZombiesForPreview());
        Debug.Log($"[Level6] 预览僵尸生成完成，共 {decorativeZombies.Count} 个");
        // ==========================

        // 4. (未来步骤) 显示选卡界面 (SeedChooser 上移, SeedBank 下移, 生成 Idle 僵尸)

        // 5. (未来步骤) 等待选卡完成

        // 6. (未来步骤) 隐藏选卡界面 (SeedChooser 下移), 播放摄像头向左平移动画
        // yield return StartCoroutine(AnimateCameraPan(false));

        // 7. (未来步骤) 正式启动游戏逻辑 (调用 GameManagement.awakeAll 或类似方法开始生成阳光、僵尸等)
        Debug.Log("[Level6] === 准备阶段完成（等待选卡确认） ===");
    }
    // ============================

    // === 新增：摄像头动画协程 ===
    private IEnumerator AnimateCameraPan(bool panRight)
    {
        if (mainCamera == null) yield break;

        Vector3 startPos = mainCamera.transform.position;
        // 计算目标位置，只改变X轴
        Vector3 targetPos = panRight ? new Vector3(cameraPannedXPosition, initialCameraPosition.y, initialCameraPosition.z) : initialCameraPosition;

        Debug.Log($"[Level6] 摄像头动画: 从 {startPos} 到 {targetPos}");

        float elapsed = 0f;
        while (elapsed < cameraPanDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cameraPanDuration);
            // 可以使用 SmoothStep 实现缓动效果，或直接用 t 实现匀速
             float smoothT = Mathf.SmoothStep(0f, 1f, t);
             mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            // mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t); // 匀速版本
            yield return null;
        }

        // 确保最终位置精确
        mainCamera.transform.position = targetPos;
         Debug.Log($"[Level6] 摄像头动画完成，当前位置: {mainCamera.transform.position}");
    }
    // ============================

    // === 新增：生成预览僵尸的协程 ===
    private IEnumerator SpawnIdleZombiesForPreview()
    {
        // --- 加载预制体 --- 
        previewZombiePrefab = Resources.Load<GameObject>("Prefabs/Zombies/ZombieNormal");
        // -------------------
        
        if (previewZombiePrefab == null) {
             Debug.LogError("[Level6] 预览僵尸预制体(previewZombiePrefab)加载失败或未指定!");
             yield break;
        }
        // *** 检查生成区域对象 ***
        previewSpawnAreaObject = GameObject.Find("PreviewSpawnArea").transform; 
        if (previewSpawnAreaObject == null) {
             Debug.LogError("[Level6] 预览僵尸生成区域对象(previewSpawnAreaObject)未在Inspector中指定或未找到!"); 
             yield break;
        }
        // *** 尝试获取区域边界 (使用 Collider2D) ***
        Collider2D spawnAreaCollider = previewSpawnAreaObject.GetComponent<Collider2D>(); 
        if (spawnAreaCollider == null) {
            Debug.LogError("[Level6] 预览僵尸生成区域对象上没有找到Collider2D组件来确定边界!"); 
             yield break;
        }
        Bounds spawnBounds = spawnAreaCollider.bounds;
        Debug.Log($"[Level6] 获取到预览僵尸生成边界: Min={spawnBounds.min}, Max={spawnBounds.max}");
        // ==========================
        
        decorativeZombies.Clear(); 
                
        for (int i = 0; i < previewZombieCount; i++)
        {
            // *** 在边界内计算随机位置 (使用 System.Random) ***
            // float randomX = spawnBounds.min.x + ((float)sysRandom.NextDouble() * spawnBounds.size.x);
            // float randomY = spawnBounds.min.y + ((float)sysRandom.NextDouble() * spawnBounds.size.y);
            // *** Create NEW instance inside the loop for debugging ***
            System.Random sysRandom = new System.Random(); 
            // ========================================================
            float randomX = spawnBounds.min.x + ((float)sysRandom.NextDouble() * spawnBounds.size.x); 
            float randomY = spawnBounds.min.y + ((float)sysRandom.NextDouble() * spawnBounds.size.y); 
            Debug.Log($"[Level6] 随机位置 (New System.Random): X={randomX}, Y={randomY}"); // Modified log
            Vector3 spawnPos = new Vector3(randomX, randomY, spawnBounds.center.z); 
            // ====================================================
            
            GameObject zombieInstance = Instantiate(previewZombiePrefab, spawnPos, Quaternion.identity);
            zombieInstance.name = $"PreviewZombie_{i}";
            
            // --- 设置为 Idle 状态 --- 
            var zombieComponent = zombieInstance.GetComponent<Zombie>(); 
            if(zombieComponent != null) 
            {
                 zombieComponent.isPreviewZombie = true; // *** 设置预览标志 ***
                 zombieComponent.speed = 0; // 设置脚本速度
                 // Animator 速度可能不需要设为0了，如果 Idle 动画本身不移动的话
                 // Animator animator = zombieInstance.GetComponent<Animator>();
                 // if (animator != null) animator.speed = 0; 
                 Debug.Log($"[Level6] 设置僵尸 {zombieInstance.name} 脚本速度为0, isPreview={zombieComponent.isPreviewZombie}"); // 更新日志
            } else {
                 Debug.LogWarning($"[Level6] 预览僵尸 {zombieInstance.name} 未找到 Zombie 脚本!");
            }
            // -------------------------
            
            decorativeZombies.Add(zombieInstance);
            
            yield return new WaitForSeconds(previewZombieSpawnDelay);
        }
    }
    // ==============================

    // === 新增：公共方法，用于从外部触发摄像头返回动画 ===
    public void StartCameraReturnAnimation()
    {
        Debug.Log("[Level6] 收到请求，开始摄像头返回动画");
        if (mainCamera != null) {
             StartCoroutine(AnimateCameraPan(false)); // false 表示向左移回初始位置
        }
    }

    // === 新增：销毁预览僵尸的方法 ===
    public void DestroyPreviewZombies()
    {
        Debug.Log($"[Level6] 开始销毁 {decorativeZombies.Count} 个预览僵尸");
        foreach (GameObject zombie in decorativeZombies)
        {
            if (zombie != null) 
            {
                Destroy(zombie);
            }
        }
        decorativeZombies.Clear(); 
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
            plantCards = new List<string>() // 初始化为空，由选卡结果填充
        };
        Debug.Log("[Level6] 关卡数据初始化完成");

        // 设置初始阳光值为50 - 这部分可能也需要移到 GameManagement.awakeAll 之后
        // GameObject.Find("Sun Text").GetComponent<Text>().text = "50";

        // 初始化僵尸管理器的可生成僵尸列表 - 这部分现在不需要立即执行
        // InitializeZombieList();
    }

    private void InitializeZombieList()
    {
        // ... (这个方法暂时不需要在init中调用) ...
        // Debug.Log("[Level6] 初始化可生成僵尸列表");
        
        // // 获取僵尸管理器
        // ZombieManagement zombieManagement = GameObject.Find("Zombie Management")?.GetComponent<ZombieManagement>();
        // if (zombieManagement != null)
        // {
        //     // 加载所有可用的僵尸预制体
        //     List<GameObject> zombieList = new List<GameObject>();
            
        //     // 添加普通僵尸
        //     GameObject normalZombie = Resources.Load<GameObject>("Prefabs/Zombies/ZombieNormal");
        //     if (normalZombie != null)
        //     {
        //         zombieList.Add(normalZombie);
        //         Debug.Log("[Level6] 添加普通僵尸到可生成列表");
        //     }

        //     // 设置僵尸列表
        //     zombieManagement.zombies = zombieList.ToArray();
        //     Debug.Log($"[Level6] 完成僵尸列表初始化，共{zombieList.Count}种僵尸");
        // }
        // else
        // {
        //     Debug.LogError("[Level6] 未找到僵尸管理器");
        // }
    }

    public override void activate()
    {
        // 不在这里启动游戏，避免循环调用
        Debug.Log("[Level6] activate被调用，但不执行任何操作"); //以避免循环调用
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

        // === 新增：检测切换观察视图按键 ===
        if (Input.GetKeyDown(KeyCode.Tab)) // 使用 Tab 键作为示例
        {
            ToggleOpponentView();
        }
        // =============================
    }

    private void GenerateZombies()
        {
        ZombieManagement zombieManagement = GameObject.Find("Zombie Management")?.GetComponent<ZombieManagement>();
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            zombieManagement?.SpawnZombieInRow(0, "ZombieNormal");
        }
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            zombieManagement?.SpawnZombieInRow(1, "ZombieNormal");
        }
        if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            zombieManagement?.SpawnZombieInRow(2, "ZombieNormal");   
        }
        if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            zombieManagement?.SpawnZombieInRow(3, "ZombieNormal");
        }
        if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            zombieManagement?.SpawnZombieInRow(4, "ZombieNormal");
        }
    }

    // === 新增：切换画中画视图方法 ===
    private void ToggleOpponentView()
    {
        // 每次切换前再次检查引用，以防对象在运行时被销毁
        if (opponentViewDisplayObject == null || opponentViewCamera == null)
        {
            // 尝试再次查找，以防它们在 Start 后才被创建
            if (opponentViewDisplayObject == null) {
                GameObject foundDisplayObject = GameObject.Find("OpponentViewDisplay");
                if (foundDisplayObject != null) opponentViewDisplayObject = foundDisplayObject;
            }
            if (opponentViewCamera == null) {
                 GameObject foundCameraObject = GameObject.Find("OpponentViewCamera");
                 if (foundCameraObject != null) opponentViewCamera = foundCameraObject.GetComponent<Camera>();
            }

            // 再次检查是否成功找到
            if (opponentViewDisplayObject == null || opponentViewCamera == null) {
                 Debug.LogError("[Level6] 无法切换观察视图：仍然无法找到 opponentViewDisplayObject 或 opponentViewCamera!");
                 return; // 如果还是找不到，就无法继续
            }
             Debug.LogWarning("[Level6] 在切换时重新找到了视图组件。");
        }


        isOpponentViewActive = !isOpponentViewActive;

        opponentViewDisplayObject.SetActive(isOpponentViewActive);
        opponentViewCamera.enabled = isOpponentViewActive; // 激活/禁用摄像头渲染

        Debug.Log($"[Level6] 切换观察视图: {(isOpponentViewActive ? "开启" : "关闭")}");
    }
    // ==============================
} 