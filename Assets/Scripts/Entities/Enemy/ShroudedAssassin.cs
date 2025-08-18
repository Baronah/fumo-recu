using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShroudedAssassin : EnemyBase
{
    [SerializeField] private float skillWindupTime = 1.25f;
    [SerializeField] private float skillPlayerDistanceOffset = 50f;
    [SerializeField] private float skillCooldown = 15f;
    private bool isUsingSkill = false, isDashing = false;

    IEnumerator UseSkill()
    {
        isUsingSkill = true;
        IsFreezeImmune = true;
        IsStunImmune = true;
        isInvulnerable = true;

        rb2d.velocity = Vector2.zero;
        animator.SetTrigger("skill");
        yield return new WaitForSeconds(skillWindupTime);

        isUsingSkill = false;
        IsFreezeImmune = false;
        IsStunImmune = false;
        isInvulnerable = false;
    }

    private void OnPlayerDashHit(PlayerBase player)
    {

    }    

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing) return;

        PlayerBase player = collision.gameObject.GetComponent<PlayerBase>();
        if (player) OnPlayerDashHit(player);
    }
}
