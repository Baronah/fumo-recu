using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShroudedAssassin : EnemyBase
{
    [SerializeField] private float skillWindupTime = 1.25f;
    [SerializeField] private float skillPlayerDistanceOffset = 50f;
    [SerializeField] private float skillCooldown = 15f;
    private bool isUsingSkill = false, isDashing = false;

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
