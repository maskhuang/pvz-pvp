using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class Main_Scene_Code : MonoBehaviour
{
    // �� Inspector �ж�Ӧ�� UI �ؼ�������Щ����
    public Button toLogInScene;

    // �������¼��ťʱ����
    public void OntoLogInSceneButtonClicked()
    {
        SceneManager.LoadScene("Login_Scene");
    }

}
