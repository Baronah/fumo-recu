using Unity.VisualScripting;
using UnityEngine;
using static EnemySpawnpointScript;

public class Key : MonoBehaviour
{
    [SerializeField] GameObject[] TargetGatesToOpen;
    [SerializeField] ActionType actionType = ActionType.DESTROY;

    [SerializeField] GameObject MessageBox;

    bool picked = false;
    void OnKeyPickUp()
    {
        if (picked) return;

        foreach (var gate in TargetGatesToOpen)
        {
            switch (actionType)
            {
                default:
                    break;

                case ActionType.DESTROY:
                    Destroy(gate);
                    break;

                case ActionType.DEACTIVATE:
                    gate.SetActive(false);
                    break;

                case ActionType.ACTIVATE:
                    gate.SetActive(true);
                    break;
            }
        }
        
        if (MessageBox)
        {
            Color color = GetComponent<SpriteRenderer>().color;
            string message = $"<color=#{color.ToHexString()}>Gate</color> ";
            string addendum = actionType switch
            {
                ActionType.DESTROY => "unlocked",
                ActionType.ACTIVATE => "activated",
                ActionType.DEACTIVATE => "disabled",
                _ => "did something?",
            };

            message += addendum;
            GameObject o = Instantiate(MessageBox, transform.position + new Vector3(0, 35), Quaternion.identity);
            PopupMessageBox messageObj = o.GetComponent<PopupMessageBox>();
            messageObj.SetMessage(message);
            messageObj.Display(1.5f);
        }

        picked = true;
        gameObject.SetActive(false);
        Destroy(gameObject, 1f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision || !collision.gameObject || !collision.CompareTag("Player")) return;
        OnKeyPickUp();
    }
}