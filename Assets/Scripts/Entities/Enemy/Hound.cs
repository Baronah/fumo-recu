public class Hound : EnemyBase
{
    public override void WriteStats()
    {
        Description = "A hunting hound trained and utilized by locals since ancient times. For early aboriginals and pioneers, these hounds were considered invaluable partners.";
        Skillset = "";
        TooltipsDescription = "A hunting hound trained and utilized by locals since ancient times. " +
            "<color=yellow>Fast movement</color>.";

        base.WriteStats();
    }
}