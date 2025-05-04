using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using TMPro;

public class NetworkTimer : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
    [Header("配置参数")]
    [SerializeField] float syncInterval = 1f; // 同步间隔
    [SerializeField] float maxDeviation = 0.2f; // 最大允许偏差
    [SerializeField] public float duration;  // 添加public访问修饰符

    // 运行时状态
    double startTime;
    double pausedTime;
    bool isRunning;
    bool isPaused;

    // 网络补偿
    double lastSyncTime;
    double timeDeviation;

    void Awake()
    {
        // 自动添加到Photon回调监听
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void StartTimer(float seconds)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        duration = seconds;
        startTime = PhotonNetwork.Time;
        isRunning = true;
        isPaused = false;
        
        photonView.RPC("RPC_StartTimer", RpcTarget.AllBuffered, startTime, duration);
    }

    [PunRPC]
    void RPC_StartTimer(double serverStartTime, float timerDuration)
    {
        startTime = serverStartTime;
        duration = timerDuration;
        isRunning = true;
        isPaused = false;
        timeDeviation = 0;
    }

    public void PauseTimer()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isPaused = true;
        pausedTime = PhotonNetwork.Time;
        photonView.RPC("RPC_PauseTimer", RpcTarget.AllBuffered, pausedTime);
    }

    [PunRPC]
    void RPC_PauseTimer(double pauseTime)
    {
        isPaused = true;
        pausedTime = pauseTime;
    }

    public void ResumeTimer()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        isPaused = false;
        startTime += PhotonNetwork.Time - pausedTime;
        photonView.RPC("RPC_ResumeTimer", RpcTarget.AllBuffered, startTime);
    }

    [PunRPC]
    void RPC_ResumeTimer(double newStartTime)
    {
        startTime = newStartTime;
        isPaused = false;
    }

    void Update()
    {
        if (!isRunning) return;

        // 主客户端定期同步
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.Time - lastSyncTime > syncInterval)
        {
            photonView.RPC("RPC_SyncTime", RpcTarget.Others, GetRemainingTime());
            lastSyncTime = PhotonNetwork.Time;
        }

        // 计算剩余时间
        float remaining = GetRemainingTime();
        if (remaining <= 0)
        {
            isRunning = false;
            OnTimerEnd();
        }
    }

    [PunRPC]
    void RPC_SyncTime(float masterRemaining)
    {
        float localRemaining = GetRemainingTime();
        timeDeviation = masterRemaining - localRemaining;

        // 偏差超过阈值时进行补偿
        if (Mathf.Abs((float)timeDeviation) > maxDeviation)
        {
            startTime += timeDeviation;
            Debug.Log($"时间补偿: {timeDeviation:F2}秒");
        }
    }

    public float GetRemainingTime()
    {
        if (!isRunning) return 0;
        if (isPaused) return (float)(duration - (pausedTime - startTime));
        
        double elapsed = PhotonNetwork.Time - startTime;
        return Mathf.Clamp((float)(duration - elapsed), 0, duration);
    }

    void OnTimerEnd()
    {
        // 触发游戏事件，例如开始僵尸波次
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_TimerEnd", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_TimerEnd()
    {
        Debug.Log("计时结束");
        // 在此处添加游戏逻辑
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 新玩家加入时同步状态
            photonView.RPC("RPC_StartTimer", newPlayer, startTime, duration);
            if (isPaused)
            {
                photonView.RPC("RPC_PauseTimer", newPlayer, pausedTime);
            }
        }
    }
} 