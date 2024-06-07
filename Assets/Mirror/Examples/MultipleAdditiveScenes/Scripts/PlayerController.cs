using Mirror.Examples.Chat;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(NetworkTransformReliable))]
    public class PlayerController : NetworkBehaviour
    {
        public GameObject newPrefab;
        public enum GroundState : byte { Jumping, Falling, Grounded }

        public override void OnStartAuthority()
        {
            this.enabled = true;
        }

        public override void OnStopAuthority()
        {
            this.enabled = false;
            
        }

        void Update()
        {
            if(isLocalPlayer)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    CmdReplaceChild(sceneName);
                }
            }
        }

       

        [Command]
        void CmdReplaceChild(string sceneName)
        {
            // ���� �ڽ� ������Ʈ�� ����
            foreach (Transform child in transform)
            {
                NetworkServer.Destroy(child.gameObject);
            }
            // ���ο� �ڽ� ������Ʈ ����
            GameObject newChild = Instantiate(newPrefab, transform.position, transform.rotation);
            Debug.Log(connectionToClient);

            Scene targetScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.MoveGameObjectToScene(newChild, targetScene);
            NetworkServer.Spawn(newChild, connectionToClient);
            newChild.GetComponent<ChildObject>().RpcSetParent(connectionToClient.identity);


            //newChild.transform.SetParent(connectionToClient.identity.transform); // �θ� ����
            //RpcReplaceChild(connectionToClient.identity, newChild.GetComponent<NetworkIdentity>());

            // Ŭ���̾�Ʈ���� �ڽ� ������Ʈ�� ����ȭ
        }
        /*[ClientRpc]
        void RpcReplaceChild(NetworkIdentity parent, NetworkIdentity newChildIdentity)
        {
            if (isServer || !isClientInitialized) return; // ���� �Ǵ� �ʱ�ȭ���� ���� Ŭ���̾�Ʈ�� �������� ����


            // �������� ������ �������� ã�Ƽ� �θ� ����
            GameObject newChild = newChildIdentity.gameObject;
            if (newChild != null)
            {
                newChild.transform.SetParent(parent.transform);
            }
        }*/
        bool isClientInitialized = false; // Ŭ���̾�Ʈ �ʱ�ȭ ���� ���� �߰�

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                isClientInitialized = true; // Ŭ���̾�Ʈ �ʱ�ȭ �Ϸ�
            }
        }

        [TargetRpc]
        public void TargetUpdatePlayerPosition(NetworkConnection target, Vector3 position)
        {
            // Ŭ���̾�Ʈ���� ������ �� ����
            transform.position = position;
            Debug.Log($"Client: Position updated to {position}");
        }
        
      
    }
}
