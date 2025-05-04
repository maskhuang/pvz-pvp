using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 卡片模式枚举
public enum CardMode
{
    GamePlay,    // 游戏中模式
    Selection    // 选卡阶段模式
}

public class Card : MonoBehaviour
{
    //冷却贴图
    public GameObject upperImageObj;
    public Image lowerImage;
    public GameObject lowerImageObj;
    public Image cardImage; // 添加卡片图像引用

    private Button myButton;   // CHANGE: Make private, assign in Awake

    //冷却时间与冷却状态
    public float coolingTime;
    float timer;
    bool coolingState = true;

    //阳光是否充足状态
    bool sunEnough;

    //种植相关
    PlantingManagement planting;
    public string plantName;
    public int sunNeeded;
    
    // 添加卡片模式
    public CardMode currentMode = CardMode.GamePlay;

    // ADD: Awake method to get components early
    void Awake()
    {
        // Ensure Button component is found early
        myButton = GetComponent<Button>();
        // DEBUG: Log the result of GetComponent<Button>
        Debug.Log($"Card {gameObject.name} Awake(): GetComponent<Button> result is null? {(myButton == null)}", this.gameObject);
        if (myButton == null)
        {   
             Debug.LogError($"Card {gameObject.name} is missing Button component!", this.gameObject);
        }

        // Ensure Image component is found early (for the card background)
        cardImage = GetComponent<Image>();
        if (cardImage == null)
        {   
             Debug.LogError($"Card {gameObject.name} is missing Image component!", this.gameObject);
        }
        
        // Note: lowerImage, upperImageObj, lowerImageObj are public and expected 
        // to be assigned in the Inspector for the prefab.
    }

    // Start is called before the first frame update
    void Start()
    {
        //该组件须由管理对象加载，故在Start获取
        if (currentMode == CardMode.GamePlay)
        {
            planting = GameObject.Find("Planting Management").GetComponent<PlantingManagement>();
            if (coolingTime > 10f) cooling();
            else endCooling();
        }
    }

    // 初始化方法
    public void Initialize(string plantName, CardMode mode)
    {
        this.plantName = plantName;
        this.currentMode = mode;
        
        // 加载卡片图像
        // if (cardImage == null) // REMOVE: Assigned in Awake
        //     cardImage = GetComponent<Image>();
            
        Sprite cardSprite = Resources.Load<Sprite>($"Sprites/Cards/{plantName}Card");
        if (cardSprite != null)
            cardImage.sprite = cardSprite;
        
        // 设置初始状态
        if (lowerImageObj != null)
        {
            // 在选卡模式下，默认隐藏lowerImage
            if (mode == CardMode.Selection)
            {
                lowerImageObj.SetActive(false);
                // *** Explicitly disable upper image in Selection mode ***
                if (upperImageObj != null) 
                {
                    upperImageObj.SetActive(false);
                    Debug.Log($"[{plantName}Card Initialize(Selection)] Disabled upperImageObj.");
                }
            }
            // Optional: Add an else block to ensure upperImage IS active for GamePlay if needed
            // else if (mode == CardMode.GamePlay)
            // {
            //     // Ensure upperImageObj state matches cooling state logic if necessary
            // }
        }
        
        // 根据模式设置不同行为
        if (mode == CardMode.Selection)
        {   
            // 选卡模式下的设置
            // if (myButton == null) // REMOVE: Assigned in Awake
            //     myButton = GetComponent<Button>();
                
            if(myButton != null) { // Add null check before using
                 myButton.onClick.RemoveAllListeners();
                 myButton.onClick.AddListener(OnSelectionModeClick);
                 myButton.enabled = true; // 确保按钮可点击
            } else {
                 Debug.LogError($"Cannot setup Selection mode click for {plantName}, myButton is null!", this.gameObject);
            }
        }
        else // GamePlay Mode
        {   
            // 游戏模式下的设置 - 保持原有逻辑
            // if (myButton == null) // REMOVE: Assigned in Awake
            //     myButton = GetComponent<Button>();
                
            if(myButton != null) { // Add null check before using
                 myButton.onClick.RemoveAllListeners();
                 myButton.onClick.AddListener(click);
                 // Button enabled state will be handled by cooling logic
            } else {
                 Debug.LogError($"Cannot setup GamePlay mode click for {plantName}, myButton is null!", this.gameObject);
            }
        }
    }

    // 选卡模式下的点击处理
    private void OnSelectionModeClick()
    {
        Debug.Log($"Selection mode click: {plantName}");
        // 通知SeedChooserManager处理选卡逻辑
        SeedChooserManager manager = FindObjectOfType<SeedChooserManager>();
        if (manager != null)
        {
            manager.OnCardClicked(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMode == CardMode.GamePlay && coolingState == true)
        {
            timer += Time.deltaTime;
            if (timer / coolingTime < 1)
                lowerImage.rectTransform.localScale = new Vector3(1, 1 - timer / coolingTime, 1);
            else endCooling();
        }
    }

    public void cooling()
    {
        coolingState = true;
        timer = 0;
        lowerImage.fillAmount = 1;
        upperImageObj.SetActive(true);
        lowerImageObj.SetActive(true);
        if (myButton != null) myButton.enabled = false;
    }

    private void endCooling()
    {
        coolingState = false;
        if (lowerImageObj != null) lowerImageObj.SetActive(false);
        if (upperImageObj != null) upperImageObj.SetActive(false); // 冷却结束时，总是隐藏冷却覆盖物
        if (myButton != null) myButton.enabled = true; // 冷却结束时，总是启用按钮交互
        // updateSunEnough 会根据当前的 sunEnough 状态处理视觉效果（比如灰色遮罩）
    }

    public void updateSunEnough(bool state)
    {
        if (currentMode == CardMode.GamePlay)
        {
            if(state == true)
            {
                sunEnough = true;
                if (coolingState == false)
                {
                    upperImageObj.SetActive(false);
                    if (myButton != null) myButton.enabled = true;
                }
            }
            else
            {
                sunEnough = false;
                upperImageObj.SetActive(true);
                if (myButton != null) myButton.enabled = false;
            }
        }
    }    

    public void click()
    {
        if (currentMode == CardMode.GamePlay)
        {
            //播放音效
            gameObject.GetComponent<AudioSource>().Play();

            //转给种植管理
            planting.clickPlant(plantName, gameObject.GetComponent<Card>());
        }
    }
}
