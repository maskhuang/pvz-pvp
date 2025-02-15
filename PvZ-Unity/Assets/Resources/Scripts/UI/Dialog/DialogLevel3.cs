using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogLevel3 : MonoBehaviour
{
    public SpeechBubble flowerSpeechBubble;   //���տ��Ի���
    public SpeechBubble squashSpeechBubble;    //�ѹ϶Ի���
    public SpeechBubble peaSpeechBubble;   //�㶹�Ի���
    public GameObject introduce1;   //��ʬ������1
    public GameObject introduce2;   //��ʬ������2

    GameObject flower;
    GameObject squash;
    GameObject pea;

    int count = 0;  //�Ի���������ǰ�ǵڼ����Ի�

    private void Awake()
    {
        //��ֲ����Ի���ֲ��
        flower = GameObject.Find("Plant-2-2")
            .GetComponent<PlantGrid>().plantByGod("SunFlowerForDialog");
        pea = GameObject.Find("Plant-5-2")
            .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
    }

    // Start is called before the first frame update
    void Start()
    {
        Invoke("flipPea", 1f);
        Invoke("flipPea", 2f);
        Invoke("peaHide", 3f);
        Invoke("showFirstTalk", 3.5f);
    }

    // Update is called once per frame
    void Update()
    {
        //�����������������һ�¼�
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            switch (count)
            {
                case 1:
                    flowerSpeechBubble.showDialog("ȷʵ���");
                    count++;
                    break;
                case 2:
                    flowerSpeechBubble.showDialog("���㶹��ʿ���㲻���ö㵽ָ�ӹٱ�����һ�ֳ�����");
                    count++;
                    break;
                case 3:
                    peaSpeechBubble.showDialog("��......");
                    count++;
                    break;
                case 4:
                    squash = GameObject.Find("Plant-5-2")
                        .GetComponent<PlantGrid>().plantByGod("SquashForDialog");
                    Invoke("squashTalk", 1f);
                    count = -1;
                    break;
                case 5:
                    peaSpeechBubble.showDialog("�ѹ���ξ������");
                    count++;
                    break;
                case 6:
                    squashSpeechBubble.showDialog("��Ϊսʿ�����Ǿ�Ӧ����ֱǰ");
                    count++;
                    break;
                case 7:
                    pea.GetComponent<Plant>().die("");
                    pea = GameObject.Find("Plant-6-2")
                        .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
                    Invoke("peaTalk2", 0.5f);
                    count = -1;
                    break;
                case 8:
                    GameObject.Find("Zombie Management").GetComponent<ZombieManagement>()
                        .createZombieByGod(3, "GhostZombie");
                    Invoke("peaAmazed", 1f);
                    count = -1;
                    break;
                case 9:
                    squashSpeechBubble.showDialog("��С��");
                    count++;
                    break;
                case 10:
                    introduce1.SetActive(true);

                    //�Ի�ֲ����ʧ
                    flower.GetComponent<Plant>().die("");
                    squash.GetComponent<Plant>().die("");
                    pea.GetComponent<Plant>().die("");

                    count++;
                    break;
                default:
                    break;
            }
        }
    }

    private void flipPea()
    {
        pea.GetComponent<SpriteRenderer>().flipX = !(pea.GetComponent<SpriteRenderer>().flipX);
    }

    private void showFirstTalk()
    {
        peaSpeechBubble.showDialog("���տ�ָ�ӹ٣��������ɭ��");
        count++;
    }

    private void peaTalk2()
    {
        peaSpeechBubble.transform.localPosition += new Vector3(300, 0, 0);
        peaSpeechBubble.showDialog("��˵�öԣ���Ϊսʿ����һ���ᵲ�ڳ���ǰ���");
        count = 8;
    }

    private void peaAmazed()
    {
        Instantiate(
            Resources.Load<GameObject>("Prefabs/UI/Effect/Effect_Amazed"),
            pea.transform.position + new Vector3(0.32f, 0.57f, 0),
            Quaternion.Euler(0, 0, 0),
            pea.transform
        );
        Invoke("peaHide", 1f);
    }

    private void peaHide()
    {
        pea.GetComponent<Plant>().die("");
        pea = GameObject.Find("Plant-1-2")
            .GetComponent<PlantGrid>().plantByGod("PeaShooterSingle");
        if(count == -1)
        {
            peaSpeechBubble.transform.localPosition -= new Vector3(300, 0, 0);
            peaSpeechBubble.showDialog("��*����ʲô����");
            count = 9;
        }
            
    }

    private void squashTalk()
    {
        squashSpeechBubble.showDialog("���տ�ָ�ӹ�˵�ö�");
        count = 5;
    }

    public void clickNext()
    {
        introduce1.SetActive(false);
        introduce2.SetActive(true);
    }

    public void clickStart()
    {
        AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>("Sounds/UI/graveButtonClick"),
                new Vector3(0, 0, -10)
            );
        introduce2.SetActive(false);

        GameObject.Find("Game Management").GetComponent<GameManagement>().awakeAll();
        gameObject.SetActive(false);
    }
}
