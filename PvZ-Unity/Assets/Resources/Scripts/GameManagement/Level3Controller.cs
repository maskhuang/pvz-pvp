using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level3Controller : LevelController
{

    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 3,   //�ؿ����
            levelName = "����֮��",   //�ؿ���

            mapSuffix = "_Night_Bone", //��ͼͼƬ��׺
            rowCount = 5,       //�ܹ�����
            landRowCount = 5,   //����½��
            isDay = false,       //�Ƿ����
            plantingManagementSuffix = "_OriginalLawn",   //��Ӧ����ֲ���������׺
            backgroundSuffix = "_Night_Bone",   //��Ӧ�������ֺ�׺

            //���н�ʬ��ʼY��λ��
            zombieInitPosY = new List<float> { -2.3f, -1.25f, -0.35f, 0.7f, 1.7f },

            //����ֲ�￨������
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash"
            }
        };
    }

    public override void activate()
    {
        Invoke("createFirstGhost", 45f);
    }

    private void createFirstGhost()
    {
        // 在随机行生成幽灵僵尸
        GameObject.Find("Zombie Management").GetComponent<ZombieManagement>()
            .createGhost(UnityEngine.Random.Range(0, GameManagement.levelData.landRowCount));
    }
}
