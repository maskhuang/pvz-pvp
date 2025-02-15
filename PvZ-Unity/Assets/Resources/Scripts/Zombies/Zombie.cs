using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Zombie : MonoBehaviour
{
    public float speed;   //移动速度
    public float eatOffset;   //偏移位置，当植物距离很远时不会移动
    public int pos_row;   //位置在第几行
    public ZombieState state = ZombieState.Normal;
    private Plant parasiticPlant;   //状态记录，植物

    //生命值
    public int bloodVolume;   //血量
    protected int bloodVolumeMax;
    private bool alive = true;

    //攻击力
    public int attackPower;  //攻击力
    protected Plant plant;   //前一个接触的植物Plant

    protected Animator myAnimator;   //动画器
    protected AudioSource audioSource;  //音频源
    protected string audioOfBeingAttacked = "Sounds/Zombies/bodyhit";
    private int audioIndex = 1;

    static int orderOffset = 0;

    bool sleep = true;   //是否开始初始化延迟

    public event Action<Zombie> OnDeath;

    protected virtual void Awake()
    {
        //获取组件
        myAnimator = gameObject.GetComponent<Animator>();
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        //初始化延迟一秒，使初始化延迟效果生效
        if (sleep == true)
        {
            gameObject.SetActive(false);
            Invoke("activate", UnityEngine.Random.Range(0.0f, 5.0f));
        }

        //随机增加速度
        float increase = UnityEngine.Random.Range(1.0f, 1.5f);
        speed *= increase;
        myAnimator.speed *= increase;

        bloodVolumeMax = bloodVolume;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (myAnimator.GetBool("Walk") == true)
        {
            transform.Translate(-speed * Time.deltaTime, 0, 0);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Plant" 
            && collision.GetComponent<Plant>().row == pos_row 
            && collision.transform.position.x < transform.position.x + eatOffset
            && myAnimator.GetBool("Attack") == false)
        {
            myAnimator.SetBool("Walk", false);
            myAnimator.SetBool("Attack", true);

            plant = collision.GetComponent<Plant>();
        }
        else if (collision.tag == "GameOverLine")
        {
            GameObject.Find("Game Management").GetComponent<GameManagement>().gameOver();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Plant" && collision.GetComponent<Plant>().row == pos_row)
        {
            myAnimator.SetBool("Attack", false);
            myAnimator.SetBool("Walk", true);
        }
    }

    protected virtual void activate()
    {
        gameObject.SetActive(true);
    }

    public virtual void attack()
    {
        //植物被攻击
        if (plant != null)
        {
            plant.beAttacked(attackPower, "beEated");
        }
    }

    protected virtual void die()
    {
        //碰撞体失效
        gameObject.GetComponent<Collider2D>().enabled = false;
        //全部僵尸数减一
        GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
        alive = false;
        //隐藏头
        hideHead();
        //动画切换
        myAnimator.SetBool("Walk", false);
        myAnimator.SetBool("Die", true);

        // 触发死亡事件
        OnDeath?.Invoke(this);
    }

    //可能不同僵尸隐藏头的方法不同，具体实现需要根据实际情况来写
    protected virtual void hideHead()
    {

    }

    //攻击，被火焰攻击时调用
    public virtual void beAttacked(int hurt)
    {
        bloodVolume -= hurt;
        if (bloodVolume <= 0 && alive == true)
        {
            die();
        }
    }

    public virtual void playAudioOfBeingAttacked()
    {
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>(audioOfBeingAttacked + audioIndex)
        );
        if (audioIndex == 1) audioIndex = 2;
        else audioIndex = 1;
    }

    //攻击，被火焰攻击时调用
    public virtual void beBurned()
    {
        beAttacked(10);
    }

    public virtual void beSquashed()
    {
        bloodVolume -= 1800;
        if(bloodVolume <= 0)
        {
            //全部僵尸数减一
            GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
            //僵尸死亡
            Destroy(gameObject);
        }
    }

    public void beParasiticed(Plant parasiticPlant)
    {
        if(state != ZombieState.Parasiticed)
        {
            SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = new Color(0.4f, 1, 0.4f, spriteRenderer.color.a);
            }
            this.parasiticPlant = parasiticPlant;
            state = ZombieState.Parasiticed;
            InvokeRepeating("suckBlood", 0, 1f);
        }
    }

    private void suckBlood()
    {
        int hurt = (int)(bloodVolumeMax * 0.01);
        beAttacked(hurt);
        if (parasiticPlant != null) parasiticPlant.recover(hurt);
    }

    public void cancelSleep()
    {
        if(gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }
        sleep = false;
    }

    //设置位置行，影响渲染顺序
    public virtual void setPosRow(int pos)
    {
        //设置位置行
        pos_row = pos;

        //设置渲染顺序和显示顺序
        SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer.sortingLayerName == "Default")
            {
                spriteRenderer.sortingLayerName = "Zombie-" + pos_row;
                spriteRenderer.sortingOrder += orderOffset * 20;
            }
        }
        orderOffset++;
    }

    //设置死亡声音
    public virtual void fallDown()
    {
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>("Sounds/Zombies/zombie_falling")
        );
    }

    //设置吃声音
    public virtual void PlayEatAudio()
    {
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>("Sounds/Zombies/chomp" + UnityEngine.Random.Range(1, 3))
        );
    }

    //死亡，僵尸死亡时调用
    public void disappear()
    {
        Destroy(gameObject);
    }

}

public enum ZombieState { Normal, Cold, Parasiticed }