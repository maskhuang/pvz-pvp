using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    // �� Inspector �ж�Ӧ�� UI �ؼ�������Щ����
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginBotton;
    public Button toSignUpScene;
    public Text messageText;

    // ������ API �ӿڵ�ַ���滻����ʵ�ʲ���ĵ�ַ��
    private string loginUrl = "http://localhost:3000/api/login";

    // �������¼��ťʱ����
    public void OnLoginButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        // �򵥷ǿ���֤
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            messageText.text = "�û��������벻��Ϊ��";
            return;
        }

        // �����첽Э�̽��е�¼����
        StartCoroutine(LoginRoutine(username, password));
    }

    public void OnToSignUpSceneClicked()
    {
        SceneManager.LoadScene("Signup_Scene");
    }

    // ���� UnityWebRequest ���� POST ���󵽷�����������֤
    IEnumerator LoginRoutine(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(loginUrl, form))
        {
            yield return www.SendWebRequest();

            // �����������״̬
            if (www.result != UnityWebRequest.Result.Success)
            {
                messageText.text = "����ʧ��: " + www.error;
            }
            else
            {
                string result = www.downloadHandler.text;
                // �������������ص� JSON ��ʽ����
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(result);
                if (response.success)
                {
                    messageText.text = "��¼�ɹ���";
                    // ���ڴ˴��л�������ִ�к���ҵ���߼�
                }
                else
                {
                    messageText.text = "��¼ʧ��: " + response.message;
                }
            }
        }
    }
}

// ��Ӧ���������ص����ݽṹ����Ҫ����������ص� JSON ��ʽ����һ��
[System.Serializable]
public class LoginResponse
{
    public bool success;
    public string message;
}
