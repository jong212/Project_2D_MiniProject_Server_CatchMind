using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public class CountdownManager : NetworkBehaviour
{

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("????zzz");
    }



    private void Update()
    {
        // Ŭ���̾�Ʈ���� ī��Ʈ�ٿ� UI�� ������Ʈ
        // ��: countdownText.text = countdownTime.ToString();
    }
}
