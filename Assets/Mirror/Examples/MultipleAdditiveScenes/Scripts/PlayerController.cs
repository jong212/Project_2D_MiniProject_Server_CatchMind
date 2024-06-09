using Mirror.Examples.Chat;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(NetworkTransformReliable))]
    public class PlayerController : NetworkBehaviour
    {

        public GameObject newPrefab;
        public GameObject[] Prefabs;
        public RectTransform canvasRectTransform;
        public RectTransform RawRectTransform;
        public TextMeshProUGUI textMeshProUGUI;
        public InputField IField;
        public enum GroundState : byte { Jumping, Falling, Grounded }
        [SyncVar(hook = nameof(OnTextChanged))]
        private string syncedText = "";

        [SyncVar(hook = nameof(OnAlignmentChanged))]
        private TextAlignmentOptions syncedAlignment = TextAlignmentOptions.Left;

        public override void OnStartAuthority()
        {
            IField = GameObject.FindGameObjectWithTag("field").GetComponent<InputField>();
            Debug.Log(IField);
            this.enabled = true;
            if (isLocalPlayer)
            {
                Debug.Log("OnStartAuthority: Initializing components for local player.");

                InitializeComponents();
                string playerId = PlayerPrefs.GetString("PlayerID", "No ID Found");
                if (!string.IsNullOrEmpty(playerId))
                {
                    string characternum = DatabaseUI.Instance.SelectPlayercharacterNumber(playerId);

                    if (!string.IsNullOrEmpty(characternum))
                    {
                        string sceneName = SceneManager.GetActiveScene().name;
                        CmdReplaceChild(sceneName, characternum);
                    }
                }
            }
        }

        void InitializeComponents()
        {
   Canvas canvas = GetComponentInChildren<Canvas>();
    if (canvas != null)
    {
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        
        // �ڽ� ������Ʈ �߿��� RectTransform�� ���� ������Ʈ�� ã��
        foreach (RectTransform child in canvas.GetComponentsInChildren<RectTransform>())
        {
            if (child != canvasRectTransform)
            {
                RawRectTransform = child;
                break;
            }
        }

        Debug.Log(RawRectTransform); // ã�� RectTransform�� �α׷� ���

        textMeshProUGUI = canvas.GetComponentInChildren<TextMeshProUGUI>();
        if (textMeshProUGUI == null)
        {
            Debug.LogError("TextMeshProUGUI component not found during initialization.");
        }
    }
    else
    {
        Debug.LogError("Canvas component not found during initialization.");
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
                if (canvasRectTransform == null || textMeshProUGUI == null)
                {
                    InitializeComponents();
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
/*
1. Ŭ���̾�Ʈ�� �ؽ�Ʈ �Է�:
    * Ŭ���̾�Ʈ���� ���� Ű�� ������ Update �޼��忡�� CmdChangeText�� ȣ��˴ϴ�.
    CmdChangeText�� �������� ����Ǹ�, syncedText ������ ������Ʈ�մϴ�.

2. ������ ���� ���� ����:
    * �������� syncedText ������ ������Ʈ�Ǹ�, ���� ������ ��� Ŭ���̾�Ʈ�� ���۵˴ϴ�.

3. Ŭ���̾�Ʈ�� ���� ���� ����:
    * Ŭ���̾�Ʈ�� ����� syncedText ���� �����ϰ�, OnTextChanged �޼��带 ȣ���մϴ�.
    * OnTextChanged �޼���� ���ο� �ؽ�Ʈ ���� textMeshProUGUI�� �����Ͽ� UI�� ������Ʈ�մϴ�.

���
SyncVar ������ �������� ����Ǹ� �ڵ����� Ŭ���̾�Ʈ�� ���۵˴ϴ�.
Ŭ���̾�Ʈ���� SyncVar ������ ���� ������ �����ϸ� hook �޼��尡 ȣ��˴ϴ�.
hook �޼���� Ŭ���̾�Ʈ ������ ����Ǹ�, UI ������Ʈ ���� �۾��� �����մϴ�.                     
*/
                    string inputText = IField.text;
                    CmdChangeText(inputText);
                }
            }
        }

        void OnTextChanged(string oldText, string newText)
        {
            Debug.Log("OnTextChanged called with text: " + newText);
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = newText;
            }
        }
        void OnAlignmentChanged(TextAlignmentOptions oldAlignment, TextAlignmentOptions newAlignment)
        {
            Debug.Log("OnAlignmentChanged called with alignment: " + newAlignment);
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.alignment = newAlignment;
            }
        }

        [Command]
        void CmdChangeText(string inputText)
        {
            syncedText = inputText;
          /*  RpcChangeText(inputText);*/
        }

     /*   [ClientRpc]
        void RpcChangeText(string inputText)
        {
            Debug.Log("RpcChangeText called on the clients");
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = inputText;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in RpcChangeText.");
            }
        }*/

        [Command]
        void CmdReplaceChild(string sceneName, string characternum)
        {
            // ���� �κи� ����
            string numberPart = System.Text.RegularExpressions.Regex.Match(characternum, @"\d+").Value;
            Debug.Log("teset" + numberPart);
            if (int.TryParse(numberPart, out int intnum))
            {

                // �ε��� ��ȿ�� �˻�
                if (intnum < 0 || intnum >= Prefabs.Length)
                {
                    Debug.LogError("Invalid character index.");
                    return;
                }

                // ���ο� �ڽ� ������Ʈ ���� �� ��Ʈ��ũ ����ȭ
                GameObject newChild = Instantiate(Prefabs[intnum], transform.position, Prefabs[intnum].transform.rotation);

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
                Debug.LogError("???????????");
            }
        }
        [ClientRpc]
        void ResetClientToZero(GameObject obj)
        {
            if (obj != null)
            {
                Debug.Log($"Rotation Before ResetClientToZero: {obj.transform.rotation}");
                obj.transform.localPosition = transform.position;
                Debug.Log($"Rotation After ResetClientToZero: {obj.transform.rotation}");
            }
            else
            {
                Debug.LogError("???????????????????");
            }
        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                InitializeComponents();
            }
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = syncedText;
                textMeshProUGUI.alignment = syncedAlignment;
            }

        }

        [TargetRpc]
        public void TargetUpdatePlayerPosition(NetworkConnection target, Vector3 position, int playerIndex)
        {
            // Ŭ���̾�Ʈ���� ������ �� ����
            transform.position = position;
            if (canvasRectTransform == null || textMeshProUGUI == null)
            {
                InitializeComponents(); // �ʿ��� ��� ������Ʈ ���ʱ�ȭ
            }
            if (canvasRectTransform != null)
            {
                if (playerIndex % 2 == 0) // ¦�� �ε���
                {
                    canvasRectTransform.anchoredPosition = new Vector2(3.23f, 1.98f);
         
                    CmdChangeAlignment(TextAlignmentOptions.Left); // �������� ���� ���� ������Ʈ

                }
                else // Ȧ�� �ε���
                {
                    canvasRectTransform.anchoredPosition = new Vector2(-3.55f, 2.03f);
                    if (textMeshProUGUI != null)
                    {
                        Vector3 currentRotation = RawRectTransform.transform.rotation.eulerAngles;
                        currentRotation.y = 180f;
                        RawRectTransform.transform.rotation = Quaternion.Euler(currentRotation);
                        CmdChangeAlignment(TextAlignmentOptions.Right); // �������� ���� ���� ������Ʈ

                    }
                    else
                    {
                        Debug.LogError("TextMeshProUGUI component not found.");
                    }
                }
            }
            else
            {
                Debug.LogError("Canvas RectTransform not found.");
            }
        }
        [Command]
        void CmdChangeAlignment(TextAlignmentOptions alignment)
        {
            syncedAlignment = alignment;
        }


    }
}
