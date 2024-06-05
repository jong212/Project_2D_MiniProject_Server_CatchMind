using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    // A �����¸��Ҷ� ����� 
    // B  StartClientFromLobby �Լ��� �κ������ Join ��ư�� ������ �� ����� 
    // B-1 StartClient �Լ��� Ŭ���̾�Ʈ�� ������ �����ϴ� �Լ��ε�, ���� ������ 
    // B-2 OnServerAddPlayer() �ݹ��Լ��� �ڵ����� ȣ��˴ϴ�. �� �Լ��� ���ο� �÷��̾ ������ �߰��� �� ����Ǵ� �ݹ� �Լ��Դϴ�.
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
        }

        [Header("Spawner Setup")]
        [Tooltip("Reward Prefab for the Spawner")]
        public GameObject rewardPrefab;

        [Header("MultiScene Setup")]
        public int instances = 3;
        public int roomCapacity = 4; // �� �� �ִ� �÷��̾� �� ����

        [Scene]
        public string gameScene;

        // This is set true after server loads all subscene instances
        bool subscenesLoaded;

        // subscenes are added to this list as they're loaded
        readonly List<Scene> subScenes = new List<Scene>();

        // Dictionary to track player count per scene
        Dictionary<Scene, int> scenePlayerCount = new Dictionary<Scene, int>();

     

        #region Server System Callbacks
        //B-2
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            while (!subscenesLoaded)
                yield return null;
            /*
             # �� �ε� ��� ����

                 �� �ڵ忡���� gameScene ������ ����� �� �̸��� ����Ͽ� ���ο� ���� �ε��մϴ�.
                 gameScene�� �� �ڵ忡�� �̸� ���ǵ� ������, � ���� �ε����� �����Ǿ� �ֽ��ϴ�.
                 ���� ���� ����� Ŭ���̾�Ʈ���� ���۵Ǵ� SceneMessage�� sceneName �ʵ忡�� gameScene�� �����Ǿ� �ֽ��ϴ�.
                 �� �ε� ���:

                 SceneOperation.LoadAdditive�� ����Ͽ� ���� ���� �� ���� �߰��� �ε��մϴ�.
                 �� ����� ���� ���� ���ο� ���� �����ϴ� ����Դϴ�.
                 ��, ���� ���� gameScene�� �߰��� �ε�Ǵ� ���Դϴ�.
                 �� �ε� ����:

                 �� �ڵ�� ���ο� �÷��̾ ������ ����� ��OnServerAddPlayerDelayed �ڷ�ƾ�� ���� ����˴ϴ�.
                 ���� ���� ����� Ŭ���̾�Ʈ���� gameScene�� �߰��� �ε�˴ϴ�.
             */
            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            yield return new WaitForEndOfFrame();
            /*
            # �÷��̾� ��ü �̵�:
                base.OnServerAddPlayer(conn);�� ȣ���Ͽ� �⺻ �÷��̾� ���� ������ �����մϴ�.
                GetTargetSceneForPlayer()�� ȣ���Ͽ� �÷��̾ ��ġ�� ��� ���� ã���ϴ�.
                SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);�� ����Ͽ� �÷��̾� ��ü�� ��� ������ �̵���ŵ�ϴ�.
                �� �� �÷��̾� ��ü�� NetworkIdentity ������Ʈ�� Ȱ��ȭ�Ǿ� �ִ��� Ȯ���մϴ�.
            */
            base.OnServerAddPlayer(conn);            
            Scene targetScene = GetTargetSceneForPlayer();// �÷��̾� ���� �� ã��
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);
            Debug.Log(targetScene);

            /*
            # PlayArea Ȱ��ȭ
                ��� ���� ��Ʈ ���� ������Ʈ�� ��ȸ�ϸ鼭 "PlayArea" ���� ������Ʈ�� ã���ϴ�.
                ã�� "PlayArea" ���� ������Ʈ�� NetworkIdentity ������Ʈ�� ������ Ȱ��ȭ�մϴ�.
            */
            foreach (GameObject go in targetScene.GetRootGameObjects())
            {
                if (go.name == "PlayArea")
                {
                    NetworkIdentity networkIdentity = go.GetComponent<NetworkIdentity>();
                    if (networkIdentity != null)
                    {
                        networkIdentity.enabled = true;
                    }
                }
            }

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
        }
        // B-2 ����
        // �� �޼���� �÷��̾ ������ �� �ִ� ������ ���� ã�� ���� subScenes ����Ʈ�� ��ȸ�մϴ�. 
        // ���� �÷��̾� ���� ��� �ο����� ���� ���� �ִٸ� �� ���� ��ȯ�ϰ�, �׷��� ������ �÷��̾ ���� ���� ��ȯ�մϴ�. 
        // ��� ���� ���� �� �ְų� �÷��̾ ���� ���� ���� ��쿡�� ����Ʈ�� ������ ���� ��ȯ�մϴ�.
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

        #endregion

        #region Start & Stop Callbacks
        //A
        public override void OnStartServer()
        {
            StartCoroutine(ServerLoadSubScenes());
        }

        // A-1
        IEnumerator ServerLoadSubScenes()
        {
            for (int index = 1; index <= instances; index++)
            {
                yield return SceneManager.LoadSceneAsync(gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });

                Scene newScene = SceneManager.GetSceneAt(index);
                subScenes.Add(newScene);
                scenePlayerCount[newScene] = 0; // Initialize player count for each scene
                Spawner.InitialSpawn(newScene);
            }

            subscenesLoaded = true;
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

        
        public void StartClientFromLobby()//B 
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesGame");
            StartClient();//B-1
        }

        // Method to handle client connection
       
    } 

    [System.Serializable]
    public class RoomInfo
    {
        public string name;
        public int playerCount;
    }
}
