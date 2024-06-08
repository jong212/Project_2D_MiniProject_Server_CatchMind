using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    //1. 
    //  
    // B  StartClientFromLobby �Լ��� �κ������ Join ��ư�� ������ �� ����� 
    // B-1 
    // B-2 
    [AddComponentMenu("")]
    public class MultiSceneNetManager : NetworkManager
    {
        public static MultiSceneNetManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            Debug.Log(scenePlayerCount);
        }

        [Header("Spawner Setup")]

        [Header("MultiScene Setup")]
        public int instances = 2;
        public int roomCapacity = 2; // �� �� �ִ� �÷��̾� �� ����

        [Scene]
        public string gameScene;                                               //���Ӿ�         
        bool subscenesLoaded;
        readonly List<Scene> subScenes = new List<Scene>();
        Dictionary<Scene, int> scenePlayerCount = new Dictionary<Scene, int>();

        [SerializeField]Dictionary<Scene, List<NetworkConnectionToClient?>> scenePlayers = new Dictionary<Scene, List<NetworkConnectionToClient?>>();
        Vector3[] spawnPositions = new Vector3[]
{
            new Vector3(-7.01001f, 3.6f, 0),
            new Vector3(7.3f, 3.6f, 0),
            new Vector3(-7.01001f, 1.08f, 0),
            new Vector3(7.3f, 0.99f, 0),
            new Vector3(-7.01001f,-1.29f, 0),
            new Vector3(7.3f,-1.12f, 0),
            new Vector3(-7.01001f,-3.47f, 0),
            new Vector3(7.3f,-3.57f, 0),
};
        // A <����> �����¸��Ҷ� �����
        public override void OnStartServer()
        {
            StartCoroutine(ServerLoadSubScenes());
        }

        // A-1 
        IEnumerator ServerLoadSubScenes()
        {
            for (int index = 1; index <= instances; index++)
            {
                //�������� �񵿱������� ���Ӿ��� �ε���ѳ���
                yield return SceneManager.LoadSceneAsync(gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
                //���� �������� �ε� �� ���� �ε��� 1���� ���� ��ȯ�Ѵ� ������ ���Ӿ��� ������������ 1���� ���Ӿ���
                Scene newScene = SceneManager.GetSceneAt(index);
                //���Ӿ� �ϴ� ������� �߰��ϰ�
                subScenes.Add(newScene);
                scenePlayerCount[newScene] = 0;
                // �߰� �� 1��° ���Ӿ��� �迭�� �߰��Ѵ�
                //scenePlayers[newScene] = new List<NetworkConnectionToClient?>();
                scenePlayers[newScene] = new List<NetworkConnectionToClient?>(new NetworkConnectionToClient?[roomCapacity]);


                // �����ʸ� ���� ������ �ʱ� ���� �۾��� �����Ѵ�.
                Spawner.InitialSpawn(newScene);
            }

            subscenesLoaded = true;
        }


// B Ŭ���̾�Ʈ�� Join ��ư Ŭ�� �� �ߵ�       
        public void StartClientFromLobby()
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesGame");
// B-1 Ŭ���̾�Ʈ�� ������ �����ϴ� StartClient �Լ��̰� OnServerAddPlayer �ݹ����� �����Ŵ 
            StartClient();
        }


        #region Server System Callbacks
// B-2 <�����ݹ�>   OnServerAddPlayer() �ݹ��Լ��� StartClient �� �����߱� ������ �ڵ����� ȣ��˴ϴ�. �� �Լ��� ���ο� �÷��̾ ������ �߰��� �� ����Ǵ� �ݹ� �Լ��Դϴ�.
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            while (!subscenesLoaded)
                yield return null;

// B-3 <���� �ڷ�ƾ> �������� ������ ������ �Ʒ� �ڵ�� ������ Ŭ���̾�Ʈ���� ���Ӿ��� �߰��� �ε��϶�� �ǹ� ��, Ŭ�� Join�� ������ ��¶�� ������ �˰� Ŭ�󿡰� �׾� �߰��϶�� ��Ű�°���

            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            yield return new WaitForEndOfFrame();
            Scene targetScene = GetTargetSceneForPlayer();// �÷��̾� ��� ������ ������ �ϴ��� ��ȯ�ϼ��� (����)
            Vector3 spawnPosition = GetSpawnPosition(targetScene, out int playerIndex); // ��ȯ�ϴ� �濡�� �� ���� �ڸ��� ����ִ��� Ȯ���ϰ� �� ��ǥ���� ��ȯ�ϼ��� (����)            
       
            // �ϴ� �÷��̾� ���� ���� �߰��ҰԿ�? 
            // ���Ӿ��� �ƴ� ��Ȯ���� ���Ӿ� �̵� �� (����) 
            // base.OnServerAddPlayer(conn)�� ȣ���ϸ� ȣ���ϸ� ������ Ŭ���̾�Ʈ�� ���̾��Ű�� �÷��̾� ������Ʈ�� �ڵ����� �����˴ϴ�. TargetRpc�� ���� �߰����� ���� ���̵� ������ Ŭ���̾�Ʈ�� ���°� ����ȭ�˴ϴ�. (�������� �����ϰ� Ŭ��� ���� ����)
            base.OnServerAddPlayer(conn); 

            // ���̾��Ű�� ������ �÷��̾� ������ player�� ���� !(���� �ڷ�ƾ Ÿ�°� �� �������� �����ϰ� �ִ»���)
            GameObject player = conn.identity.gameObject;
            // �� ���� �÷��̾� ��ü�� ���Ӿ��������� �̵��ҰԿ�? (����)
            SceneManager.MoveGameObjectToScene(player, targetScene);

            // Ŭ���̾�Ʈ �÷��̾��� PlayerController�� �����ͼ� TargetRpc ȣ��
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TargetUpdatePlayerPosition(conn, spawnPosition,playerIndex);
            }
           
                /*player.GetComponent<RectTransform>().anchoredPosition = new Vector2(1, 1);*/
            // �÷��̾� ��Ͽ� �߰�
            scenePlayers[targetScene][playerIndex] = conn;

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
            LogScenePlayers();
        }
// B-4
//�� �޼���� �÷��̾ ������ �� �ִ� ������ ���� ã�� ���� subScenes ����Ʈ�� ��ȸ�մϴ�. 
//���� �÷��̾� ���� ��� �ο����� ���� ���� �ִٸ� �� ���� ��ȯ�ϰ�, �׷��� ������ �÷��̾ ���� ���� ��ȯ�մϴ�. 
//��� ���� ���� �� �ְų� �÷��̾ ���� ���� ���� ��쿡�� ����Ʈ�� ������ ���� ��ȯ�մϴ�.
        Scene GetTargetSceneForPlayer()
        {
            foreach (var scene in subScenes)
            {
                if (scenePlayerCount.TryGetValue(scene, out int count))
                {
                    if (count < roomCapacity)
                    {
                        return scene;
                    }
                }
                else
                {
                    return scene; // If the scene has no players yet
                }
            }

            return subScenes[subScenes.Count - 1];
        }
        Vector3 GetSpawnPosition(Scene scene, out int playerIndex)
        {
            if (scenePlayers.TryGetValue(scene, out List<NetworkConnectionToClient?> players))
            {
                for (int i = 0; i < roomCapacity; i++)
                {
                    if (players[i] == null)
                    {
                        playerIndex = i;
                        Debug.Log($"Spawn position for player index {i} is {spawnPositions[i]}");

                        return spawnPositions[i]; // �ε����� ���� �̸� ���ǵ� ��ġ ��ȯ
                    }
                }
            }
            playerIndex = -1;
            return Vector3.zero;
        } 
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            foreach (var scene in subScenes)
            {
                if (scenePlayers[scene].Contains(conn))
                {
                    int playerIndex = scenePlayers[scene].IndexOf(conn);
                    scenePlayers[scene][playerIndex] = null;
                    UpdatePlayerPositions(scene);
                    scenePlayerCount[scene]--;
                    LogScenePlayers();
                    break;
                }
            }
        }

        void UpdatePlayerPositions(Scene scene)
        {
            if (scenePlayers.TryGetValue(scene, out List<NetworkConnectionToClient?> players))
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] != null)
                    {
                        var player = players[i].identity.gameObject;
                        player.transform.position = spawnPositions[i];
                    }
                }
            }
        }
        #endregion

        /*�α�üũ��*/
        #region Start & Stop Callbacks
        void LogScenePlayers()
        {
            foreach (var kvp in scenePlayers)
            {
                string sceneName = kvp.Key.name;
                string players = string.Join(", ", kvp.Value.Select(c => c != null ? c.connectionId.ToString() : "null"));
                Debug.Log($"Scene: {sceneName}, Players: {players}");
            }
        }

        void Update()
        {
            // �� �����Ӹ��� �ֿܼ� �α� ���
            LogScenePlayers();
        }
        public override void OnStopServer()
        {
            NetworkServer.SendToAll(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.UnloadAdditive });
            StartCoroutine(ServerUnloadSubScenes());
        }

        IEnumerator ServerUnloadSubScenes()
        {
            for (int index = 0; index < subScenes.Count; index++)
                if (subScenes[index].IsValid())
                    yield return SceneManager.UnloadSceneAsync(subScenes[index]);

            subScenes.Clear();
            scenePlayers.Clear();
            scenePlayerCount.Clear();
            subscenesLoaded = false;

            yield return Resources.UnloadUnusedAssets();
        }

        public override void OnStopClient()
        {
            if (mode == NetworkManagerMode.Offline)
                StartCoroutine(ClientUnloadSubScenes());
        }

        IEnumerator ClientUnloadSubScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
        }

        #endregion


        // Method to handle client connection

    }

    [System.Serializable]
    public class RoomInfo
    {
        public string name;
        public int playerCount;
    }
}
