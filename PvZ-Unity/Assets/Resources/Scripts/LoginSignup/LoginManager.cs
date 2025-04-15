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

        // ����Э�̽��е�¼
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
            // �ȴ��������
            yield return www.SendWebRequest();

            // ��˷��ص�ԭʼ��Ӧ���ݣ����۳ɹ���ʧ�ܣ�
            string rawResponse = www.downloadHandler.text;

            // �ж� HTTP �������Ƿ�ɹ���״̬�� 200~299��
            if (www.result == UnityWebRequest.Result.Success)
            {
                // ����ɹ������Խ��� JSON
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(rawResponse);
                if (response.success)
                {
                    messageText.text = "��¼�ɹ���";
                    // ��ת����������ִ�к����߼�
                    SceneManager.LoadScene("Main_Scene");
                }
                else
                {
                    // ��ʹ�� 200 ״̬�룬�����Ҳ���ܷ��� { success=false, message=... }
                    messageText.text = "��¼ʧ��: " + response.message;
                }
            }
            else
            {
                // ����״̬�루401��400��500�ȣ�
                // �Գ��Խ��� JSON�����Ƿ��ܻ�ȡ��˷��ص���ϸ����
                try
                {
                    LoginResponse response = JsonUtility.FromJson<LoginResponse>(rawResponse);
                    // ����ɹ������� { success=false, message="..." }
                    messageText.text = "��¼ʧ��: " + response.message;
                }
                catch
                {
                    // �������ʧ�ܣ�ֻ����ʾ��򵥵Ĵ�����ʾ
                    messageText.text = "����ʧ��: " + www.error;
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
