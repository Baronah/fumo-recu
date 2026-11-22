using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static EnemySpawnpointScript;

public class KillGate : MonoBehaviour
{
    [SerializeField] EnemySpawnpointScript[] KillgateEnemies;
    [SerializeField] GameObject MessagePrefab;
    [SerializeField] string CustomMessage = string.Empty;

    private void Start()
    {
        cnt = (short)Random.Range(0, 20);
    }

    bool unlocked = false;
    short cnt = 0;
    private void Update()
    {
        cnt++;
        if (cnt < 20) return;
        CheckForUnlock();
        cnt = 0;
    }

    void CheckForUnlock()
    {
        if (unlocked) return;

        bool allDead = KillgateEnemies.All(kg => kg.spawnedEnemiesAreDead);
        if (allDead) Unlock();
    }

    void Unlock()
    {
        unlocked = true;

        if (MessagePrefab)
        {
            Vector3 pos = PlayerManager._instance.activePlayer.transform.position + new Vector3(0, 50);
            GameObject o = Instantiate(MessagePrefab, pos, Quaternion.identity);
            PopupMessageBox messageObj = o.GetComponent<PopupMessageBox>();
            messageObj.SetMessage(
                string.IsNullOrEmpty(CustomMessage) 
                ? "Gate unlocked!" 
                : CustomMessage);
            messageObj.Display(2f);
        }

        gameObject.SetActive(false);
        Destroy(gameObject, 1f);
    }
}