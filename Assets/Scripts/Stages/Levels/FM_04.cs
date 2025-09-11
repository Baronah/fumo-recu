using UnityEngine;

public class FM_04 : StageManager
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
}