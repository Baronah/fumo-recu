using System.Collections;
using TMPro;
using UnityEngine;

public class Turret : DeviceBase
{
    [SerializeField] private Transform ProjectilePosition;
    [SerializeField] private float FiringInterval = 7.0f;

    IEnumerator Fire()
    {
        while (true)
        {
            /*
            CreateProjectileAndShootToward(
                ProjectilePrefab,
                new DamageInstance(atk, 0, 0),
                transform.position,
                ProjectilePosition.position,
                projectileType: ProjectileScript.ProjectileType.CATCH_FIRST_TARGET_OF_TYPE,
                travelSpeed: ProjectileSpeed,
                acceleration: 0,
                lifeSpan: 8f,
                targetType: typeof(EntityBase));
            */
            yield return null;
        }
    }
}