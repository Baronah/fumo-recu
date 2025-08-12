using System.Collections;

public class DeviceBase : EntityBase
{
    public override void FixedUpdate()
    {
        UpdateCooldowns();
    }

    public override void UpdateCooldowns()
    {
        
    }

    public override IEnumerator Attack()
    {
        yield return null;
    }

    public override void Move()
    {

    }

    public override void OnFreezeMaintain()
    {
    }

    public override void OnFreezeExit()
    {
        
    }
}