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
        [Tooltip("Reward Prefab for the Spawner")]
        public GameObject rewardPrefab;

        [Header("MultiScene Setup")]
        public int instances = 3;
        public int roomCapacity = 4; // �� �� �ִ� �÷��̾� �� ����

        [Scene]
        public string gameScene;                                               //���Ӿ�         
        bool subscenesLoaded;
        readonly List<Scene> subScenes = new List<Scene>();
        Dictionary<Scene, int> scenePlayerCount = new Dictionary<Scene, int>();

        [SerializeField]Dictionary<Scene, List<NetworkConnectionToClient?>> scenePlayers = new Dictionary<Scene, List<NetworkConnectionToClient?>>();
        Vector3[] spawnPositions = new Vector3[]
{
            new Vector3(0, 0, 0),
            new Vector3(10, 0, 0),
            new Vector3(20, 0, 0),
            new Vector3(30, 0, 0),
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
                scenePlayers[newScene] = new List<NetworkConnectionToClient?>();

                // �����ʸ� ���� ������ �ʱ� ���� �۾��� �����Ѵ�.
                Spawner.InitialSpawn(newScene);
            }

            subscenesLoaded = true;
        }


// B Ŭ���̾�Ʈ�� Join ��ư Ŭ�� �� �ߵ�       
        public void StartClientFromLobby()
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesGame");
// B-1 Ŭ���̾�Ʈ�� ������ �����ϴ� StartClient �Լ��̸� 
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
            Scene targetScene = GetTargetSceneForPlayer();// �÷��̾� ���� �� ã��
            Vector3 spawnPosition = GetSpawnPosition(targetScene, out int playerIndex);


            // # �÷��̾� ��ü �̵�:
            //base.OnServerAddPlayer(conn);�� ȣ���Ͽ� �⺻ �÷��̾� ���� ������ �����մϴ�.
            //GetTargetSceneForPlayer()�� ȣ���Ͽ� �÷��̾ ��ġ�� ��� ���� ã���ϴ�.
            //SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);�� ����Ͽ� �÷��̾� ��ü�� ��� ������ �̵���ŵ�ϴ�.
            //�� �� �÷��̾� ��ü�� NetworkIdentity ������Ʈ�� Ȱ��ȭ�Ǿ� �ִ��� Ȯ���մϴ�.
            base.OnServerAddPlayer(conn);
            // �÷��̾� ��ü�� �����ͼ� ��ġ ����
            GameObject player = conn.identity.gameObject;
            player.transform.position = spawnPosition;

            // �÷��̾� ��ü�� Ÿ�� ������ �̵�
            SceneManager.MoveGameObjectToScene(player, targetScene);
            /*SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);*/
            // �÷��̾� ��Ͽ� �߰�
            scenePlayers[targetScene].Add(conn);


            // # PlayArea Ȱ��ȭ
            //��� ���� ��Ʈ ���� ������Ʈ�� ��ȸ�ϸ鼭 "PlayArea" ���� ������Ʈ�� ã���ϴ�.
            //ã�� "PlayArea" ���� ������Ʈ�� NetworkIdentity ������Ʈ�� ������ Ȱ��ȭ�մϴ�.

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
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
                    if (i >= players.Count || players[i] == null)
                    {
                        playerIndex = i;
                        return spawnPositions[i]; // �ε����� ���� �̸� ���ǵ� ��ġ ��ȯ
                    }
                }
            }
            playerIndex = -1;
            return Vector3.zero;
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
