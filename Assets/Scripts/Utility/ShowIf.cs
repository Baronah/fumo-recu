public class ShowIf : UnityEngine.PropertyAttribute
{
    public string condition;
    public object inverse;

    public ShowIf(string condition, object inverse)
    {
        this.condition = condition;
        this.inverse = inverse;
    }
}