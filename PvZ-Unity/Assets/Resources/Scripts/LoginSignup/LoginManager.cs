using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    // 将 Inspector 中对应的 UI 控件拖入这些变量
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginBotton;
    public Button toSignUpScene;
    public Text messageText;

    // 服务器 API 接口地址（替换成你实际部署的地址）
    private string loginUrl = "http://localhost:3000/api/login";

    // 当点击登录按钮时调用
    public void OnLoginButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        // 简单非空验证
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            messageText.text = "用户名和密码不能为空";
            return;
        }

        // 调用异步协程进行登录操作
        StartCoroutine(LoginRoutine(username, password));
    }

    public void OnToSignUpSceneClicked()
    {
        SceneManager.LoadScene("Signup_Scene");
    }

    // 利用 UnityWebRequest 发送 POST 请求到服务器进行验证
    IEnumerator LoginRoutine(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(loginUrl, form))
        {
            yield return www.SendWebRequest();

            // 检查网络请求状态
            if (www.result != UnityWebRequest.Result.Success)
            {
                messageText.text = "请求失败: " + www.error;
            }
            else
            {
                string result = www.downloadHandler.text;
                // 解析服务器返回的 JSON 格式数据
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(result);
                if (response.success)
                {
                    messageText.text = "登录成功！";
                    // 可在此处切换场景或执行后续业务逻辑
                }
                else
                {
                    messageText.text = "登录失败: " + response.message;
                }
            }
        }
    }
}

// 对应服务器返回的数据结构，需要与服务器返回的 JSON 格式保持一致
[System.Serializable]
public class LoginResponse
{
    public bool success;
    public string message;
}
