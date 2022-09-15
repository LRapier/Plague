using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [HideInInspector]
    public int id;
    public bool isDead = false;
    public bool isPoisoned = false;

    [Header("Info")]
    public float moveSpeed;
    public float jumpForce;
    public GameObject hatObject;
    
    [Header("Materials")]
    public Material player1;
    public Material player2;
    public Material player3;
    public Material player4;

    [HideInInspector]
    public float curHatTime;

    [Header("Components")]
    public Rigidbody rig;
    public GameObject player;
    public Player photonPlayer;

    [PunRPC]
    public void Initialize(Player player)
    {
        photonPlayer = player;
        id = player.ActorNumber;
        GameManager.instance.players[id - 1] = this;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        switch (id)
        {
            case 1:
                GameManager.instance.GiveHat(id, true);
                renderer.material = player1;
                break;
            case 2:
                renderer.material = player2;
                break;
            case 3:
                renderer.material = player3;
                break;
            case 4:
                renderer.material = player4;
                break;
        }

        if (!photonView.IsMine)
            rig.isKinematic = true;

        curHatTime = GameManager.instance.timeToDie;
    }

    void Update()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            if((curHatTime <= 0) && !GameManager.instance.gameEnded)
            {
                isDead = true;
                GameManager.instance.photonView.RPC("KillPlayer", RpcTarget.All, id);
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (GameManager.instance.playersDead == GameManager.instance.playersNum - 1)
            {
                GameManager.instance.photonView.RPC("WinGame", RpcTarget.All, id);
            }
        }

        if (photonView.IsMine)
        {
            Move();
            if (Input.GetKeyDown(KeyCode.Space))
                TryJump();
            if (hatObject.activeInHierarchy)
                curHatTime -= Time.deltaTime;
        }
    }

    void Move()
    {
        if(hatObject.activeSelf == true)
        {
            float x = Input.GetAxis("Horizontal") * (moveSpeed + 1);
            float z = Input.GetAxis("Vertical") * (moveSpeed + 1);
            rig.velocity = new Vector3(x, rig.velocity.y, z);
        }
        else
        {
            float x = Input.GetAxis("Horizontal") * moveSpeed;
            float z = Input.GetAxis("Vertical") * moveSpeed;
            rig.velocity = new Vector3(x, rig.velocity.y, z);
        }
    }

    void TryJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, 0.7f))
            rig.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    public void SetHat (bool hasHat)
    {
        hatObject.SetActive(hasHat);
        isPoisoned = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!photonView.IsMine)
            return;
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("player touch");
            if ((GameManager.instance.GetPlayer(collision.gameObject).id != GameManager.instance.playerWithHat) && isPoisoned == true)
            {
                Debug.Log("you are poison and they are not");
                if (GameManager.instance.CanGetHat())
                {
                    Debug.Log("Give away poison");
                    GameManager.instance.photonView.RPC("GiveAwayPoison", RpcTarget.All, GameManager.instance.GetPlayer(collision.gameObject).id);
                }
            }
        }
    }

    void OnTriggerEnter(Collider death)
    {
        if (death.gameObject.CompareTag("Death"))
            Respawn();
    }

    public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
            stream.SendNext(curHatTime);
        else if(stream.IsReading)
            curHatTime = (float)stream.ReceiveNext();
    }

    public void Respawn()
    {
        player.transform.position = new Vector3(0, 8, 0);
    }
}
