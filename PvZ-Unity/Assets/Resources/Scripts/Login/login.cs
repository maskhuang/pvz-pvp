using UnityEngine;
using TMPro;    // �����ʹ�õ��� TextMeshPro ��ص� UI����Ҫ������������ռ�
using UnityEngine.UI; // ���Ҫʹ����ͨ�� UI �����������UnityEngine.UI

public class LoginManager : MonoBehaviour
{
    // ���� TextMeshPro Input Field
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    // �������ʹ����ͨ InputField������Ϊ��
    // public InputField usernameInput;
    // public InputField passwordInput;

    public Button loginButton;

    private void Start()
    {
        // ����ť��ӵ������
        loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    private void OnLoginButtonClick()
    {
        // ��������ȡ�û�����
        string username = usernameInput.text;
        string password = passwordInput.text;

        // ������Խ�����ĵ�¼�߼������籾��У��������������
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Username or Password is empty!");
        }
        else
        {
            // TODO: ��������֤��ִ�������߼�
            Debug.Log("Attempting login with Username: " + username + ", Password: " + password);
        }
    }
}
