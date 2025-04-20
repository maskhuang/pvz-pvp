using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections; // 添加协程命名空间


public class CardLoader : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform gridContainer;
    
    public float cardInstantiationDelay = 0.02f; // 卡片实例化延迟
    // 可用的植物卡片列表
    public List<string> availablePlants = new List<string>
    {
        "PeaShooter",
        "SunFlower",
        "WallNut",
        "Squash",
        "TorchWood",
        "CherryBomb",
        "SnowKing"
        // 添加更多植物...
    };
    
    // 预加载的卡片预制体字典
    private Dictionary<string, GameObject> _preloadedCardPrefabs = new Dictionary<string, GameObject>();
    
    void Start()
    {
        Debug.Log("[CardLoader] Start方法开始执行");
        Debug.Log("[CardLoader] availablePlants: " + availablePlants.Count);
        
        // 检查引用是否为空
        if (cardPrefab == null)
        {
            Debug.LogError("[CardLoader] cardPrefab为空！请在Inspector中指定卡片预制体");
            return;
        }
        
        if (gridContainer == null)
        {
            Debug.LogError("[CardLoader] gridContainer为空！请在Inspector中指定网格容器");
            return;
        }
        
        // 检查植物列表
        if (availablePlants == null || availablePlants.Count == 0)
        {
            Debug.LogWarning("[CardLoader] 可用植物列表为空或为null");
        }
        else
        {
            Debug.Log($"[CardLoader] 可用植物列表包含{availablePlants.Count}个植物: {string.Join(", ", availablePlants)}");
        }
        
        // 使用协程预加载，然后开始加载卡片
        StartCoroutine(LoadCardsCoroutine());
        
        Debug.Log("[CardLoader] Start方法执行完毕");
    }
    
    // 协程加载卡片
    IEnumerator LoadCardsCoroutine()
    {
        Debug.Log("[CardLoader] 开始加载卡片 (协程)");
        
        // 1. 预加载所有卡片预制体
        yield return StartCoroutine(PreloadCardPrefabs());
        
        // 获取所有卡片槽
        // 等待一小会儿确保网格生成协程完成
        yield return new WaitForSeconds(0.2f); // 等待GridGenerator完成
        CardSlot[] cardSlots = gridContainer.GetComponentsInChildren<CardSlot>();
        Debug.Log($"[CardLoader] 找到{cardSlots.Length}个卡片槽");
        
        if (cardSlots.Length == 0)
        {
            Debug.LogError("[CardLoader] 未找到卡片槽！请确保GridGenerator已正确生成网格");
            yield break;
        }
        
        // 确保我们有足够的卡片槽
        int plantCount = Mathf.Min(availablePlants.Count, cardSlots.Length);
        Debug.Log($"[CardLoader] 将加载{plantCount}个植物卡片");
        
        // 加载卡片到槽位
        for (int i = 0; i < plantCount; i++)
        {
            string plantName = availablePlants[i];
            CardSlot slot = cardSlots[i];
            
            Debug.Log($"[CardLoader] 开始加载第{i+1}个卡片: {plantName}");
            
            // 检查槽位是否有效
            if (slot == null)
            {
                Debug.LogError($"[CardLoader] 卡片槽[{i}]为空！");
                continue;
            }
            
            try
            {
                // 从预加载的字典中获取预制体
                GameObject prefab = null;
                if (!_preloadedCardPrefabs.TryGetValue(plantName, out prefab) || prefab == null)
                {
                    Debug.LogWarning($"[CardLoader] 预加载字典中未找到或预制体为空: {plantName}Card，使用通用预制体");
                    prefab = cardPrefab; // Fallback to default card prefab
                }

                if (prefab == null) { Debug.LogError($"[CardLoader] 连通用预制体(cardPrefab)都为空！"); continue; }
                
                GameObject cardObject = Instantiate(prefab, slot.transform);
                
                if (cardObject == null)
                {
                    Debug.LogError($"[CardLoader] 卡片{plantName}实例化失败！");
                    continue;
                }
                
                cardObject.name = $"Card_{plantName}";
                Debug.Log($"[CardLoader] 成功实例化卡片: {cardObject.name}");
                
                // 确保卡片位置和大小正确
                RectTransform rectTransform = cardObject.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 重置所有RectTransform属性以避免继承问题
                    rectTransform.localPosition = Vector3.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    
                    // 使用槽位大小并略微缩小以确保适合
                    RectTransform slotRect = slot.GetComponent<RectTransform>();
                    if (slotRect != null)
                    {
                        rectTransform.sizeDelta = slotRect.sizeDelta; // 与槽位大小相同
                    }
                    
                    rectTransform.localScale = Vector3.one;
                    Debug.Log($"[CardLoader] 调整卡片{plantName}的RectTransform: pos={rectTransform.anchoredPosition}, size={rectTransform.sizeDelta}, anchors={rectTransform.anchorMin}/{rectTransform.anchorMax}");
                    
                    // 专门调整lowerImage和upperImage的尺寸
                    AdjustCardImages(cardObject);
                }
                else
                {
                    Debug.LogWarning($"[CardLoader] 卡片{plantName}没有RectTransform组件");
                }
                
                // 设置卡片数据和外观
                Card cardComponent = cardObject.GetComponent<Card>();
                if (cardComponent != null)
                {
                    Debug.Log($"[CardLoader] 开始初始化卡片组件: {plantName}");
                    
                    // 如果还没有定义CardMode，先确认它存在
                    try {
                        // 这里使用了CardMode枚举，确保它已被定义
                        // 如果出现编译错误，你需要先定义这个枚举
                        CardMode selectionMode = CardMode.Selection;
                        
                        // 初始化卡片
                        cardComponent.Initialize(plantName, selectionMode);
                        Debug.Log($"[CardLoader] 卡片初始化成功: {plantName}");
                        
                        // 更新槽位状态
                        slot.containedCard = cardObject;
                        slot.isOccupied = true;
                        Debug.Log($"[CardLoader] 卡片{plantName}已放置在槽位[{slot.row}, {slot.column}]");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[CardLoader] 初始化卡片{plantName}时出错: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogError($"[CardLoader] 卡片预制体没有Card组件！请确保预制体附加了Card脚本");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CardLoader] 加载卡片{plantName}时发生异常: {e.Message}\n{e.StackTrace}");
            }
            
            yield return new WaitForSeconds(cardInstantiationDelay); // 等待下一帧或更长时间再实例化下一个
        }
        
        Debug.Log($"[CardLoader] 卡片加载完成，共加载{plantCount}个卡片");
    }

    // 预加载卡片预制体
    IEnumerator PreloadCardPrefabs()
    {
        Debug.Log("[CardLoader] 开始预加载卡片预制体...");
        _preloadedCardPrefabs.Clear();
        foreach (string plantName in availablePlants)
        {
            string cardPath = "Prefabs/UI/Card/" + plantName + "Card";
            GameObject prefab = Resources.Load<GameObject>(cardPath);
            if (prefab != null) {
                _preloadedCardPrefabs[plantName] = prefab;
                Debug.Log($"[CardLoader] 预加载成功: {cardPath}");
            } else {
                Debug.LogWarning($"[CardLoader] 预加载失败: 未找到 {cardPath}");
            }
            yield return null; // 每加载一个等待一帧，避免单帧压力过大
        }
        Debug.Log($"[CardLoader] 卡片预制体预加载完成，共{_preloadedCardPrefabs.Count}个");
    }

    private void AdjustCardImages(GameObject cardObject)
    {
        Debug.Log($"[CardLoader] 正在调整卡片布局: {cardObject.name}");
        
        // 查找LowerImage和UpperImage
        // 1. 直接查找名称为LowerImage和UpperImage的组件
        Transform lowerImage = cardObject.transform.Find("LowerImage");
        Transform upperImage = cardObject.transform.Find("UpperImage");
        
        // 2. 如果没有找到，尝试查找带有Image字样的组件
        if (lowerImage == null)
        {
            foreach (Transform child in cardObject.transform)
            {
                if (child.name.IndexOf("Image", StringComparison.OrdinalIgnoreCase) >= 0 && child != upperImage)
                {
                    lowerImage = child;
                    Debug.Log($"[CardLoader] 使用替代图像组件: {lowerImage.name}");
                    break;
                }
            }
        }
        
        // 调整下部主图像（植物图像）
        if (lowerImage != null)
        {
            RectTransform lowerRect = lowerImage.GetComponent<RectTransform>();
            if (lowerRect != null)
            {
                // 设置填充卡片主要空间
                lowerRect.anchorMin = new Vector2(0, 0);
                lowerRect.anchorMax = new Vector2(1, 1);  // 留出上方空间给价格图像
                lowerRect.pivot = new Vector2(0.5f, 0.5f);
                lowerRect.anchoredPosition = new Vector2(0, -5);  // 稍微下移
                
                // 确保图像设置正确
                UnityEngine.UI.Image image = lowerImage.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.preserveAspect = true;
                    Debug.Log($"[CardLoader] 设置{lowerImage.name}为保持纵横比");
                }
                
                // 默认隐藏lowerImage，等待被选中时显示
                lowerImage.gameObject.SetActive(false);
                Debug.Log($"[CardLoader] 默认隐藏{cardObject.name}的{lowerImage.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[CardLoader] 卡片{cardObject.name}中找不到主图像组件");
        }
        
        // 调整上部图像（价格/阳光消耗）
        if (upperImage != null)
        {
            // 隐藏上部图像
            upperImage.gameObject.SetActive(false);
            Debug.Log($"[CardLoader] 隐藏了{cardObject.name}的{upperImage.name}");
            
            /*
            RectTransform upperRect = upperImage.GetComponent<RectTransform>();
            if (upperRect != null)
            {
                // 设置为卡片右上角的小块区域
                upperRect.anchorMin = new Vector2(0.7f, 0.7f);
                upperRect.anchorMax = new Vector2(1, 1);
                upperRect.pivot = new Vector2(1, 1);  // 右上角为轴心点
                upperRect.anchoredPosition = new Vector2(-5, -5);  // 与右上角保持一定距离
                upperRect.sizeDelta = new Vector2(-10, -10);  // 四周留出一些边距
                
                // 确保图像设置正确
                UnityEngine.UI.Image image = upperImage.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.preserveAspect = true;
                    Debug.Log($"[CardLoader] 设置{upperImage.name}为保持纵横比");
                }
                
                Debug.Log($"[CardLoader] 成功调整{cardObject.name}的{upperImage.name}");
            }
            */
        }
        else
        {
            Debug.LogWarning($"[CardLoader] 卡片{cardObject.name}中找不到价格图像组件");
        }
    }

    // 根据名称查找卡片组件
    private Transform FindCardComponentByNames(GameObject cardObject, string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            // 直接在顶层查找
            Transform component = cardObject.transform.Find(name);
            if (component != null)
            {
                Debug.Log($"[CardLoader] 在顶层找到组件: {name}");
                return component;
            }
            
            // 在所有子层级查找包含指定名称的对象
            component = FindChildWithNameContains(cardObject.transform, name);
            if (component != null)
            {
                Debug.Log($"[CardLoader] 在子层级找到组件: {component.name} (匹配: {name})");
                return component;
            }
        }
        
        return null;
    }

    // 查找包含特定名称的子对象
    private Transform FindChildWithNameContains(Transform parent, string nameContains)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                return child;
            
            Transform found = FindChildWithNameContains(child, nameContains);
            if (found != null)
                return found;
        }
        
        return null;
    }

    // 打印游戏对象层级结构
    private void PrintGameObjectHierarchy(GameObject obj, string indent = "")
    {
        Debug.Log($"{indent}GameObject: {obj.name} (Active: {obj.activeSelf})");
        
        // 打印所有组件
        Component[] components = obj.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null) continue;
            
            string componentInfo = component.GetType().Name;
            
            // 为特定组件类型添加更多信息
            if (component is RectTransform)
            {
                RectTransform rt = component as RectTransform;
                componentInfo += $" [Size: {rt.sizeDelta}, Anchors: ({rt.anchorMin}-{rt.anchorMax}), Pivot: {rt.pivot}]";
            }
            else if (component is UnityEngine.UI.Image)
            {
                UnityEngine.UI.Image img = component as UnityEngine.UI.Image;
                componentInfo += $" [Sprite: {(img.sprite ? img.sprite.name : "null")}, PreserveAspect: {img.preserveAspect}]";
            }
            
            Debug.Log($"{indent}  - {componentInfo}");
        }
        
        // 递归打印子对象
        foreach (Transform child in obj.transform)
        {
            PrintGameObjectHierarchy(child.gameObject, indent + "    ");
        }
    }

    // 递归查找子对象
    private Transform FindChildRecursively(Transform parent, string name)
    {
        // 检查当前层级
        Transform child = parent.Find(name);
        if (child != null)
            return child;
        
        // 检查包含指定名称的对象
        foreach (Transform t in parent)
        {
            if (t.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return t;
        }
        
        // 递归检查子层级
        foreach (Transform t in parent)
        {
            child = FindChildRecursively(t, name);
            if (child != null)
                return child;
        }
        
        return null;
    }
}