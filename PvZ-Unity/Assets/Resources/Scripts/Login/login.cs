using UnityEngine;
using TMPro;    // 如果你使用的是 TextMeshPro 相关的 UI，需要引入这个命名空间
using UnityEngine.UI; // 如果要使用普通的 UI 组件，需引入UnityEngine.UI

public class LoginManager : MonoBehaviour
{
    // 对于 TextMeshPro Input Field
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    // 如果你是使用普通 InputField，则定义为：
    // public InputField usernameInput;
    // public InputField passwordInput;

    public Button loginButton;

    private void Start()
    {
        // 给按钮添加点击监听
        loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    private void OnLoginButtonClick()
    {
        // 从输入框获取用户输入
        string username = usernameInput.text;
        string password = passwordInput.text;

        // 这里可以进行你的登录逻辑，例如本地校验或者请求服务器
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Username or Password is empty!");
        }
        else
        {
            // TODO: 请求后端验证或执行其他逻辑
            Debug.Log("Attempting login with Username: " + username + ", Password: " + password);
        }
    }
}
