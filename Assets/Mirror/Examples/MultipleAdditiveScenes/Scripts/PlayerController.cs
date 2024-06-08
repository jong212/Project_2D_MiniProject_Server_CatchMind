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
        public GameObject[] Prefabs;
        public enum GroundState : byte { Jumping, Falling, Grounded }

        public override void OnStartAuthority()
        {
            this.enabled = true;
            if (isLocalPlayer)
            {
                Debug.Log("OnStartAuthority called");

                string playerId = PlayerPrefs.GetString("PlayerID", "No ID Found");
                if (!string.IsNullOrEmpty(playerId))
                {
                    string characternum = DatabaseUI.Instance.SelectPlayercharacterNumber(playerId);
                    Debug.Log($"Character Number: {characternum}");
                    if (!string.IsNullOrEmpty(characternum))
                    {
                        string sceneName = SceneManager.GetActiveScene().name;
                        CmdReplaceChild(sceneName, characternum);
                    }
                }
            }
        }


        public override void OnStopAuthority()
        {
            this.enabled = false;
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    //CmdReplaceChild(sceneName);
                }
            }
        }



        [Command]
        void CmdReplaceChild(string sceneName, string characternum)
        {
            // ���� �κи� ����
            string numberPart = System.Text.RegularExpressions.Regex.Match(characternum, @"\d+").Value;
            Debug.Log("teset" + numberPart);
            if (int.TryParse(numberPart, out int intnum))
            {

                // ���� �ڽ� ������Ʈ�� ����
                foreach (Transform child in transform)
                {
                    NetworkServer.Destroy(child.gameObject);
                }

                // �ε��� ��ȿ�� �˻�
                if (intnum < 0 || intnum >= Prefabs.Length)
                {
                    Debug.LogError("Invalid character index.");
                    return;
                }

                // ���ο� �ڽ� ������Ʈ ���� �� ��Ʈ��ũ ����ȭ
                GameObject newChild = Instantiate(Prefabs[intnum], transform.position, transform.rotation);
                Debug.Log($"Instantiated newChild: {newChild.name}");
                // (����) ������ ���� Ÿ�� ������ ����
                Scene targetScene = SceneManager.GetSceneByName(sceneName);

                // (����) �������� ������ ������ ���Ӿ����� �̵�
                SceneManager.MoveGameObjectToScene(newChild, targetScene);

                // (����) �÷��̾� �ڽ����� ����
                newChild.transform.SetParent(transform);

                // (����) ��� Ŭ���̾�Ʈ�� ������ ����ȭ
                NetworkServer.Spawn(newChild, connectionToClient);
                Debug.Log($"Spawned newChild: {newChild.name}");

                ResetClientToZero(newChild);
            }
            else
            {
                Debug.LogError("Failed to parse character number.");
            }
        }
        [ClientRpc]
        void ResetClientToZero(GameObject obj)
        {
            if (obj != null)
            {
                obj.transform.localPosition = transform.position;
                Debug.Log($"ResetClientToZero called for: {obj.name}");
            }
            else
            {
                Debug.LogError("ResetClientToZero called with null object.");
            }
        }

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
