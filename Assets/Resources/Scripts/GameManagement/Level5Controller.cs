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
    private Coroutine sendViewFrameCoroutine;

    // --- Copied/Adapted from Level 6 ---
    public float cameraPanDuration = 1.0f; 
    public float cameraPannedXPosition = 3.6f; 
    private Vector3 initialCameraPosition; 
    private Camera mainCamera;
    public int previewZombieCount = 5; 
    public GameObject previewZombiePrefab; 
    public Transform previewSpawnAreaObject; 
    private List<GameObject> decorativeZombies = new List<GameObject>(); 
    public float previewZombieSpawnDelay = 0.2f; 
    // --- End Copied ---

    // === 修改：画中画相关 (用于接收和显示对手画面) ===
    public GameObject opponentViewDisplayObject; // 包含RawImage的UI对象 (Inspector 或 Find)
    // private Camera opponentViewCamera;      // 不再需要，画面直接来自网络
    private RawImage opponentViewRawImage;    // RawImage 组件引用
    // private RenderTexture opponentViewRenderTexture; // 不再需要
    private bool isOpponentViewActive = false; // 画中画激活状态
    private Texture2D receivedTexture;        // 用于解码和显示接收到的画面
    // =======================================

    // === 新增: 画面传输参数 ===
    public float sendFrameInterval = 0.5f; // 发送帧间隔 (秒)，0.5f = 2 FPS
    public int captureWidth = 320;        // 捕捉画面的宽度
    public int captureHeight = 180;       // 捕捉画面的高度
    public int jpgQuality = 50;           // JPG 压缩质量 (0-100)
    private RenderTexture captureRenderTexture; // 用于捕捉本地画面的RT
    private Texture2D captureTexture;      // 用于编码本地画面的Texture
    // ==========================

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
        
        // === 修改：初始化画中画 (只查找UI和RawImage) ===
        InitializeOpponentViewReceiver();
        // =============================================

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
        decorativeZombies.Clear();

        // === 新增：清理 RenderTexture ===
        // if (opponentViewRenderTexture != null) {
        //     opponentViewRenderTexture.Release(); // 释放 RenderTexture 资源
        //     Destroy(opponentViewRenderTexture); // 销毁对象
        //      Debug.Log("[Level5] 释放并销毁了画中画 RenderTexture。");
        // }
        // ==============================
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
            
            // === 在启动游戏逻辑前，尝试初始化画中画 (接收端设置) ===
            InitializeOpponentViewReceiver();
            // ========================================

            // === 在启动游戏逻辑后，启动画面发送协程 (仅限本地玩家) ===
            if (photonView.IsMine)
            {
                // 创建用于捕捉的 Render Texture 和 Texture2D
                // 需要检查 mainCamera 是否有效
                if (mainCamera != null) {
                    // 确保旧的 opponentViewRenderTexture (如果因某种原因还存在) 不被引用
                    captureRenderTexture = new RenderTexture(captureWidth, captureHeight, 16, RenderTextureFormat.Default);
                    captureTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
                    sendViewFrameCoroutine = StartCoroutine(SendViewCoroutine());
                    Debug.Log("[Level5] 本地玩家，启动画面发送协程。");
                } else {
                     Debug.LogError("[Level5] 无法启动画面发送：主摄像头 mainCamera 为空!");
                }
            }
            // ======================================================

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

    // === 修改：画中画接收端初始化方法 ===
    private void InitializeOpponentViewReceiver()
    {
        Debug.Log("[Level5] 初始化画中画接收端...");

        // 1. 查找 UI 显示对象
        if (opponentViewDisplayObject == null)
        {
            Debug.Log("[Level5] opponentViewDisplayObject 未在 Inspector 中设置，尝试通过名称 'OpponentViewDisplay' 查找...");
            opponentViewDisplayObject = GameObject.Find("OpponentViewDisplay");
        }

        // 2. 获取 RawImage 组件
        if (opponentViewDisplayObject != null)
        {
            opponentViewRawImage = opponentViewDisplayObject.GetComponent<RawImage>();
            if (opponentViewRawImage == null)
            {
                Debug.LogError("[Level5] 'OpponentViewDisplay' 对象上没有找到 RawImage 组件! 画中画接收功能将不可用。");
                opponentViewDisplayObject = null; // 标记为无效
            }
            else
            {
                // 初始化用于显示接收画面的 Texture2D
                // 尺寸可以先随意设，LoadImage 会自动调整
                receivedTexture = new Texture2D(2, 2);
                opponentViewRawImage.texture = receivedTexture; // 先关联上
                opponentViewRawImage.enabled = false; // 初始隐藏 RawImage
                opponentViewDisplayObject.SetActive(false); // 初始隐藏父对象
                isOpponentViewActive = false;
                Debug.Log("[Level5] 画中画接收端初始化完成，UI 设置为隐藏。");
            }
        }
        else
        {
            Debug.LogError("[Level5] 未能找到名为 'OpponentViewDisplay' 的 GameObject! 画中画接收功能将不可用。");
        }
    }
    // ===============================

    // === 修改：切换画中画视图方法 (只控制显隐) ===
    private void ToggleOpponentView()
    {
        // 检查核心组件是否存在
        if (opponentViewDisplayObject == null || opponentViewRawImage == null)
        {
             Debug.LogError("[Level5] 无法切换观察视图：缺少必要的组件 (DisplayObject 或 RawImage)!");
             // 尝试重新初始化
             InitializeOpponentViewReceiver();
             // 再次检查
             if (opponentViewDisplayObject == null || opponentViewRawImage == null) {
                return; // 如果还是找不到，就无法继续
             }
             Debug.LogWarning("[Level5] 在切换时重新尝试初始化视图组件。");
        }

        isOpponentViewActive = !isOpponentViewActive;

        opponentViewDisplayObject.SetActive(isOpponentViewActive); // 控制整个UI对象的显隐
        opponentViewRawImage.enabled = isOpponentViewActive;   // RawImage 也需要同步显隐状态

        Debug.Log($"[Level5] 切换观察视图: {(isOpponentViewActive ? "开启" : "关闭")}");

        // 如果关闭视图，可以考虑清空一下纹理减少内存占用？(可选)
        // if (!isOpponentViewActive && receivedTexture != null) {
        //     receivedTexture.LoadImage(new byte[0]); // 加载空数据可能清空
        // }
    }
    // ==============================

    // === 新增：画面发送协程 ===
    private IEnumerator SendViewCoroutine()
    {
        while (coreGameStarted) // 游戏进行中才发送
        {
            yield return new WaitForSeconds(sendFrameInterval); // 等待指定间隔

            // 再次确认没有引用 opponentViewRenderTexture
            if (mainCamera == null || captureRenderTexture == null || captureTexture == null)
            {
                Debug.LogWarning("[Level5] SendViewCoroutine: 缺少必要组件 (Camera, RT, Texture)，跳过此次发送。");
                continue; // 如果组件丢失，跳过本次循环
            }

            try
            {
                // 1. 将主摄像头渲染到 Render Texture
                RenderTexture previousTarget = mainCamera.targetTexture;
                mainCamera.targetTexture = captureRenderTexture;
                mainCamera.Render();
                mainCamera.targetTexture = previousTarget; // 还原主摄像头目标

                // 2. 从 Render Texture 读取像素到 Texture2D
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = captureRenderTexture;
                captureTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                captureTexture.Apply();
                RenderTexture.active = previousActive;

                // 3. 编码为 JPG 字节数组
                byte[] imageData = captureTexture.EncodeToJPG(jpgQuality);

                // 4. 通过 RPC 发送给其他玩家
                // TODO: 检查 imageData 大小是否超过 Photon 限制 (约 512 KB)，如果超过需要分块或进一步压缩
                if (imageData.Length > 500000) {
                    Debug.LogWarning($"[Level5] SendViewCoroutine: Image data size ({imageData.Length} bytes) might exceed Photon limit. Consider lowering resolution/quality or implementing chunking.");
                    // 可以选择在此处放弃发送或尝试分块
                    continue;
                }

                photonView.RPC("RPC_ReceiveOpponentFrame", RpcTarget.Others, imageData);
                // Debug.Log($"[Level5] Sent frame: {imageData.Length} bytes"); // 频繁日志可能影响性能
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Level5] SendViewCoroutine Error: {ex.Message}\n{ex.StackTrace}");
                // 发生错误时可以考虑停止协程或增加等待时间
                // yield return new WaitForSeconds(5f); // 不能在 catch 块中使用 yield
            }
        }
        Debug.Log("[Level5] SendViewCoroutine finished.");
    }
    // ==========================

    // === 新增：接收画面帧的 RPC ===
    [PunRPC]
    void RPC_ReceiveOpponentFrame(byte[] imageData)
    {
        // 检查接收端是否准备好
        // 再次确认没有引用 opponentViewRenderTexture
        if (opponentViewRawImage == null || receivedTexture == null)
        {
            // Debug.LogWarning("[Level5] RPC_ReceiveOpponentFrame: Receiver not ready, discarding frame.");
            return;
        }

        // 只有当画中画激活时才处理和显示，节省资源
        if (isOpponentViewActive)
        {
             try
             {
                 // 使用 LoadImage 加载数据，它会自动调整 Texture2D 尺寸
                 bool success = receivedTexture.LoadImage(imageData);
                 if (success)
                 {
                      // 确保 RawImage 的纹理是更新后的 Texture
                      // LoadImage 会复用 Texture 对象，所以理论上不需要重新赋值
                      // 但为了确保，可以取消注释下一行
                      // opponentViewRawImage.texture = receivedTexture;
                 }
                 else
                 {
                      Debug.LogError("[Level5] RPC_ReceiveOpponentFrame: Failed to load image data.");
                 }
             }
             catch (System.Exception ex)
             {
                  Debug.LogError($"[Level5] RPC_ReceiveOpponentFrame Error: {ex.Message}\n{ex.StackTrace}");
             }
        }
    }
    // ===========================

    // === 新增：在Update中检测按键 ===
    void Update() {
         // 只有在游戏核心逻辑开始后才允许切换视角
         if (coreGameStarted && Input.GetKeyDown(KeyCode.Tab)) // 使用 Tab 键作为示例
         {
             ToggleOpponentView();
         }
    }
    // =============================

    // 清理资源
    void OnApplicationQuit()
    {
        // 清理资源
        // 再次确认 OnApplicationQuit 中没有引用 opponentViewRenderTexture
        CleanUpTransmissionResources();
    }

    void CleanUpTransmissionResources() {
        if (photonView.IsMine && sendViewFrameCoroutine != null)
        {
            StopCoroutine(sendViewFrameCoroutine);
            sendViewFrameCoroutine = null;
        }
        // 释放捕捉用的 Texture 和 RT
        if (captureRenderTexture != null)
        {
            if (captureRenderTexture.IsCreated()) {
                 captureRenderTexture.Release();
            }
            DestroyImmediate(captureRenderTexture); // Use DestroyImmediate in OnDestroy/OnApplicationQuit
             captureRenderTexture = null;
        }
        if (captureTexture != null)
        {
            DestroyImmediate(captureTexture);
            captureTexture = null;
        }
        // 释放接收用的 Texture
        if (receivedTexture != null)
        {
            DestroyImmediate(receivedTexture);
            receivedTexture = null;
        }

        Debug.Log("[Level5] 清理了画面传输相关资源。");
    }
} 