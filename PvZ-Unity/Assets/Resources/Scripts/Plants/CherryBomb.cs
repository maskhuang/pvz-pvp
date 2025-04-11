using UnityEngine;

public class CherryBomb : Plant
{
    private Animator animator;
    private float animationLength;
    private float startTime;
    private bool isAnimating = false;
    private PlantGrid plantGrid;  // 添加对PlantGrid的引用
    private bool hasDamaged = false;  // 确保伤害只造成一次
    private const int DAMAGE = 1800;  // 爆炸伤害值

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 获取Animator组件
        animator = GetComponent<Animator>();
        
        // 如果没有Animator组件，直接销毁物体
        if (animator == null)
        {
            Debug.LogError("CherryBomb缺少Animator组件");
            Destroy(gameObject);
            return;
        }

        // 获取当前动画片段的长度
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            animationLength = clipInfo[0].clip.length;
            startTime = Time.time;
            isAnimating = true;
        }
        else
        {
            Debug.LogError("CherryBomb动画片段未找到");
            Destroy(gameObject);
            return;
        }

        // 获取PlantGrid组件
        plantGrid = transform.parent.GetComponent<PlantGrid>();
        if (plantGrid == null)
        {
            Debug.LogError("CherryBomb的父物体缺少PlantGrid组件");
            Destroy(gameObject);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isAnimating && !hasDamaged)
        {
            // 检查动画是否播放完成
            if (Time.time - startTime >= animationLength)
            {
                // 造成爆炸伤害
                Explode();
                hasDamaged = true;
                
                // 通知PlantGrid格子可以重新种植
                plantGrid.plantDie("");
                // 动画播放完成，销毁物体
                Destroy(gameObject);
            }
        }
    }

    private void Explode()
    {
        // 获取当前格子的行号
        Debug.Log($"[CherryBomb] Explode - 当前格子行号: {plantGrid.row}");
        int currentRow = plantGrid.row;
        
        // 获取所有僵尸
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        Debug.Log($"[CherryBomb] Explode - 找到的僵尸数量: {zombies.Length}");
        
        foreach (GameObject zombie in zombies)
        {
            // 获取僵尸所在的行
            int zombieRow = zombie.GetComponent<Zombie>().pos_row;
            // 获取僵尸的X坐标
            float zombieX = zombie.transform.position.x;
            // 获取当前樱桃炸弹的X坐标
            float bombX = transform.position.x;
            
            // 检查僵尸是否在爆炸范围内（3x3格子）
            if (Mathf.Abs(zombieRow - currentRow) <= 1 && // 上下一行范围内
                Mathf.Abs(zombieX - bombX) <= 1.5f)       // 左右一格范围内（假设每格宽度约1.5单位）
            {
                // 对僵尸造成伤害
                Zombie zombieComponent = zombie.GetComponent<Zombie>();
                if (zombieComponent != null)
                {
                    zombieComponent.beAttacked(DAMAGE);
                }
            }
        }
    }
}
