using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Mirror;
public class RoomListManager : NetworkBehaviour
{
    public string lobbyScene;

    private List<Room> rooms;

    private void Start()
    {
        // �÷��̾� ID ��������
        string playerId = PlayerPrefs.GetString("PlayerID", "Unknown");

        // �÷��̾� ID�� ����Ͽ� �ʿ��� �ʱ�ȭ �۾� ����
        Debug.Log("Player ID: " + playerId);

        // �� ����Ʈ �ҷ����� ����
        LoadRoomList();
    }

    private void LoadRoomList()
    {
        // �� ����Ʈ �ҷ����� ���� ����
        rooms = new List<Room>(); // ���÷� �� ����Ʈ ����
    }

    public void OnCreateRoomButtonClicked()
    {
        // �� ���� ����
        CreateRoom();
    }

    public void OnJoinRoomButtonClicked(int roomId)
    {
        // �� ���� ����
        JoinRoom(roomId);
    }

    private void CreateRoom()
    {
        // �� ���� ���� ����
        SceneManager.LoadScene(lobbyScene);
    }

    private void JoinRoom(int roomId)
    {
        // �� ���� ���� ����
        SceneManager.LoadScene(lobbyScene);
    }
}

[System.Serializable]
public class Room
{
    public int id;
    public string name;
    public int playerCount;
    public bool isInGame;
}
