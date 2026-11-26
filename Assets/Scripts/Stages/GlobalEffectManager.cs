using System;
using Unity.VisualScripting;
using UnityEngine;

[Singleton]
public class GlobalEffectManager : MonoBehaviour
{
	public static GlobalEffectManager _instance;
    private void Awake()
    {
        if (!_instance)
        {
            _instance = this;
        }
        else Destroy(gameObject);
    }

    [SerializeField] EffectData[] EffectDatas;
    public GameObject GetEffect(string Name)
    {
        foreach (var data in EffectDatas)
        {
            if (data.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                return data.EffectPrefab;
        }

        return null;
    }

    public GameObject Acceleration;
    private void Start()
    {
        Acceleration = GetEffect("Acceleration");
    }
}

[Serializable]
public class EffectData
{
    public string Name;
    public GameObject EffectPrefab;
}