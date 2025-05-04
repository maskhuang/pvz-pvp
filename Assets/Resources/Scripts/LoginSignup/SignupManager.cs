using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class SignupManager : MonoBehaviour
{
    // 将 Inspector 中对应的 UI 控件拖入这些变量
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button signupBotton;
    public Button toLogInScene;
    public Text messageText;

    // 服务器 API 接口地址（替换成你实际部署的地址）
    private string signupUrl = "http://localhost:3000/api/signup";

    // 当点击登录按钮时调用
    public void OnSignupButtonClicked()
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
        StartCoroutine(SignupRoutine(username, password));
    }

    public void OnToLogInSceneClicked()
    {
        SceneManager.LoadScene("Login_Scene");
    }

    // 利用 UnityWebRequest 发送 POST 请求到服务器进行验证
    // 示例： SignupRoutine 协程
    IEnumerator SignupRoutine(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(signupUrl, form))
        {
            yield return www.SendWebRequest();

            // 先获取原始响应内容（无论成功或失败都能获取）
            string rawResponse = www.downloadHandler.text;

            // 检查网络请求是否成功
            if (www.result == UnityWebRequest.Result.Success)
            {
                // 200 OK
                // 这时 rawResponse 里通常是后端成功返回的 JSON
                SignupResponse response = JsonUtility.FromJson<SignupResponse>(rawResponse);
                if (response.success)
                {
                    messageText.text = "创建成功！";
                }
                else
                {
                    // 即使返回 200，但 success = false，后端可能携带了错误信息
                    messageText.text = "创建失败: " + response.message;
                }
            }
            else
            {
                // 非 200： 比如 400 Bad Request, 401 Unauthorized, 500 等
                // 1) 先从 rawResponse 里尝试解析后端返回的 JSON
                // 2) 如果后端确实返回了 { success: false, message: "xxx" }，就能解析到具体信息
                SignupResponse response = null;
                try
                {
                    response = JsonUtility.FromJson<SignupResponse>(rawResponse);
                }
                catch
                {
                    // 如果无法解析成 JSON，可能后端没返回 JSON，或者格式不匹配
                    response = null;
                }

                if (response != null && !string.IsNullOrEmpty(response.message))
                {
                    // 使用后端返回的详细信息
                    messageText.text = "请求失败: " + response.message;
                }
                else
                {
                    // 如果依旧没法获取更多信息，只能展示最简错误
                    messageText.text = "请求失败: " + www.error;
                }
            }
        }
    }

}

// 对应服务器返回的数据结构，需要与服务器返回的 JSON 格式保持一致
[System.Serializable]
public class SignupResponse
{
    public bool success;
    public string message;
}
