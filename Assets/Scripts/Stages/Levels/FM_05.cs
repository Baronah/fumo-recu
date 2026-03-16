using UnityEngine;

public class FM_05 : StageManager
{
    [SerializeField] private float CM_OriginiutantASPD_Bonus = 100f;

    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);

        if (CharacterPrefabsStorage.EnableChallengeMode)
        {
            if (enemy as Originiutant) enemy.ASPD += CM_OriginiutantASPD_Bonus;
            enemy.IsPhysicalImmune = enemy.IsMagicalImmune = true;
        }
    }

    public override void OnPlayerSpawn(PlayerBase player)
    {
        base.OnPlayerSpawn(player);
        player.b_moveSpeed *= 1.15f;
    }
}