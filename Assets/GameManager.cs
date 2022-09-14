using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Stats")]
    public bool gameEnded = false;
    public float timeToDie;
    public float invincibleDuration;
    private float hatPickupTime;

    [Header("Players")]
    public string playerPrefabLocation;
    public Transform[] spawnPoints;
    public PlayerController[] players;
    public int playerWithHat;
    private int playersInGame;

    [HideInInspector]
    public int playersNum;
    public int playersDead;

    public static GameManager instance;

    void Awake()
    {
        // instance
        instance = this;
    }

    void Start()
    {
        players = new PlayerController[PhotonNetwork.PlayerList.Length];
        photonView.RPC("ImInGame", RpcTarget.AllBuffered);
    }

    [PunRPC]
    void ImInGame()
    {
        playersInGame++;
        playersNum++;
        if (playersInGame == PhotonNetwork.PlayerList.Length)
            SpawnPlayer();
    }

    void SpawnPlayer()
    {
        GameObject playerObj = PhotonNetwork.Instantiate(playerPrefabLocation, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.Euler(0, 45, 0));
        PlayerController playerScript = playerObj.GetComponent<PlayerController>();
        playerScript.photonView.RPC("Initialize", RpcTarget.All, PhotonNetwork.LocalPlayer);
    }

    public PlayerController GetPlayer (int playerId)
    {
        return players.First(x => x.id == playerId);
    }

    public PlayerController GetPlayer(GameObject playerObj)
    {
        return players.First(x => x.gameObject == playerObj);
    }

    [PunRPC]
    public void GiveHat (int playerId, bool initialGive)
    {
        if (!initialGive)
            GetPlayer(playerWithHat).SetHat(false);

        playerWithHat = playerId;
        GetPlayer(playerId).SetHat(true);
        hatPickupTime = Time.time;
    }

    [PunRPC]
    public void GiveAwayPoison(int victimId)
    {
        GetPlayer(playerWithHat).SetHat(false);
        playerWithHat = victimId;
        GetPlayer(victimId).SetHat(true);
        hatPickupTime = Time.time;
    }

    public bool CanGetHat()
    {
        if (Time.time > hatPickupTime + invincibleDuration)
            return true;
        else
            return false;
    }

    [PunRPC]
    void WinGame(int playerId)
    {

        Debug.Log("Game Over");
        gameEnded = true;
        PlayerController player = GetPlayer(playerId);
        GameUI.instance.SetWinText(player.photonPlayer.NickName);

        Invoke("GoBackToMenu", 3.0f);
    }

    void GoBackToMenu()
    {
        PhotonNetwork.LeaveRoom();
        NetworkManager.instance.ChangeScene("Main Menu");
    }

    [PunRPC]
    void KillPlayer(int playerId)
    {
        PlayerController player = GetPlayer(playerId);
        int successorId = Random.Range(1, 4);
        while(GetPlayer(successorId).player.activeSelf == false)
            successorId = Random.Range(1, 4);
        GiveHat(successorId, false);
        ++playersDead;
        player.player.SetActive(false);
    }
}
