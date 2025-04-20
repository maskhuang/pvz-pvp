using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantGrid : MonoBehaviour
{
    #region 变量

    public int row;   //在第几行

    GameObject toBePlanted;   //To Be Planted对象
    GameObject selectedShovel;        //SelectedShovel对象

    SpriteRenderer spriteRenderer;  //自身SpriteRenderer组件
    AudioSource audioSource;   //自身AudioSource组件

    // NEW: More detailed grid state representation
    private Plant normalPlant = null;      // Reference to the normal plant
    private Plant shellPlant = null;       // Reference to the shell plant (e.g., Pumpkin)
    private Plant carrierPlant = null;     // Reference to the carrier plant (e.g., Pot, LilyPad)
    private Plant flyerPlant = null;       // Reference to the flyer plant (e.g., Coffee Bean)
    private float craterTimer = 0f;        // Timer for Doom-shroom crater effect

    #endregion

    #region 系统消息

    private void Awake()
    {
        //获取必要组件
        toBePlanted = GameObject.Find("To Be Planted");
        selectedShovel = GameObject.Find("SelectedShovel");

        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    // ADD: Update method to handle crater timer
    void Update()
    {
        if (craterTimer > 0f)
        {
            craterTimer -= Time.deltaTime;
            if (craterTimer <= 0f)
            {   
                craterTimer = 0f;
                // Optionally, add visual feedback for crater disappearing
                Debug.Log($"[PlantGrid ({row},{transform.position.x})] Crater disappeared."); 
            }
        }
    }

    private void OnMouseEnter()
    {
        // Handle planting preview
        if (toBePlanted.activeSelf == true)
        {
            string plantName = toBePlanted.GetComponent<ToBePlanted>().plantName;
            PlantCategory category = GetPlantCategoryFromName(plantName);

            if (CanPlantHere(category))
            {
                // Show preview sprite if available
                Sprite previewSprite = toBePlanted.GetComponent<SpriteRenderer>()?.sprite; // Use null-conditional operator
                if (previewSprite != null) {
                    spriteRenderer.sprite = previewSprite;
                } else {
                     // Optional: Show a generic highlight or different preview for mesh-based plants
                }
            } else {
                 // Optional: Show a 'cannot plant here' visual cue
                 spriteRenderer.sprite = null; // Ensure no preview if cannot plant
            }
        }
        // Handle shoveling highlight
        else if (selectedShovel.activeSelf == true)
        {
            // Highlight the primary plant if it exists (normal or carrier if no normal)
            Plant plantToHighlight = normalPlant ?? carrierPlant; // Highlight normal first, then carrier
            if (plantToHighlight != null) {
                plantToHighlight.highlight();
            }
            // Optionally highlight shell or flyer too?
            // if (shellPlant != null) shellPlant.highlight(); 
        }
    }

    private void OnMouseExit()
    {
        // Clear planting preview
        if (toBePlanted.activeSelf == true)
        {
            spriteRenderer.sprite = null;
        }
        // Clear shoveling highlight
        else if (selectedShovel.activeSelf == true)
        {
            // Cancel highlight on all potentially highlighted plants
            normalPlant?.cancelHighlight(); // Use null-conditional operator
            shellPlant?.cancelHighlight();
            carrierPlant?.cancelHighlight();
            flyerPlant?.cancelHighlight();
        }
    }

    private void OnMouseDown()
    {
        // Handle planting action
        if (toBePlanted.activeSelf == true)
        {
            string plantName = toBePlanted.GetComponent<ToBePlanted>().plantName;
            PlantCategory category = GetPlantCategoryFromName(plantName);

            if (CanPlantHere(category))
            {
                plant(plantName, category); // Pass category to plant method
            }
            else
            {
                // Optional: Play a 'cannot plant' sound
                Debug.Log("[PlantGrid] Cannot plant " + plantName + " here based on rules.");
            }
        }
        // Handle shoveling action
        else if (selectedShovel.activeSelf == true)
        {
            // Shovel logic: Prioritize removing flyers, then shells, then normal, then carriers.
            if (flyerPlant != null) {
                 flyerPlant.die("shovelPlant");
            } else if (shellPlant != null) {
                 shellPlant.die("shovelPlant");
            } else if (normalPlant != null) {
                 normalPlant.die("shovelPlant");
            } else if (carrierPlant != null) {
                 carrierPlant.die("shovelPlant");
            } else {
                // Optional: Play an 'empty shovel' sound
                 Debug.Log("[PlantGrid] Nothing to shovel here.");
            }
        }
    }

    #endregion

    #region 私有自定义函数

    // ADD: Helper to get plant category from prefab name
    private PlantCategory GetPlantCategoryFromName(string plantName)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Plants/" + plantName);
        if (prefab != null)
        {   
            Plant plantComponent = prefab.GetComponent<Plant>();
            if (plantComponent != null)
            {   
                return plantComponent.Category;
            }
            else
            {   
                Debug.LogError($"[PlantGrid] Prefab {plantName} does not have a Plant component!");
            }
        }
        else
        {
            Debug.LogError($"[PlantGrid] Could not load prefab for {plantName}");
        }
        // Return a default or error category if loading fails
        return PlantCategory.Normal; // Or handle error differently
    }

    // ADD: Method to check if a plant of a specific category can be planted here
    private bool CanPlantHere(PlantCategory categoryToPlant)
    {
        if (craterTimer > 0f && categoryToPlant != PlantCategory.Carrier) // Can only plant carriers (Pots) in craters?
        {
            Debug.Log("[PlantGrid] Cannot plant: Crater present.");
            return false;
        }

        switch (categoryToPlant)
        {
            case PlantCategory.Normal:
            case PlantCategory.Upgrader: // Assume upgraders replace normal plants
                // Can plant if no normal plant exists AND (it's land OR a carrier exists)
                bool needsCarrier = false; // TODO: Determine if this grid requires a carrier (e.g., water, roof)
                bool canPlantNormal = normalPlant == null && (!needsCarrier || carrierPlant != null);
                if (!canPlantNormal) Debug.Log("[PlantGrid] Cannot plant Normal/Upgrader here.");
                return canPlantNormal;
            
            case PlantCategory.Shell:
                // Can plant if a normal plant exists AND no shell plant exists yet
                bool canPlantShell = normalPlant != null && shellPlant == null;
                 if (!canPlantShell) Debug.Log("[PlantGrid] Cannot plant Shell here.");
                return canPlantShell;

            case PlantCategory.Carrier:
                // Can plant if no carrier exists AND grid type allows (e.g., water, roof)
                bool isWaterOrRoof = false; // TODO: Determine grid type
                bool canPlantCarrier = carrierPlant == null && isWaterOrRoof;
                 if (!canPlantCarrier) Debug.Log("[PlantGrid] Cannot plant Carrier here.");
                return canPlantCarrier;

            case PlantCategory.Flyer: 
                // Can plant if a suitable normal plant exists (e.g., sleeping mushroom) AND no flyer exists
                // TODO: Need more specific check for the type of normalPlant
                bool suitableNormalPlant = normalPlant != null; // Simplified check for now
                bool canPlantFlyer = suitableNormalPlant && flyerPlant == null;
                 if (!canPlantFlyer) Debug.Log("[PlantGrid] Cannot plant Flyer here.");
                return canPlantFlyer;

            case PlantCategory.Instant: 
                // Instants might ignore most rules, or maybe just need empty ground
                 bool canPlantInstant = normalPlant == null; // Simplified: Can plant if no normal plant
                 if (!canPlantInstant) Debug.Log("[PlantGrid] Cannot plant Instant here.");
                return canPlantInstant;

            case PlantCategory.Special:
            case PlantCategory.CraterMaker:
                // Can plant if grid is empty (no normal, no carrier)
                bool canPlantSpecial = normalPlant == null && carrierPlant == null;
                 if (!canPlantSpecial) Debug.Log("[PlantGrid] Cannot plant Special/CraterMaker here.");
                return canPlantSpecial;

            default:
                Debug.LogWarning($"[PlantGrid] Unhandled plant category in CanPlantHere: {categoryToPlant}");
                return false;
        }
    }

    #endregion

    #region 公有自定义函数

    // MODIFIED: Accept PlantCategory
    public void plant(string name, PlantCategory category)
    {
        //清除阴影
        spriteRenderer.sprite = null;
        
        //生成植物
        Debug.Log($"[PlantGrid] 正在实例化植物: {name} (Category: {category})");
        GameObject plantGO = Instantiate(Resources.Load<GameObject>("Prefabs/Plants/" + name),
                                      transform.position + new Vector3(0, 0, 5),
                                      Quaternion.Euler(0, 0, 0),
                                      transform);
        Plant newPlant = plantGO.GetComponent<Plant>();

        if (newPlant == null) {
             Debug.LogError($"[PlantGrid] Instantiated plant {name} is missing Plant component!");
             Destroy(plantGO);
             return;
        }

        // Assign reference based on category
        switch (category)
        {
            case PlantCategory.Normal:
            case PlantCategory.Upgrader:
            case PlantCategory.Instant: // Instants might briefly occupy normal slot?
            case PlantCategory.Special: 
                 normalPlant = newPlant;
                 break;
            case PlantCategory.Shell:
                 shellPlant = newPlant;
                 break;
            case PlantCategory.Carrier:
                 carrierPlant = newPlant;
                 break;
            case PlantCategory.Flyer:
                 flyerPlant = newPlant;
                 break;
            case PlantCategory.CraterMaker:
                 normalPlant = newPlant; // Doom-shroom occupies normal slot initially
                 // TODO: Trigger crater creation upon explosion in Doom-shroom script
                 // It should call a new method like grid.CreateCrater(duration)
                 break;
            default:
                 Debug.LogWarning($"[PlantGrid] Unhandled plant category in plant assignment: {category}");
                 normalPlant = newPlant; // Assign to normal as fallback
                 break;
        }
        
        // Initialize the plant
        newPlant.initialize(
            this,
            spriteRenderer.sortingLayerName,
            spriteRenderer.sortingOrder
        );

        //播放音效
        audioSource.clip =
            Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/plant");
        audioSource.Play();

        //向PlantingManagement发送消息以处理UI相关事件
        // TODO: Consider if PlantingManagement needs more info than just 'plant()'
        GameObject.Find("Planting Management").GetComponent<PlantingManagement>().plant();
    }

    //上帝模式种植，用于关卡开始或回合开始种植
    // MODIFIED: Needs category logic too, or simplify if only normal plants are planted by god?
    public GameObject plantByGod(string name)
    {       
        PlantCategory category = GetPlantCategoryFromName(name); // Determine category
        Debug.Log($"[PlantGrid] God planting: {name} (Category: {category})");

        GameObject plantGO = Instantiate(Resources.Load<GameObject>("Prefabs/Plants/" + name),
                                          transform.position + new Vector3(0, 0, 5),
                                          Quaternion.Euler(0, 0, 0),
                                          transform);
        Plant newPlant = plantGO.GetComponent<Plant>();

        if (newPlant == null) { /* Error handling */ Destroy(plantGO); return null; }

        // Assign reference based on category (Simplified - assuming God only plants normals for now)
        // TODO: Expand this if God needs to plant other types
        if (category == PlantCategory.Normal || category == PlantCategory.CraterMaker || category == PlantCategory.Special) { 
             normalPlant = newPlant;
        } else {
            Debug.LogWarning($"[PlantGrid] plantByGod currently only supports Normal/Special/CraterMaker. Planting {name} ({category}) might have issues.");
            // Assign to normal as fallback, might cause issues
             normalPlant = newPlant; 
        }

        newPlant.initialize(
            this,
            spriteRenderer.sortingLayerName,
            spriteRenderer.sortingOrder
        );

        return newPlant.gameObject;
    }

    // MODIFIED: Accept the Plant instance that died
    public void plantDie(Plant deadPlant, string reason)
    {
        if (deadPlant == null) return;

        PlantCategory category = deadPlant.Category;
        Debug.Log($"[PlantGrid] Plant died: {deadPlant.name} (Category: {category}), Reason: {reason}");

        // Clear the correct reference
        if (normalPlant == deadPlant) normalPlant = null;
        if (shellPlant == deadPlant) shellPlant = null;
        if (carrierPlant == deadPlant) carrierPlant = null;
        if (flyerPlant == deadPlant) flyerPlant = null;

        // TODO: If a carrier dies, what happens to plants on it (normal, shell, flyer)? They should probably die too.
        if (category == PlantCategory.Carrier) {
            normalPlant?.die("carrierDestroyed"); // Use null-conditional call
            shellPlant?.die("carrierDestroyed");
            flyerPlant?.die("carrierDestroyed");
        }
        // TODO: If a normal plant dies, what happens to shell/flyer on it?
        // Shell might fall off (destroy?), flyer might disappear.
        if (category == PlantCategory.Normal) {
             shellPlant?.die("basePlantDestroyed"); 
             flyerPlant?.die("basePlantDestroyed");
        }

        AudioClip clip = null;
        if (!string.IsNullOrEmpty(reason)) clip = Resources.Load<AudioClip>("Sounds/Plants/" + reason);
        if (clip != null)
        {   
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
    
    // ADD: Method for plants (like Doom-shroom) to call when creating a crater
    public void CreateCrater(float duration)
    {
        craterTimer = duration;
        // Optional: Visual effect for crater creation
        Debug.Log($"[PlantGrid ({row},{transform.position.x})] Crater created for {duration}s.");
        // If a normal plant was here (Doom-shroom itself), it should be gone now.
        // The Doom-shroom's die() method should have called plantDie already.
    }

    #endregion
}
