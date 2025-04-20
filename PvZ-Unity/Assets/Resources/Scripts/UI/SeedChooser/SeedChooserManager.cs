using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class SeedChooserManager : MonoBehaviour
{
    // 已选卡片列表
    private List<Card> selectedCards = new List<Card>();
    
    // 已选卡片槽位容器
    public Transform selectedCardsPanel;
    
    // 最大可选卡片数
    public int maxSelectedCards = 10;
    
    // 确认按钮
    public Button confirmButton;
    
    // 最小所需卡片数
    public int minRequiredCards = 1;
    
    // 动画持续时间（秒）
    public float animationDuration = 0.5f;
    
    // 是否正在播放动画
    private bool isAnimating = false;
    
    // --- 动画对象池 ---
    private GameObject animCardPrefab;
    private List<GameObject> _animCardPool = new List<GameObject>();
    private int _poolSize = 3; // 同时最多播放3个动画应该够了
    
    // 动画目标位置微调偏移量 (可根据实际效果调整)
    private const float ANIMATION_TARGET_OFFSET_X = 8f; // 稍微增大偏移试试
    
    // 主UI画布引用
    private Canvas _mainCanvas;
    
    // *** 新增：映射原始卡片和其在已选面板中的UI对象 ***
    private Dictionary<Card, GameObject> _selectedCardUIMap = new Dictionary<Card, GameObject>();
    
    // *** 新增：GameManagement 引用 ***
    private GameManagement _gameManagement;
    
    // *** 修改：引用 Level5Controller ***
    private Level5Controller _level5Controller; 
    
    public void Awake()
    {
        // 获取主画布
        _mainCanvas = GetComponentInParent<Canvas>();
        if (_mainCanvas == null)
        {
            Debug.LogError("[SeedChooserManager] 无法找到父级Canvas!");
            _mainCanvas = FindObjectOfType<Canvas>();
             if (_mainCanvas == null)
             {
                  Debug.LogError("[SeedChooserManager] 场景中没有找到任何Canvas!");
             }
        }
        
        // *** 查找 GameManagement (不再查找 Level6Controller) ***
        GameObject gmObject = GameObject.Find("Game Management");
        if (gmObject != null) {
            _gameManagement = gmObject.GetComponent<GameManagement>();
            if (_gameManagement == null) {
                 Debug.LogError("[SeedChooserManager] Game Management 对象上未找到 GameManagement 脚本!");
            }
            // *** 获取 Level5Controller ***
            _level5Controller = gmObject.GetComponent<Level5Controller>(); 
            if (_level5Controller == null) {
                 Debug.LogError("[SeedChooserManager] Game Management 对象上未找到 Level5Controller 脚本!");
            }
        } else {
            Debug.LogError("[SeedChooserManager] 未找到 Game Management 对象! 无法获取 Level5Controller。");
        }
        
        // === 确认按钮设置 ===
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmSelection);
            Debug.Log("[SeedChooserManager] 已为确认按钮添加ConfirmSelection监听器");
        } else {
            Debug.LogWarning("[SeedChooserManager] 确认按钮(confirmButton)未在Inspector中分配!");
        }
        // ===================
        
        // 设置SeedChooser的位置
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(0, 15f);
        }
        
        // 初始化确认按钮状态
        UpdateConfirmButtonState();
        
        // 创建动画卡片预制体和对象池
        CreateAnimationCardPrefab();
        InitializeAnimCardPool();

        // *** 初始化已选卡片面板和映射 ***
        InitializeSelectedCardsPanel(); 
        _selectedCardUIMap.Clear(); 
        foreach (Transform child in selectedCardsPanel)
        {
            Destroy(child.gameObject);
        }
    }
    
    // 创建动画卡片预制体（只创建模板）
    private void CreateAnimationCardPrefab()
    {
        if (_mainCanvas == null)
        {
             Debug.LogError("[SeedChooserManager] 无法创建动画预制体，因为没有找到Canvas。");
             return;
        }
        
        // 删除之前创建的预制体（如果存在）
        if (animCardPrefab != null)
        {
            Destroy(animCardPrefab);
        }
        
        animCardPrefab = new GameObject("AnimCardPrefab");
        
        RectTransform rt = animCardPrefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(43f, 61f); // 标准卡片大小
        
        Image img = animCardPrefab.AddComponent<Image>();
        img.raycastTarget = false; // 不接收点击事件
        img.color = Color.white;
        
        // 将预制体移到主画布下并隐藏
        animCardPrefab.transform.SetParent(_mainCanvas.transform, false); // *** 修改父对象为Canvas ***
        animCardPrefab.SetActive(false);
        
        // 预制体模板不需要 DontDestroyOnLoad
        Debug.Log("[SeedChooserManager] 动画卡片预制体模板创建完成");
    }
    
    // 初始化动画卡片对象池
    private void InitializeAnimCardPool()
    {
        if (animCardPrefab == null) return;
        
        for (int i = 0; i < _poolSize; i++)
        {
            GameObject pooledCard = Instantiate(animCardPrefab, _mainCanvas.transform);
            pooledCard.name = $"AnimCard_Pooled_{i}";
            pooledCard.SetActive(false);
            _animCardPool.Add(pooledCard);
        }
        Debug.Log($"[SeedChooserManager] 动画卡片对象池初始化完成，大小: {_poolSize}");
    }
    
    // 从对象池获取一个动画卡片
    private GameObject GetPooledAnimCard()
    {
        return _animCardPool.Find(card => !card.activeInHierarchy); // 找到第一个未激活的
    }
    
    // 处理卡片点击
    public void OnCardClicked(Card card)
    {
        // --- Log Start ---
        Debug.Log($"[SeedChooserManager] OnCardClicked called for: {card?.name ?? "NULL"}. Current selected count: {selectedCards.Count}. IsAnimating: {isAnimating}");
        if (card == null)
        {
            Debug.LogError("[SeedChooserManager] OnCardClicked received a NULL card!");
            return;
        }
        // --- Log End ---
        
        if (isAnimating) return;
        
        bool alreadySelected = selectedCards.Contains(card);
        // --- Log Contains Result ---
        Debug.Log($"[SeedChooserManager] Does selectedCards contain {card.name}? {alreadySelected}");
        // Log content of selectedCards if needed for deeper debug
        // foreach(var c in selectedCards) { Debug.Log($" - Selected: {c.name}"); }
        // --- Log End ---
        
        if (alreadySelected)
        {
            Debug.Log($"[SeedChooserManager] Processing DESELECTION for {card.name}");
            int index = selectedCards.IndexOf(card);
            UpdateCardAppearance(card, false);
            StartCoroutine(PlayCardRemoveAnimation(card, index, () => {
                RemoveSelectedCardUI(card);
                selectedCards.Remove(card); 
                UpdateConfirmButtonState(); 
            })); 
        }
        else if (selectedCards.Count < maxSelectedCards)
        {
            Debug.Log($"[SeedChooserManager] Processing SELECTION for {card.name}");
            UpdateCardAppearance(card, true);
            StartCoroutine(PlayCardAddAnimation(card, () => {
                selectedCards.Add(card); 
                AddSelectedCardUI(card);
                UpdateConfirmButtonState(); 
            })); 
        }
        else
        {
             Debug.LogWarning($"[SeedChooserManager] Cannot select {card.name}, max cards reached ({selectedCards.Count}/{maxSelectedCards}).");
        }
    }
    
    // 播放卡片添加动画 (添加Action回调)
    private IEnumerator PlayCardAddAnimation(Card card, Action onCompleteCallback = null)
    {
        if (_mainCanvas == null || animCardPrefab == null)
        {
             Debug.LogError("[SeedChooserManager] 无法播放添加动画，Canvas或预制体模板未准备好。");
             yield break; 
        }
        
        isAnimating = true;
        
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Vector3 startPosition = cardRect.position;
        // *** 注意：目标位置计算时，selectedCards 还没有添加新卡片，所以用 Count ***
        Vector3 endPosition = CalculateTargetPosition(selectedCards.Count); 
        
        Debug.Log($"[SeedChooserManager] 开始添加动画 - 从 {startPosition} 到 {endPosition} (目标索引: {selectedCards.Count})");
        
        GameObject animCard = GetPooledAnimCard();
        if (animCard == null) {
            Debug.LogWarning("[SeedChooserManager] 对象池已满，无法播放添加动画！直接添加UI。");
            selectedCards.Add(card); // 直接添加数据
            AddSelectedCardUI(card);   // 直接添加UI
            UpdateConfirmButtonState(); // 更新按钮
            isAnimating = false; // 别忘了重置状态
            yield break; 
        }
        animCard.SetActive(true);
        
        RectTransform animRect = animCard.GetComponent<RectTransform>();
        animRect.position = startPosition; 
        
        Image animImage = animCard.GetComponent<Image>();
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null && cardImage.sprite != null)
        {
            animImage.sprite = cardImage.sprite;
            animImage.color = Color.white;
        }
        else
        {
            animImage.color = Color.red;
        }
        
        animImage.color = Color.white; 
        animRect.localScale = Vector3.one; 
        animCard.transform.SetAsLastSibling();
        
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / animationDuration);
            animRect.position = Vector3.Lerp(startPosition, endPosition, normalizedTime);
            yield return null;
        }
        
        animRect.position = endPosition; 
        animCard.SetActive(false); 
        
        onCompleteCallback?.Invoke(); 
        
        isAnimating = false;
        Debug.Log("[SeedChooserManager] 添加动画完成");
    }
    
    // 播放卡片移除动画 (添加Action回调)
    private IEnumerator PlayCardRemoveAnimation(Card card, int fromIndex, Action onCompleteCallback = null)
    {
         if (_mainCanvas == null || animCardPrefab == null)
        {
             Debug.LogError("[SeedChooserManager] 无法播放移除动画，Canvas或预制体未准备好。");
             yield break; 
        }
        
        isAnimating = true;
        
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Vector3 endPosition = cardRect.position;
        Vector3 startPosition = CalculateTargetPosition(fromIndex); // 起始位置是它当前在面板中的位置
        
        Debug.Log($"[SeedChooserManager] 开始移除动画 - 从 {startPosition} 到 {endPosition} (起始索引: {fromIndex})");
        
        GameObject animCard = GetPooledAnimCard();
         if (animCard == null) {
            Debug.LogWarning("[SeedChooserManager] 对象池已满，无法播放移除动画！直接移除UI。");
            RemoveSelectedCardUI(card); // 直接移除UI
            selectedCards.Remove(card); // 直接移除数据
            UpdateConfirmButtonState(); // 更新按钮
            isAnimating = false; // 重置状态
            yield break; 
        }
        animCard.SetActive(true);
        
        RectTransform animRect = animCard.GetComponent<RectTransform>();
        animRect.position = startPosition; 
        
        Image animImage = animCard.GetComponent<Image>();
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null && cardImage.sprite != null)
        {
            animImage.sprite = cardImage.sprite;
            animImage.color = Color.white;
        }
        else
        {
            animImage.color = Color.red;
        }

        animImage.color = Color.white; 
        animRect.localScale = Vector3.one; 
        animCard.transform.SetAsLastSibling();

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / animationDuration);
            animRect.position = Vector3.Lerp(startPosition, endPosition, normalizedTime);
            yield return null;
        }

        animRect.position = endPosition;
        animCard.SetActive(false); 

        onCompleteCallback?.Invoke();

        isAnimating = false;
        Debug.Log("[SeedChooserManager] 移除动画完成");
    }
    
    // 计算目标位置（世界坐标）
    private Vector3 CalculateTargetPosition(int index)
    {
        if (selectedCardsPanel == null)
        {
            Debug.LogError("[SeedChooserManager] selectedCardsPanel is null!");
            return Vector3.zero;
        }
        
        RectTransform panelRect = selectedCardsPanel.GetComponent<RectTransform>();
        HorizontalLayoutGroup layoutGroup = selectedCardsPanel.GetComponent<HorizontalLayoutGroup>();
        
        if (panelRect == null || layoutGroup == null)
        {
            Debug.LogError("[SeedChooserManager] selectedCardsPanel is missing RectTransform or HorizontalLayoutGroup!");
            return selectedCardsPanel.position; // Fallback to panel position
        }
        
        // 获取缩放因子和缩放后的卡片尺寸
        float scale = GetResolutionScaleFactor();
        float scaledCardWidth = 43f * scale;
        float scaledCardHeight = 61f * scale;

        // --- 计算卡片在面板内的局部坐标 --- 
        
        // X 坐标: 基于左内边距、索引、卡片宽度、间距，并应用偏移量
        float localX = layoutGroup.padding.left 
                       + index * (scaledCardWidth + layoutGroup.spacing) 
                       + scaledCardWidth * 0.5f 
                       + ANIMATION_TARGET_OFFSET_X * scale; // *** 应用X轴偏移量 (按比例缩放) ***
        
        // Y 坐标: 基于顶部内边距和卡片高度
        float localY = -layoutGroup.padding.top - scaledCardHeight * 0.5f;
        
        Vector3 localPosition = new Vector3(localX, localY, 0);
        
        // --- 将局部坐标转换为世界坐标 --- 
        Vector3 worldPosition = selectedCardsPanel.TransformPoint(localPosition);
        
        Debug.Log($"[SeedChooserManager] CalculateTargetPosition - Index: {index}, Scale: {scale:F3}, OffsetX: {ANIMATION_TARGET_OFFSET_X * scale:F2}, LocalPos: {localPosition}, WorldPos: {worldPosition}");
        
        return worldPosition;
    }
    
    // 需要从GridGenerator复制这个方法或将其设为公共静态方法
    private float GetResolutionScaleFactor()
    {
        const float REFERENCE_WIDTH = 3840f;
        const float REFERENCE_HEIGHT = 2160f;
        
        float widthScale = Screen.width / REFERENCE_WIDTH;
        float heightScale = Screen.height / REFERENCE_HEIGHT;
        
        // 使用较小的缩放因子，避免UI元素溢出屏幕
        return Mathf.Min(widthScale, heightScale);
    }
    
    // 更新卡片外观
    private void UpdateCardAppearance(Card card, bool isSelected)
    {
        if (card == null) return;
        
        // 新方法：查找并控制lowerImage的显示/隐藏
        // --- 修改：直接使用 Card 脚本的 lowerImageObj 引用 ---
        // Transform lowerImageTransform = FindLowerImage(card.gameObject); // No longer needed?
        
        if (card.lowerImageObj != null) // Use the reference from the Card script
        {
            // 如果被选中，显示lowerImage；否则隐藏
            card.lowerImageObj.SetActive(isSelected); // Set active state directly
            Debug.Log($"[SeedChooserManager] Setting card {card.name}'s lowerImageObj active state to: {isSelected}");
        }
        else
        {
            Debug.LogWarning($"[SeedChooserManager] 无法找到卡片 {card.name} 的lowerImageObj 引用"); // Updated log message
            
            // 备用方案：如果找不到lowerImage，仍使用原来的颜色变化方式
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = isSelected ? 
                new Color(0.7f, 0.7f, 0.7f, 1f) : // 变暗
                Color.white; // 正常颜色
        }
        }
    }
    
    // 查找卡片中的lowerImage组件
    private Transform FindLowerImage(GameObject cardObject)
    {
        // 直接查找名为"LowerImage"的子对象
        Transform lowerImage = cardObject.transform.Find("LowerImage");
        
        // 如果没找到，尝试递归查找包含"Image"的组件
        if (lowerImage == null)
        {
            lowerImage = FindImageRecursively(cardObject.transform);
        }
        
        return lowerImage;
    }
    
    // 递归查找图像组件
    private Transform FindImageRecursively(Transform parent)
    {
        // 检查所有子对象
        foreach (Transform child in parent)
        {
            // 检查名称是否包含"Image"
            if (child.name.IndexOf("Image", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // 排除明确是UpperImage的情况
                if (!child.name.Equals("UpperImage", StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            
            // 递归检查子层级
            Transform found = FindImageRecursively(child);
            if (found != null)
            {
                return found;
            }
        }
        
        return null;
    }
    
    // *** 新增：添加单个已选卡片的UI ***
    private void AddSelectedCardUI(Card originalCard)
    {
        if (originalCard == null || _selectedCardUIMap.ContainsKey(originalCard))
        {
             Debug.LogWarning($"[SeedChooserManager] 尝试添加已存在的UI卡片或原始卡片为空: {originalCard?.name}");
             return;
        }

        string plantName = originalCard.plantName;
        Debug.Log($"[SeedChooserManager] Adding UI for selected card: {plantName}");

        // 加载对应的卡片预制体 (可以考虑预加载优化)
        GameObject cardPrefab = Resources.Load<GameObject>("Prefabs/UI/Card/" + plantName + "Card");
        if (cardPrefab == null)
        {
            Debug.LogError($"[SeedChooserManager] 无法加载用于已选UI的卡片预制体: {plantName}Card");
            return;
        }

        // 实例化卡片UI到已选面板
        GameObject cardUIObject = Instantiate(cardPrefab, selectedCardsPanel);
        cardUIObject.name = $"SelectedCardUI_{plantName}";

        // 配置UI卡片
        RectTransform rectTransform = cardUIObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(43f, 61f); 
        }

        // 禁用交互，只作显示 -> 修改为：初始化为Selection模式，然后覆盖监听器
        Card cardComponent = cardUIObject.GetComponent<Card>();
        if (cardComponent != null)
        {   
            // *** 1. 初始化卡片为 Selection 模式 ***
            cardComponent.Initialize(plantName, CardMode.Selection);
            Debug.Log($"[SeedChooserManager] 初始化已选UI卡片 {cardUIObject.name} 为 Selection 模式");

            // *** 2. 覆盖按钮监听器，使其调用 HandleSeedBankCardClick ***
            Button button = cardUIObject.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true; // 保持可交互
                button.onClick.RemoveAllListeners(); // 清除 Initialize 添加的监听器
                // 添加新的监听器，点击时调用 HandleSeedBankCardClick，并传入原始卡片
                 button.onClick.AddListener(() => HandleSeedBankCardClick(originalCard));
                Debug.Log($"[SeedChooserManager] 为已选UI卡片 {cardUIObject.name} 添加了点击返回监听器");
            }
            // *** 注意：Initialize会默认隐藏lowerImage，我们需要强制显示它 ***
        }
        
        // 确保图像可见 (如果需要特殊处理，例如显示LowerImage)
        Transform lowerImage = FindLowerImage(cardUIObject);
        if (lowerImage != null)
        {
            lowerImage.gameObject.SetActive(false); 
            Debug.Log($"[SeedChooserManager] 强制显示已选UI卡片 {cardUIObject.name} 的 lowerImage");
        }

        // 添加到映射字典
        _selectedCardUIMap[originalCard] = cardUIObject;

        Debug.Log($"[SeedChooserManager] UI for {plantName} added. Total selected UI: {_selectedCardUIMap.Count}");
    }

    // *** 新增：处理点击SeedBank中卡片的逻辑 ***
    private void HandleSeedBankCardClick(Card originalCard)
    {
        // --- Log Start ---
        Debug.Log($"[SeedChooserManager] HandleSeedBankCardClick triggered for: {originalCard?.name ?? "NULL"}. IsAnimating: {isAnimating}");
        
        if (isAnimating) 
        {
            Debug.Log("[SeedChooserManager] HandleSeedBankCardClick ignored because isAnimating is true.");
            return; 
        }
        
        if (originalCard == null)
        {
             Debug.LogError("[SeedChooserManager] HandleSeedBankCardClick received a NULL originalCard!");
             return;
        }
        // --- Log End ---
        
        // 直接调用原始卡片的点击处理逻辑，触发取消选择流程
        OnCardClicked(originalCard);
    }

    // *** 新增：移除单个已选卡片的UI ***
    private void RemoveSelectedCardUI(Card originalCard)
    {
        if (originalCard == null) return;

        Debug.Log($"[SeedChooserManager] Removing UI for selected card: {originalCard.name}");

        if (_selectedCardUIMap.TryGetValue(originalCard, out GameObject cardUIObject))
        {
            if (cardUIObject != null)
            {
                Destroy(cardUIObject);
            }
            _selectedCardUIMap.Remove(originalCard);
            Debug.Log($"[SeedChooserManager] UI for {originalCard.name} removed. Total selected UI: {_selectedCardUIMap.Count}");
        }
        else
        {
             Debug.LogWarning($"[SeedChooserManager] 尝试移除不存在的UI卡片: {originalCard.name}");
        }
    }
    
    // 更新确认按钮状态
    private void UpdateConfirmButtonState()
    {
        if (confirmButton != null)
        {
            // 只有当选择的卡片数量达到最低要求时才启用确认按钮
            confirmButton.interactable = selectedCards.Count >= minRequiredCards;
        }
    }
    
    // Start方法
    void Start()
    {
        // 获取所有Card组件 - 注意：这里可能会获取到非选择界面的卡片，可能需要更精确的查找
        // 考虑通过GridGenerator获取或查找特定父对象下的Card
        // Card[] allCards = FindObjectsOfType<Card>(); 
        // foreach (Card card in allCards)
        // {
        //     // 默认设置为未选中状态
        //     UpdateCardAppearance(card, false);
        // }
        
        // 初始化已选卡片面板 (Awake中已调用)
        // InitializeSelectedCardsPanel();
        
        // 初始化确认按钮状态 (Awake中已调用)
        // UpdateConfirmButtonState();
    }
    
    // 初始化已选卡片面板
    private void InitializeSelectedCardsPanel()
    {
        if (selectedCardsPanel == null)
        {
            Debug.LogError("[SeedChooserManager] 已选卡片面板未设置！");
            return;
        }
        
        // 确保面板有水平布局组件
        HorizontalLayoutGroup layoutGroup = selectedCardsPanel.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = selectedCardsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 0;  // 卡片之间没有间距
            layoutGroup.childAlignment = TextAnchor.UpperLeft; // 左上对齐
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = false;  // 不控制子物体宽度
            layoutGroup.childControlHeight = false; // 不控制子物体高度
            layoutGroup.padding = new RectOffset(0, 0, 0, 0); // 无内边距
        }
        
        // 设置面板大小
        RectTransform panelRect = selectedCardsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // 设置足够大的尺寸来容纳所有卡片
            panelRect.sizeDelta = new Vector2(maxSelectedCards * 43f, 66f); // 43是卡片宽度，66是卡片高度
        }
    }
    
    // 确认选择
    public void ConfirmSelection()
    {
        if (selectedCards.Count >= minRequiredCards)
        {
            Debug.Log($"[SeedChooserManager] 已确认选择 {selectedCards.Count} 张卡片");
            
            List<string> selectedPlants = new List<string>();
            foreach (Card card in selectedCards)
            {
                selectedPlants.Add(card.plantName);
            }
            // 将最终选择的卡片列表更新到 GameManagement.levelData
            GameManagement.levelData.plantCards = selectedPlants;
            Debug.Log($"[SeedChooserManager] 更新 LevelData 植物列表: {string.Join(", ", GameManagement.levelData.plantCards)}");

            // *** 在启动核心逻辑前，先开始摄像头返回动画，并销毁预览僵尸 ***
             if (_level5Controller != null) {
                  Debug.Log("[SeedChooserManager] 通知 Level5Controller 本地玩家已确认...");
                  _level5Controller.PlayerConfirmedSelection();
             } else {
                 Debug.LogError("[SeedChooserManager] Level5Controller 引用为空，无法通知确认!");
             }
            
             gameObject.SetActive(false); 
             Debug.Log("[SeedChooserManager] 选卡界面已隐藏");
        }
        else
        {
            Debug.LogWarning($"[SeedChooserManager] 选择的卡片数量不足！已选择 {selectedCards.Count}，至少需要 {minRequiredCards}");
        }
    }
}
