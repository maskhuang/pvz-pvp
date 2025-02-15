using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level2Controller : LevelController
{
    protected override void Start()
    {
        GameObject.Find("Zombie Management").GetComponent<ZombieManagement>()
            .generateFunc(UnityEngine.Random.Range(0, 5), "ZombieNormal");
    }

    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 2,   //�ؿ����
            levelName = "��ʬ��",   //�ؿ���

            mapSuffix = "_Night_Wall", //��ͼͼƬ��׺
            rowCount = 5,       //�ܹ�����
            landRowCount = 5,   //����½��
            isDay = false,       //�Ƿ����
            plantingManagementSuffix = "_Wall",   //��Ӧ����ֲ���������׺
            backgroundSuffix = "_Night_Wall",   //��Ӧ�������ֺ�׺

            //���н�ʬ��ʼY��λ��
            zombieInitPosY = new List<float> { -2.45f, -1.5f, -0.5f, 0.55f, 1.55f },

            //����ֲ�￨������
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut"
            }
        };
    }
}
