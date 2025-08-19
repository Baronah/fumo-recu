public class Puppet : EnemyBase
{
    public override void Move()
    {
        
    }

    public override void EnemyFixedBehaviors()
    {
        
    }

    public override void OnFirsttimePlayerSpot(bool viaAlert = false)
    {
        
    }

    public override void WriteStats()
    {
        Description = "A harmless dummy. Very durable, great for particing purpose.";
        Skillset = ".";
        TooltipsDescription = "A harmless dummy. Very durable, great for particing purpose.";

        base.WriteStats();
    }
}