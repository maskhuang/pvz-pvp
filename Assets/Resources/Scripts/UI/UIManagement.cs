using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class UIManagement : MonoBehaviour
{
    public GameObject topMotionPanel;
    public GameObject bottomMotionPanel;
    public GameObject seedBank;
    public GameObject shovelBank;
    public Text levelNameText;
    public TextMeshProUGUI countdownTextTMP;

    public GameObject cardGroup;   //卡片群组

    // 单个卡片的宽度
    private const float CARD_WIDTH = 43f;
    // 最大卡片数量
    private const int MAX_CARD_COUNT = 10;

    // Start is called before the first frame update
    public void initUI()
    {
        //加载关卡名称
        levelNameText.text = GameManagement.levelData.levelName;

        // 初始化卡片布局
        InitializeCardLayout();

        //加载卡片群组
        List<string> plantCards = GameManagement.levelData.plantCards;
        
        List<Card> cards = new List<Card>();
        foreach (string plant in plantCards)
        {
            // 在cardGroup中实例化卡片
            GameObject cardObj = Instantiate(
                Resources.Load<Object>("Prefabs/UI/Card/" + plant + "Card"),
                cardGroup.transform
            ) as GameObject;
            
            // 获取Card组件并添加到列表
            Card cardComponent = cardObj.GetComponent<Card>();
            cards.Add(cardComponent);
            
            // 设置卡片的RectTransform，确保固定尺寸和正确位置
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 设置锚点和轴心点
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);
                
                // 设置固定尺寸
                rectTransform.sizeDelta = new Vector2(CARD_WIDTH, 61); // 假设高度为61，根据实际情况调整
                
                // 调整垂直位置
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 5); // 整体向上调整5个单位，增加偏移量
                
                Debug.Log($"[UIManagement] 设置卡片{plant}的尺寸为:{rectTransform.sizeDelta}, 位置偏移:{rectTransform.anchoredPosition}");
            }
        }
        
        GameObject.Find("Sun Text").GetComponent<SunNumber>().setCardGroup(cards);
        
        // 固定seedBank宽度为10个卡片的长度
        float cardGroupWidth = MAX_CARD_COUNT * CARD_WIDTH - 1; // 固定宽度
        
        // 设置固定宽度
        cardGroup.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardGroupWidth);
        seedBank.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardGroupWidth + 78);
        shovelBank.GetComponent<RectTransform>()
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, cardGroupWidth + 108, 60);
    }

    // 初始化卡片布局
    private void InitializeCardLayout()
    {
        // 移除已有的布局组件（如果有）
        HorizontalLayoutGroup existingLayout = cardGroup.GetComponent<HorizontalLayoutGroup>();
        if (existingLayout != null)
        {
            DestroyImmediate(existingLayout);
        }
        
        // 清空现有的所有子物体，避免重复
        foreach (Transform child in cardGroup.transform)
        {
            Destroy(child.gameObject);
        }
        
        // 添加水平布局组件
        HorizontalLayoutGroup layoutGroup = cardGroup.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 0;  // 卡片之间没有间距
        layoutGroup.childAlignment = TextAnchor.UpperLeft; // 左上对齐，便于向下调整
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = false;  // 不控制子物体宽度
        layoutGroup.childControlHeight = false; // 不控制子物体高度
        layoutGroup.padding = new RectOffset(0, 0, 0, 0); // 无内边距
        
        // 设置卡片组的RectTransform
        RectTransform rectTransform = cardGroup.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            // 设置足够大的宽度容纳所有卡片
            rectTransform.sizeDelta = new Vector2(MAX_CARD_COUNT * CARD_WIDTH, 66); // 高度根据实际情况调整
            // 调整垂直位置
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 0.25f); // 整体向上调整5个单位，增加偏移量
        }
        
        Debug.Log("[UIManagement] 初始化卡片布局完成");
    }

    // === 新增：单独启动顶部面板 (SeedBank) 动画的方法 ===
    public void AnimateSeedBankEntrance()
    {
        if (topMotionPanel != null) {
            Debug.Log("[UIManagement] Starting SeedBank entrance animation.");
            // 确保 cardGroup 在动画开始前是激活的，否则卡片不会显示
            if (cardGroup != null) cardGroup.SetActive(true);
            topMotionPanel.GetComponent<MotionPanel>()?.startMove(); // 使用 ?. 避免空引用
        } else {
             Debug.LogWarning("[UIManagement] topMotionPanel reference is not set!");
        }
    }
    // ====================================================

    public void appear()
    {
        //卡片群组本为了避免泄题而被隐藏，在此开启卡片的冷却计时
        // cardGroup.SetActive(true); // 不再在这里激活 cardGroup，由 AnimateSeedBankEntrance 处理

        // topMotionPanel.GetComponent<MotionPanel>().startMove(); // 不再在这里启动顶部动画
        bottomMotionPanel.GetComponent<MotionPanel>().startMove();
    }
}
