using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;

public class Zombie : MonoBehaviour, IPunObservable
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

    // 修改网络同步相关的变量
    protected PhotonView photonView;
    protected float initSpeed; // 初始速度
    protected Vector3 initPosition; // 初始位置
    protected bool isDead = false;

    protected virtual void Awake()
    {
        //获取组件
        myAnimator = gameObject.GetComponent<Animator>();
        audioSource = gameObject.GetComponent<AudioSource>();
        photonView = GetComponent<PhotonView>();
        
        // 保存初始状态
        initPosition = transform.position;
        initSpeed = speed;
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        // 生成时随机化速度，所有客户端使用相同的种子
        if (photonView != null)
        {
            UnityEngine.Random.InitState((int)(photonView.ViewID * 100));
            float increase = UnityEngine.Random.Range(1.0f, 1.5f);
            speed = initSpeed * increase;
            myAnimator.speed = increase;
        }

        SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in allSprites)
        {
            spriteRenderer.sortingLayerName = "Zombie-" + (4 - pos_row);
        }

        //初始化延迟一秒，使初始化延迟效果生效
        if (sleep)
        {
            gameObject.SetActive(false);
            Invoke("activate", UnityEngine.Random.Range(0.0f, 5.0f));
        }

        bloodVolumeMax = bloodVolume;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!isDead && myAnimator.GetBool("Walk"))
        {
            transform.Translate(-speed * Time.deltaTime, 0, 0);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDead)
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
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (!isDead)
        {
            if (collision.tag == "Plant" && collision.GetComponent<Plant>().row == pos_row)
            {
                myAnimator.SetBool("Attack", false);
                myAnimator.SetBool("Walk", true);
            }
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

    public virtual void beAttacked(int hurt)
    {
        if (!isDead)
        {
            bloodVolume -= hurt;
            if (bloodVolume <= 0 && alive)
            {
                // 通知对方生成僵尸
                GameObject zombieManagement = GameObject.Find("Zombie Management");
                if (zombieManagement != null)
                {
                    string zombieType = gameObject.name.Replace("(Clone)", "");
                    Debug.Log($"[Zombie] 僵尸死亡，通知对方在第{pos_row}行生成{zombieType}");
                    GameObject gameManagement = GameObject.Find("Game Management");
                    if(gameManagement.GetComponent<GameManagement>().level == 5)
                    {
                        zombieManagement.GetComponent<ZombieManagement>().OnZombieDeath(pos_row, zombieType);
                    }
                }

                // 直接处理自己的死亡
                Die();
            }
        }
    }

    private void Die()
    {
        isDead = true;
        alive = false;
        
        Debug.Log($"[Zombie] 僵尸{gameObject.name}开始死亡处理，IsMine={photonView.IsMine}");
        
        //碰撞体失效
        gameObject.GetComponent<Collider2D>().enabled = false;
        
        if (photonView.IsMine)
        {
            //全部僵尸数减一
            GameObject zombieManagement = GameObject.Find("Zombie Management");
            if (zombieManagement != null)
            {
                zombieManagement.GetComponent<ZombieManagement>().minusZombieNumAll();
            }
            else
            {
                Debug.LogError("[Zombie] 未找到Zombie Management对象");
            }
        }
        
        //隐藏头
        hideHead();
        //动画切换
        myAnimator.SetBool("Walk", false);
        myAnimator.SetBool("Die", true);

        // 触发死亡事件
        OnDeath?.Invoke(this);
        
        Debug.Log($"[Zombie] 僵尸{gameObject.name}死亡处理完成");
    }

    //可能不同僵尸隐藏头的方法不同，具体实现需要根据实际情况来写
    protected virtual void hideHead()
    {

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
        if(bloodVolume - 1800 <= 0)
        {
            beAttacked(1800);
            GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
            //僵尸死亡
            Destroy(gameObject);
        }
        else{
            beAttacked(1800);
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
        if (photonView.IsMine && gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }
        sleep = false;
    }

    //设置位置行，影响渲染顺序
    public void setPosRow(int pos)
    {
        pos_row = pos;
        
        if (photonView.IsMine)
        {
            // 新增Sprite Library处理
            ProcessSpriteLibraryComponents();
            
            // 设置渲染层级
            SetRenderSettingsRecursive(transform);
            orderOffset++;
        }
    }

    private void ProcessSpriteLibraryComponents()
    {
        // 获取所有Sprite Library组件（包括子物体）
        UnityEngine.U2D.Animation.SpriteLibrary[] libraries = GetComponentsInChildren<UnityEngine.U2D.Animation.SpriteLibrary>(true);
        foreach (var library in libraries)
        {
            // 强制刷新Sprite Library
            library.spriteLibraryAsset = library.spriteLibraryAsset;
            
            // 延迟设置确保渲染器更新
            StartCoroutine(DelaySetRenderSettings(library.gameObject));
        }
    }

    IEnumerator DelaySetRenderSettings(GameObject target)
    {
        yield return new WaitForEndOfFrame();
        
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Zombie-" + pos_row;
            sr.sortingOrder += orderOffset * 20;
        }
    }

    private void SetRenderSettingsRecursive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // 处理当前物体
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Zombie-" + pos_row;
                sr.sortingOrder += orderOffset * 20;
            }

            // 处理子物体
            if (child.childCount > 0)
            {
                SetRenderSettingsRecursive(child);
            }
        }
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 只同步关键状态
        if (stream.IsWriting)
        {
            stream.SendNext(isDead);
            stream.SendNext(bloodVolume);
        }
        else
        {
            isDead = (bool)stream.ReceiveNext();
            bloodVolume = (int)stream.ReceiveNext();
        }
    }
}

public enum ZombieState { Normal, Cold, Parasiticed }