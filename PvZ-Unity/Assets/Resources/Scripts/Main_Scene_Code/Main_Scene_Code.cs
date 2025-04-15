using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class Main_Scene_Code : MonoBehaviour
{
    // 将 Inspector 中对应的 UI 控件拖入这些变量
    public Button toLogInScene;

    // 当点击登录按钮时调用
    public void OntoLogInSceneButtonClicked()
    {
        SceneManager.LoadScene("Login_Scene");
    }

}
