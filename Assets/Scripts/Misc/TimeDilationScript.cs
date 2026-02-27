using UnityEngine;
using UnityEngine.UI;

public class TimeDilationScript : MonoBehaviour
{
    public Slider Slider;
    public Image Fill, Icon;
    public Sprite SlowIcon, RecoverIcon, FastIcon;

    public float cycleSwap = 10f, cooldown = 2f, speedBuff = 60f, speedDebuff = 50f;

    public void SetUI_TimeFast()
    {
        Slider.value = 0f;
        Slider.maxValue = cycleSwap;
        Fill.color = Icon.color = Color.yellow;
        Icon.sprite = FastIcon;
    }

    public void SetUI_Recover()
    {
        Slider.maxValue = cooldown;
        Slider.value = Slider.maxValue;
        Fill.color = Icon.color = new(0.87f, 0.87f, 0.87f);
        Icon.sprite = RecoverIcon;
    }

    public void SetUI_TimeSlow()
    {
        Slider.value = 0f;
        Slider.maxValue = cycleSwap;
        Fill.color = Icon.color = Color.cyan;
        Icon.sprite = SlowIcon;
    }
}