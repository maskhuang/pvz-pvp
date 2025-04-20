using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(PhotonView))] // Ensure PhotonView for RPCs
public class Level5Controller : LevelController
{
    // --- Game State ---
    private bool preGameSetupStarted = false;
    private bool isLocalPlayerReady = false;
    private bool isOpponentReady = false;
    private bool coreGameStarted = false; // To prevent starting awakeAll multiple times
    
    // --- References & Config ---
    private PhotonView photonView;
    private UIManagement uiManagement; // Added reference
    private SeedChooserManager seedChooserManager; // ADD THIS LINE - Restore to private
    private GameManagement gameManagement; // Added reference
    private TextMeshProUGUI selectionTimerText; // ADD THIS LINE - Reference to TMP text
    public float selectionCountdownDuration = 30.0f; // Countdown time
    private float currentCountdown;
    private Coroutine countdownCoroutine;

    // --- Copied/Adapted from Level 6 ---
    public float cameraPanDuration = 1.0f; 
    public float cameraPannedXPosition = 3.3f; 
    private Vector3 initialCameraPosition; 
    private Camera mainCamera;
    public int previewZombieCount = 5; 
    public GameObject previewZombiePrefab; 
    public Transform previewSpawnAreaObject; 
    private List<GameObject> decorativeZombies = new List<GameObject>(); 
    public float previewZombieSpawnDelay = 0.2f; 
    // --- End Copied ---

    protected override void Start()
    {
        Debug.Log("[Level5] 开始初始化");
        // 连接到Photon服务器
        if (PhotonNetworkManager.Instance == null)
        {
            Debug.LogError("[Level5] PhotonNetworkManager实例未找到");
            return;
        }

        // Subscribe to the event indicating both players are in the room and ready to start setup
        PhotonNetworkManager.Instance.OnGameStart += StartPreGameSetup;
        Debug.Log("[Level5] 注册了游戏开始事件 (OnGameStart)");
        
        // Get essential components
        photonView = GetComponent<PhotonView>();
        mainCamera = Camera.main;
        GameObject gmObject = GameObject.Find("Game Management");
        if (gmObject != null) {
            gameManagement = gmObject.GetComponent<GameManagement>();
            // Assuming UIManagement and SeedChooserManager are findable/accessible
            uiManagement = FindObjectOfType<UIManagement>(); 
        } else {
            Debug.LogError("[Level5] 未找到 Game Management 对象!");
        }
        if (mainCamera != null) initialCameraPosition = mainCamera.transform.position;
        else Debug.LogError("[Level5] 未找到主摄像头!");
        if (uiManagement == null) Debug.LogError("[Level5] 未找到 UIManagement 对象!");
        
        PhotonNetworkManager.Instance.Connect();
        Debug.Log("[Level5] 开始连接到服务器");
    }

    // Called by PhotonNetworkManager when both players are ready
    private void StartPreGameSetup()
    {
        Debug.Log("[Level5] 收到双方准备就绪信号");
        if (!preGameSetupStarted)
        {
            preGameSetupStarted = true;
            Debug.Log("[Level5] 启动选卡准备阶段协程");
            StartCoroutine(SetupPreGamePhase());
        }
    }

    private void startGame()
    {
        Debug.Log("[Level5] startGame 方法被调用 (现在由 CheckStartGameCondition 控制实际启动)");
        GameObject gameManagement = GameObject.Find("Game Management");
        if (gameManagement != null)
        {
            Debug.Log("[Level5] 找到Game Management对象");

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
            }
        };
        Debug.Log("[Level5] 关卡数据初始化完成");

        // 设置初始阳光值为50
        GameObject.Find("Sun Text").GetComponent<Text>().text = "150";

        // 初始化僵尸管理器的可生成僵尸列表
        InitializeZombieList();
    }

    private void InitializeZombieList()
    {
        Debug.Log("[Level5] 初始化可生成僵尸列表");
        
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
                Debug.Log("[Level5] 添加普通僵尸到可生成列表");
            }
            
            // 设置僵尸列表
            zombieManagement.zombies = zombieList.ToArray();
            Debug.Log($"[Level5] 完成僵尸列表初始化，共{zombieList.Count}种僵尸");
        }
        else
        {
            Debug.LogError("[Level5] 未找到僵尸管理器");
        }
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
            // Unsubscribe from the new event
            PhotonNetworkManager.Instance.OnGameStart -= StartPreGameSetup;
            Debug.Log("[Level5] 取消注册游戏开始事件 (OnGameStart)");
        }
    }

    // Adapted from Level 6
    private IEnumerator SetupPreGamePhase()
    {
        Debug.Log("[Level5] === 开始选卡准备阶段 ===");
        init(); // Initialize LevelData

        Debug.Log("[Level5] 开始摄像头向右平移");
        yield return StartCoroutine(AnimateCameraPan(true)); 
        Debug.Log("[Level5] 摄像头向右平移完成");
        seedChooserManager = FindObjectOfType<SeedChooserManager>(true);

        if (uiManagement != null) {
            Debug.Log("[Level5] 触发 SeedBank 入场动画");
            uiManagement.AnimateSeedBankEntrance();
        } else {
             Debug.LogError("[Level5] 无法触发 SeedBank 动画，uiManagement 为空!");
        }

        Debug.Log("[Level5] 开始生成预览僵尸");
        yield return StartCoroutine(SpawnIdleZombiesForPreview());
        Debug.Log($"[Level5] 预览僵尸生成完成，共 {decorativeZombies.Count} 个");
        
        // --- 显示选卡界面 ---
        if (seedChooserManager != null)
        {
             Debug.Log("[Level5] 显示 SeedChooser UI");
             seedChooserManager.gameObject.SetActive(true); 
             // TODO: Add entrance animation for SeedChooser if desired
        } else {
             Debug.LogError("[Level5] CRITICAL: SetupPreGamePhase 无法找到 SeedChooserManager 对象! 选卡阶段无法继续。");
        }
        // --------------------
        
        // --- 启动倒计时 ---
        currentCountdown = selectionCountdownDuration;
        // Get reference to TMP text from UIManagement
        if (uiManagement != null && uiManagement.countdownTextTMP != null)
        {
            selectionTimerText = uiManagement.countdownTextTMP;
            selectionTimerText.gameObject.SetActive(true);
            countdownCoroutine = StartCoroutine(CountdownTimer());
            Debug.Log("[Level5] 选卡倒计时开始");
        } else {
             Debug.LogError("[Level5] 无法启动倒计时，UIManagement 或其 countdownTextTMP 为空!");
        }
        // --------------------
        
        Debug.Log("[Level5] === 选卡准备阶段完成（等待玩家确认） ===");
    }
    
    // --- Countdown Logic ---
    private IEnumerator CountdownTimer()
    {
        // DEBUG: Log when the timer starts
        Debug.Log("[Level5] CountdownTimer Coroutine Started."); 
        while (currentCountdown > 0)
        {
            currentCountdown -= Time.deltaTime;
            if (selectionTimerText != null)
            {
                selectionTimerText.text = Mathf.CeilToInt(currentCountdown).ToString();
            }
            yield return null;
        }

        // DEBUG: Log when the while loop finishes
        Debug.Log($"[Level5] CountdownTimer loop finished. currentCountdown={currentCountdown}");

        // Time's up!
        Debug.Log("[Level5] 选卡倒计时结束!");
        if (selectionTimerText != null) selectionTimerText.gameObject.SetActive(false);

        // DEBUG: Log the state right before the check
        Debug.Log($"[Level5] Timer Ended. Checking condition: !isLocalPlayerReady = {!isLocalPlayerReady}");

        if (!isLocalPlayerReady) // 检查本地玩家是否已手动确认
        {
            // DEBUG: Log entering the 'if' block
            Debug.Log("[Level5] Condition met: !isLocalPlayerReady is true. Calling ForceConfirmSelection...");
            Debug.Log("[Level5] 本地玩家超时未确认，强制确认");
            ForceConfirmSelection(); // 调用强制确认方法
        } else {
            // DEBUG: Log if the 'if' condition was false
            Debug.Log("[Level5] Condition NOT met: isLocalPlayerReady was already true.");
        }
        // DEBUG: Log after the check
        Debug.Log("[Level5] CountdownTimer Coroutine Ended.");
    }
    
    private void StopCountdown() {
        if (countdownCoroutine != null) {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
            Debug.Log("[Level5] 倒计时协程已停止。");
        }
        if (selectionTimerText != null) {
             selectionTimerText.gameObject.SetActive(false);
        }
    }

    // Called locally if time runs out
    private void ForceConfirmSelection() {
         if (seedChooserManager != null && !isLocalPlayerReady) {
              // We might skip the SeedChooserManager call if it makes things complex,
              // and just directly call PlayerConfirmedSelection locally.
              // seedChooserManager.ConfirmSelection(); // This might trigger unwanted UI hide?
              Debug.Log("[Level5] 强制调用 PlayerConfirmedSelection");
              PlayerConfirmedSelection(); 
         }
    }

    // --- Player Readiness and Game Start Logic ---
    
    // Called by SeedChooserManager when local player confirms
    public void PlayerConfirmedSelection() {
        if (isLocalPlayerReady || coreGameStarted) return; // Prevent double calls

        Debug.Log("[Level5] 本地玩家已确认选卡");
        isLocalPlayerReady = true;

        // Trigger local visual feedback immediately
        DestroyPreviewZombies();
        StartCameraReturnAnimation();
        // Optionally hide SeedChooser UI locally immediately
        if(seedChooserManager != null) seedChooserManager.gameObject.SetActive(false); 

        // Inform the other player
        Debug.Log("[Level5] 发送 RPC_PlayerReady");
        // --- DEBUGGING --- Add checks before sending RPC
        if (photonView == null) { 
            Debug.LogError("[Level5] photonView is NULL before sending RPC_PlayerReady!"); 
            return; 
        }
        Debug.Log($"[Level5] Before RPC - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}, IsConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}");
        // --- END DEBUGGING ---
        photonView.RPC("RPC_PlayerReady", RpcTarget.Others);

        // Check if we can start the game now
        CheckStartGameCondition();
    }

    [PunRPC]
    void RPC_PlayerReady() {
        Debug.Log("[Level5] 收到 RPC_PlayerReady");
        isOpponentReady = true;
        // Opponent confirmed, check if we can start
        CheckStartGameCondition();
    }

    void CheckStartGameCondition() {
        if (isLocalPlayerReady && isOpponentReady && !coreGameStarted) {
            coreGameStarted = true; // Set flag immediately
            Debug.Log("[Level5] 条件满足！双方玩家均已准备就绪，开始游戏！");
            
            StopCountdown(); // Stop timer if running
            
            // Make sure both cameras are returning (especially if one timed out)
            if(!isLocalPlayerReady) StartCameraReturnAnimation(); // Ensure local camera returns if timed out
            // We assume the opponent's camera return was triggered by their own confirmation or timeout RPC
            
            // Start the actual game logic
            if (gameManagement != null) {
                 Debug.Log("[Level5] 调用 GameManagement.awakeAll()");
                 gameManagement.awakeAll();
            } else {
                 Debug.LogError("[Level5] GameManagement 引用为空，无法启动游戏！");
            }
        } else {
            Debug.Log($"[Level5] CheckStartGameCondition: LocalReady={isLocalPlayerReady}, OpponentReady={isOpponentReady}, CoreGameStarted={coreGameStarted}. 条件未满足。");
        }
    }
    
    // --- Copied Methods from Level 6 (Implementations are identical) ---
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

} 