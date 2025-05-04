using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GridGenerator : MonoBehaviour
{
    public GameObject cardSlotPrefab;
    public Transform gridContainer;
    public float slotInstantiationDelay = 0.01f; // 每帧实例化间隔
    
    // 网格尺寸
    private const int ROWS = 6;
    private const int COLUMNS = 8;
    
    // 参考分辨率下的网格属性
    private const float REF_CELL_WIDTH = 125f;
    private const float REF_CELL_HEIGHT = 155f;
    private const float REF_SPACING_X = 315f;
    private const float REF_SPACING_Y = 135f;
    private const float REF_PADDING_LEFT = 335f;
    private const float REF_PADDING_TOP = 230f;
    
    void Start()
    {
        StartCoroutine(GenerateGridCoroutine()); // 使用协程
    }
    
    void GenerateGrid()
    {
        // 确保网格容器有GridLayoutGroup组件
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("Grid container does not have a GridLayoutGroup component!");
            return;
        }
        
        // 使用ResolutionScaler更新网格布局
        UpdateGridLayoutForCurrentResolution();
        
        // 生成卡片槽
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                GameObject cardSlot = Instantiate(cardSlotPrefab, gridContainer);
                cardSlot.name = $"CardSlot_{row}_{col}";
                
                // 保存行列信息用于后续逻辑
                CardSlot slotComponent = cardSlot.AddComponent<CardSlot>();
                slotComponent.row = row;
                slotComponent.column = col;
            }
        }
    }
    // 当屏幕分辨率变化时调用
    void OnRectTransformDimensionsChange()
    {
        // 监测分辨率变化并重新调整布局
        UpdateGridLayoutForCurrentResolution();
    }
    
    // 更新网格布局以适应当前分辨率
    public void UpdateGridLayoutForCurrentResolution()
    {
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) return;
        
        // 使用本地实现的缩放计算方法
        float scale = GetResolutionScaleFactor();
        
        // 更新布局属性
        gridLayout.cellSize = new Vector2(REF_CELL_WIDTH * scale, REF_CELL_HEIGHT * scale);
        gridLayout.spacing = new Vector2(REF_SPACING_X * scale, REF_SPACING_Y * scale);
        gridLayout.padding = new RectOffset(
            Mathf.RoundToInt(REF_PADDING_LEFT * scale), 
            0, 
            Mathf.RoundToInt(REF_PADDING_TOP * scale), 
            0
        );
        
        Debug.Log($"更新网格布局 - 屏幕分辨率: {Screen.width}x{Screen.height}, 缩放因子: {scale}");
    }
    
    // 本地实现的分辨率缩放计算方法
    private float GetResolutionScaleFactor()
    {
        const float REFERENCE_WIDTH = 3840f;
        const float REFERENCE_HEIGHT = 2160f;
        
        float widthScale = 1.0f; //Screen.width / REFERENCE_WIDTH;
        float heightScale = 1.0f; //Screen.height / REFERENCE_HEIGHT;
        
        // 使用较小的缩放因子，避免UI元素溢出屏幕
        return Mathf.Min(widthScale, heightScale);
    }
    
    // 使用协程生成网格
    IEnumerator GenerateGridCoroutine()
    {
        // 确保网格容器有GridLayoutGroup组件
        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            Debug.LogError("Grid container does not have a GridLayoutGroup component!");
            yield break; // 退出协程
        }
        
        // 使用ResolutionScaler更新网格布局
        UpdateGridLayoutForCurrentResolution();
        
        // 生成卡片槽 - 分散到多帧
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLUMNS; col++)
            {
                GameObject cardSlot = Instantiate(cardSlotPrefab, gridContainer);
                cardSlot.name = $"CardSlot_{row}_{col}";
                
                // 保存行列信息用于后续逻辑
                CardSlot slotComponent = cardSlot.AddComponent<CardSlot>();
                slotComponent.row = row;
                slotComponent.column = col;
                
                yield return new WaitForSeconds(slotInstantiationDelay); // 等待一小段时间
            }
        }
        Debug.Log("[GridGenerator] 网格生成完毕 (协程)");
    }
}

// 自定义CardSlot组件
public class CardSlot : MonoBehaviour
{
    public int row;
    public int column;
    public bool isOccupied = false;
    public GameObject containedCard = null;
    
    // 可以添加其他卡片槽相关的逻辑
}