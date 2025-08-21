public class FM_05 : StageManager
{
    public override void OnEnemySpawn(EnemyBase enemy)
    {
        base.OnEnemySpawn(enemy);
        if (enemy is Sudaram sr)
        {
            sr.originiumPollutionBonusASPD = 100f;
            sr.originiumPollutionDamageMultiplier = 0f;
        }
    }
}