using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagement : MonoBehaviour
{
    public int level;   //��ǰ�ؿ����
    private LevelController levelController;
    public static LevelData levelData;   //��ǰ�ؿ�����

    public List<GameObject> awakeList;  //�������б������ڿ�������������Ѹû��ѵĶ���

    public GameObject endMenuPanel;   //��Ϸ�������
    public GameObject background;   //��������
    public GameObject zombieManagement;   //��ʬ��������
    public GameObject uiManagement;   //UI��������

    private void Awake()
    {
        levelController = 
            (LevelController)gameObject.AddComponent(Type.GetType("Level" + level + "Controller"));
        levelController.init();

        //���ر���ͼƬ
        background.GetComponent<SpriteRenderer>().sprite =
            Resources.Load<Sprite>("Sprites/Background/Background" + levelData.mapSuffix);
        //���ñ�������
        background.GetComponent<BGMusicControl>()
            .changeMusic("Music" + levelData.backgroundSuffix);

        //���ض�Ӧ����ֲ�������
        GameObject pm = Instantiate(
            Resources.Load<GameObject>(
                "Prefabs/PlantingManagement/PlantingManagement" + levelData.plantingManagementSuffix),
            new Vector3(0, 0, 0),
            Quaternion.Euler(0, 0, 0)
        );
        pm.name = "Planting Management";

        //����UI
        uiManagement.GetComponent<UIManagement>().initUI();

        //只有不是第5关时才加载对话框
        if (level != 5)
        {
            //ضԻ
            Instantiate(Resources.Load<UnityEngine.Object>("Prefabs/UI/DialogPanel/DialogPanel-Level" + level),
                        new Vector3(0, 0, 0),
                        Quaternion.Euler(0, 0, 0),
                        GameObject.Find("TopCanvas").transform);
        }
    }

    public void awakeAll()
    {
        foreach (GameObject gameObject in awakeList)
        {
            gameObject.SetActive(true);
        }
        uiManagement.GetComponent<UIManagement>().appear();
        zombieManagement.GetComponent<ZombieManagement>().activate();
        levelController.activate();
    }

    public void gameOver()
    {
        endMenuPanel.GetComponent<EndMenu>().gameOver();
    }

    public void win()
    {
        endMenuPanel.GetComponent<EndMenu>().win();
    }
}