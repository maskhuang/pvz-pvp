using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    public static PhotonNetworkManager Instance { get; private set; }
    public event Action OnGameStart;
    private const string GAME_VERSION = "1.0.0";

    private bool gameStarted = false;

    private void Awake()
    {
        Debug.Log("[Photon] 初始化网络管理器");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PhotonNetwork.AutomaticallySyncScene = true;

            // 设置游戏版本
            PhotonNetwork.GameVersion = GAME_VERSION;
            // 允许在房间列表中看到房间
            PhotonNetwork.EnableCloseConnection = false;
            
            Debug.Log($"[Photon] 游戏版本: {GAME_VERSION}");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Connect()
    {
        try
        {
            Debug.Log("[Photon] 尝试连接到服务器...");
            Debug.Log($"[Photon] 当前连接状态: {PhotonNetwork.NetworkClientState}");

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
                //PhotonNetwork.ConnectToRegion("asia"); // 连接到亚洲服务器
            }
            else
            {
                Debug.Log("[Photon] 已经连接到服务器，尝试加入房间");
                JoinOrCreateRoom();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Photon] 连接过程中发生错误: {e.Message}");
        }
    }

    private void JoinOrCreateRoom()
    {
        try
        {
            Debug.Log("[Photon] 尝试创建或加入房间");
            
            // 设置房间过滤条件
            ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { "GameType", "PvZ" } };
            PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 2);
            Debug.Log("[Photon] 尝试加入包含指定属性的随机房间");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Photon] 加入房间时发生错误: {e.Message}");
            CreateNewRoom();
        }
    }

    private void CreateNewRoom()
    {
        try 
        {
            Debug.Log("[Photon] 开始创建新房间");
            
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2,
                IsVisible = true,
                IsOpen = true,
                PublishUserId = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "GameType", "PvZ" },
                    { "Level", "5" }
                },
                CustomRoomPropertiesForLobby = new string[] { "GameType", "Level" }
            };

            // 使用固定房间名前缀加随机数，避免冲突
            string roomName = $"PvZ_Room_{UnityEngine.Random.Range(1000, 9999)}";
            PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
            Debug.Log($"[Photon] 尝试创建房间: {roomName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Photon] 创建房间失败: {e.Message}");
            // 如果创建失败，等待后重试
            Invoke("JoinOrCreateRoom", 2f);
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon][CALLBACK] OnConnectedToMaster called.");
        Debug.Log("[Photon] 已连接到主服务器");
        Debug.Log($"[Photon] 服务器地区: {PhotonNetwork.CloudRegion}");
        Debug.Log($"[Photon] 用户ID: {PhotonNetwork.LocalPlayer.UserId}");
        
        // 加入大厅
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("[Photon] 正在加入大厅...");
        }
        else
        {
            JoinOrCreateRoom();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Photon][CALLBACK] OnJoinedLobby called.");
        Debug.Log("[Photon] 已加入大厅，准备加入房间");
        
        // 设置玩家昵称
        string nickname = $"Player_{UnityEngine.Random.Range(1000, 9999)}";
        PhotonNetwork.NickName = nickname;
        Debug.Log($"[Photon] 设置玩家昵称: {nickname}");
        
        // 尝试加入房间
        JoinOrCreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[Photon][CALLBACK] OnJoinRandomFailed called.");
        Debug.Log($"[Photon] 加入随机房间失败: {message}");
        CreateNewRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log("[Photon][CALLBACK] OnJoinRoomFailed called.");
        Debug.Log($"[Photon] 加入指定房间失败: {message}, 错误码: {returnCode}");
        // 等待一段时间后重试
        Invoke("JoinOrCreateRoom", 2f);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("[Photon][CALLBACK] OnCreateRoomFailed called.");
        Debug.LogError($"[Photon] 创建房间失败: {message}, 错误码: {returnCode}");
        // 等待一段时间后重试
        Invoke("JoinOrCreateRoom", 2f);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[Photon][CALLBACK] OnJoinedRoom called.");
        Debug.Log($"[Photon] 成功加入房间: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"[Photon] 房间属性: 可见={PhotonNetwork.CurrentRoom.IsVisible}, 开放={PhotonNetwork.CurrentRoom.IsOpen}");
        Debug.Log($"[Photon] 当前房间人数: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        Debug.Log($"[Photon] 玩家角色: {(PhotonNetwork.IsMasterClient ? "房主" : "访客")}");
        
        // 打印房间的所有属性
        Debug.Log("[Photon] 房间属性列表:");
        foreach (var prop in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            Debug.Log($"[Photon] {prop.Key} = {prop.Value}");
        }
        
        tryStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Photon] 新玩家加入: {newPlayer.NickName}");
        Debug.Log($"[Photon] 当前房间人数: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        tryStartGame();
    }
    private void tryStartGame(){
        if(!gameStarted && PhotonNetwork.CurrentRoom.PlayerCount == 2){
            GetComponent<PhotonView>().RPC("StartGame", RpcTarget.All);
            gameStarted = true;
        }
    }

    [PunRPC]
    private void StartGame()
    {
        Debug.Log("[Photon] 收到开始游戏RPC");
        OnGameStart?.Invoke();
        Debug.Log("[Photon] 游戏开始事件已触发");
    }

    public void SyncZombieDeath(int row, string zombieType)
    {
        Debug.Log($"[Photon] 同步僵尸死亡: 行={row}, 类型={zombieType}");
        GetComponent<PhotonView>().RPC("SpawnZombieForOpponent", RpcTarget.Others, row, zombieType);
    }

    [PunRPC]
    private void SpawnZombieForOpponent(int row, string zombieType)
    {
        Debug.Log($"[Photon] 收到生成僵尸RPC: 行={row}, 类型={zombieType}");
        GameObject zombieManagement = GameObject.Find("Zombie Management");
        if (zombieManagement != null)
        {
            zombieManagement.GetComponent<ZombieManagement>().SpawnZombieInRow(row, zombieType);
            Debug.Log("[Photon] 僵尸生成完成");
        }
        else
        {
            Debug.LogError("[Photon] 未找到Zombie Management对象");
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Photon][CALLBACK] OnDisconnected called. Cause: {cause}");
        base.OnDisconnected(cause);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Photon] 玩家离开: {otherPlayer.NickName}");
        Debug.Log($"[Photon] 当前房间人数: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[Photon] 房间列表更新，当前可用房间数: {roomList.Count}");
        foreach (RoomInfo room in roomList)
        {
            Debug.Log($"[Photon] 房间: {room.Name}, 玩家数: {room.PlayerCount}/{room.MaxPlayers}");
        }
    }
} 