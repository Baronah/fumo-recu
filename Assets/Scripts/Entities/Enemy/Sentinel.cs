using System.Collections;
using UnityEngine;

public class Sentinel : EnemyBase
{
    [SerializeField] public float SpeedBuffOnAlert = 1.35f;
    [SerializeField] public float AtkBuffOnAlert = 1.2f;
    [SerializeField] private GameObject DetectCircle;

    private RectTransform DetectCircleRectTransform;

    public override void Start()
    {
        base.Start();
        DetectCircleRectTransform = DetectCircle.GetComponent<RectTransform>();
    }

    public override void InitializeComponents()
    {
        attackPattern = AttackPattern.NONE;

        base.InitializeComponents();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!SpottedPlayer)
        {
            DetectCircleRectTransform.sizeDelta = new Vector2(
                DetectionRange * 2.05f,
                DetectionRange * 2.05f
            );
        }
        DetectCircle.SetActive(IsAlive());
    }

    bool alarmed = false;
    public override void OnFirsttimePlayerSpot(bool viaAlert = false)
    {
        base.OnFirsttimePlayerSpot();

        if (alarmed) return;

        alarmed = true;
        animator.SetTrigger("skill");
        StartCoroutine(ExpandDetectCircle());

        if (viaAlert) return;
        if (sfxs[0]) sfxs[0].Play();

        EntityManager.Enemies.ForEach(enemy =>
        {
            if (enemy != this && enemy.IsAlive())
            {
                enemy.moveSpeed *= SpeedBuffOnAlert;
                enemy.atk = (short)(enemy.atk * AtkBuffOnAlert);
            }
        });
    }

    public override IEnumerator Attack()
    {
        yield break;
    }

    IEnumerator ExpandDetectCircle()
    {
        Vector3 currentScale = DetectCircle.transform.localScale, finalScale = currentScale * 10f;
        float expandTime = 0.5f, count = 0;
        while (expandTime > count)
        {
            count += Time.deltaTime;
            DetectCircle.transform.localScale = Vector3.Lerp(currentScale, finalScale, count / expandTime);
            yield return null;
        }

        DetectCircle.transform.localScale = finalScale * 5f; 
    }

    public override void WriteStats()
    {
        Description = "They are responsible for scouting, patrolling, and issuing early warnings to the entire squad. Once spotting intruder, the Herald will immediately issue a warning that spread to the entire army.";
        Skillset = 
            "• Unable to attack.\n" +
            "• Upon spotting the player, raises an alarm that increases the ATK and MSPD of all presenting enemies, " +
            "and makes them to also spot the player immediately.";
        TooltipsDescription = "Does not attack. Upon spotting the player, <color=red>alerts</color> all other enemies who haven't spotted them, increasing their ATK and movespeed.";

        base.WriteStats();
    }
}