using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class SignupManager : MonoBehaviour
{
    // �� Inspector �ж�Ӧ�� UI �ؼ�������Щ����
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button signupBotton;
    public Button toLogInScene;
    public Text messageText;

    // ������ API �ӿڵ�ַ���滻����ʵ�ʲ���ĵ�ַ��
    private string signupUrl = "http://localhost:3000/api/signup";

    // �������¼��ťʱ����
    public void OnSignupButtonClicked()
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
        StartCoroutine(SignupRoutine(username, password));
    }

    public void OnToLogInSceneClicked()
    {
        SceneManager.LoadScene("Login_Scene");
    }

    // ���� UnityWebRequest ���� POST ���󵽷�����������֤
    // ʾ���� SignupRoutine Э��
    IEnumerator SignupRoutine(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post(signupUrl, form))
        {
            yield return www.SendWebRequest();

            // �Ȼ�ȡԭʼ��Ӧ���ݣ����۳ɹ���ʧ�ܶ��ܻ�ȡ��
            string rawResponse = www.downloadHandler.text;

            // ������������Ƿ�ɹ�
            if (www.result == UnityWebRequest.Result.Success)
            {
                // 200 OK
                // ��ʱ rawResponse ��ͨ���Ǻ�˳ɹ����ص� JSON
                SignupResponse response = JsonUtility.FromJson<SignupResponse>(rawResponse);
                if (response.success)
                {
                    messageText.text = "�����ɹ���";
                }
                else
                {
                    // ��ʹ���� 200���� success = false����˿���Я���˴�����Ϣ
                    messageText.text = "����ʧ��: " + response.message;
                }
            }
            else
            {
                // �� 200�� ���� 400 Bad Request, 401 Unauthorized, 500 ��
                // 1) �ȴ� rawResponse �ﳢ�Խ�����˷��ص� JSON
                // 2) ������ȷʵ������ { success: false, message: "xxx" }�����ܽ�����������Ϣ
                SignupResponse response = null;
                try
                {
                    response = JsonUtility.FromJson<SignupResponse>(rawResponse);
                }
                catch
                {
                    // ����޷������� JSON�����ܺ��û���� JSON�����߸�ʽ��ƥ��
                    response = null;
                }

                if (response != null && !string.IsNullOrEmpty(response.message))
                {
                    // ʹ�ú�˷��ص���ϸ��Ϣ
                    messageText.text = "����ʧ��: " + response.message;
                }
                else
                {
                    // �������û����ȡ������Ϣ��ֻ��չʾ������
                    messageText.text = "����ʧ��: " + www.error;
                }
            }
        }
    }

}

// ��Ӧ���������ص����ݽṹ����Ҫ����������ص� JSON ��ʽ����һ��
[System.Serializable]
public class SignupResponse
{
    public bool success;
    public string message;
}
