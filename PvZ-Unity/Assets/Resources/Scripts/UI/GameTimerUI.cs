using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameTimerUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] TextMeshProUGUI timerText;  // 绑定到Text组件
    [SerializeField] string timeFormat = "mm':'ss";  // 时间显示格式

    [Header("计时器引用")]
    [SerializeField] NetworkTimer networkTimer;

    void Update()
    {
        if (networkTimer == null) return;

        // 获取经过时间（秒）
        float elapsedTime = networkTimer.duration - networkTimer.GetRemainingTime();
        
        // 转换为时间格式
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(elapsedTime);
        timerText.text = timeSpan.ToString(timeFormat);

        // 可选：添加网络状态提示
        if (!PhotonNetwork.IsConnected)
        {
            timerText.text += "\n<color=red>连接中...</color>";
        }
    }
} 