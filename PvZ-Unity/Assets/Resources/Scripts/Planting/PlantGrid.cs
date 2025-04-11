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

    bool havePlanted = false;   //该格是否已种植植物
    GameObject nowPlant;    //当前种植的植物

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

    private void OnMouseEnter()
    {
        if(havePlanted == false && toBePlanted.activeSelf == true)
        {
            // 根据要种植的植物类型决定如何显示预览
            if (toBePlanted.GetComponent<SpriteRenderer>() != null)
            {
                spriteRenderer.sprite = toBePlanted.GetComponent<SpriteRenderer>().sprite;
            }
            // 如果是使用MeshRenderer的植物，不显示预览（或者可以用其他方式显示）
        }
        else if(havePlanted == true && selectedShovel.activeSelf == true)
        {
            nowPlant.GetComponent<Plant>().highlight();
        }
    }

    private void OnMouseExit()
    {
        if (havePlanted == false && toBePlanted.activeSelf == true)
        {
            spriteRenderer.sprite = null;
        }
        else if (havePlanted == true && selectedShovel.activeSelf == true)
        {
            nowPlant.GetComponent<Plant>().cancelHighlight();
        }
    }

    private void OnMouseDown()
    {
        if (havePlanted == false && toBePlanted.activeSelf == true)
        {
            plant(toBePlanted.GetComponent<ToBePlanted>().plantName);
        }
        else if (havePlanted == true && selectedShovel.activeSelf == true)
        {
            nowPlant.GetComponent<Plant>().die("shovelPlant");
        }
    }

    #endregion

    #region 私有自定义函数

    #endregion

    #region 公有自定义函数

    public void plant(string name)
    {
        //清除阴影
        spriteRenderer.sprite = null;
        
        havePlanted = true;   //已种植

        //生成植物
        Debug.Log("[PlantGrid] 正在实例化植物: " + name);
        nowPlant = Instantiate(Resources.Load<GameObject>("Prefabs/Plants/" + name),
                                transform.position + new Vector3(0, 0, 5),
                                Quaternion.Euler(0, 0, 0),
                                transform);
        
        nowPlant.GetComponent<Plant>().initialize(
            this,
            spriteRenderer.sortingLayerName,
            spriteRenderer.sortingOrder
        );

        //播放音效
        audioSource.clip =
            Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/plant");
        audioSource.Play();

        //向PlantingManagement发送消息以处理UI相关事件
        GameObject.Find("Planting Management").GetComponent<PlantingManagement>().plant();
    }

    //上帝模式种植，用于关卡开始或回合开始种植
    public GameObject plantByGod(string name)
    {
        havePlanted = true;   //已种植

        //生成植物
        nowPlant = Instantiate(Resources.Load<GameObject>("Prefabs/Plants/" + name),
                                          transform.position + new Vector3(0, 0, 5),
                                          Quaternion.Euler(0, 0, 0),
                                          transform);
        
        nowPlant.GetComponent<Plant>().initialize(
            this,
            spriteRenderer.sortingLayerName,
            spriteRenderer.sortingOrder
        );

        return nowPlant;
    }

    public void plantDie(string reason)
    {
        havePlanted = false;   //已没有植物

        AudioClip clip = null;
        if (reason != "") clip = Resources.Load<AudioClip>("Sounds/Plants/" + reason);
        if (clip != null)
        {
            //播放音效
            audioSource.clip = clip; 
            audioSource.Play();
        }
    }

    #endregion
}
