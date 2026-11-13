using System.Collections.Generic;
using UnityEngine;
using static EnemyBase;
using static LevelDifficultyModifier;

public static class EnemyStatsLookup
{
    public static bool HasStatsChange(EnemyCode code, int levelIndex)
    {
        HashSet<EnemyCode> codeWithStatsChange = null;
        switch (levelIndex)
        {
            case 0:
                break;

            case 1:
                break;

            case 2:
                break;

            case 3:
                codeWithStatsChange = new() { EnemyCode.BLOODBOIL_KNIGHT };
                break;

            case 4:
                break;

            case 5:
                break;

            case 6:
                codeWithStatsChange = new() { EnemyCode.ORIGINIUM_SPIDER_ALPHA };
                break;

            case 7:
                codeWithStatsChange = new() { EnemyCode.SENTINEL, EnemyCode.SUDARAM, EnemyCode.ORIGINIUM_SPIDER, EnemyCode.ORIGINIUM_SPIDER_ALPHA };
                break;

            case 8:
                codeWithStatsChange = new() { EnemyCode.BLOODBOIL_KNIGHT, EnemyCode.SUDARAM, EnemyCode.ORIGINIUM_SPIDER_ALPHA };
                break;

            case 9:
                codeWithStatsChange = new() { EnemyCode.ARCHER, EnemyCode.HOUND, EnemyCode.WETWORK, EnemyCode.HEIR, EnemyCode.MATTERLLURGIST };
                break;

            case 10:
                codeWithStatsChange = new() { EnemyCode.SUDARAM };
                break;

            case 11:
                break;

            case 12:
                codeWithStatsChange = new() { EnemyCode.SUDARAM };
                break;

            case 13:
                codeWithStatsChange = new() { EnemyCode.HIBERNATOR_KNIGHT, EnemyCode.SENTINEL };
                break;

            case 14:
                codeWithStatsChange = new() { EnemyCode.HIBERNATOR_KNIGHT };
                break;

            case 15:
                codeWithStatsChange = new() { EnemyCode.SENTINEL };
                break;

            case 16:
                codeWithStatsChange = new() { EnemyCode.HIBERNATOR_KNIGHT, EnemyCode.SAINT_STATUE, EnemyCode.GLOOMPINCER };
                break;
        }

        if (codeWithStatsChange == null) return false;
        return codeWithStatsChange.Contains(code);
    }

    public static void GetStats(EnemyBase enemy, int levelIndex, out bool hasChanged)
    {
        hasChanged = false;
        switch (levelIndex)
        {
            case 0:
                break;

            case 1:
                break;

            case 2:
                break;

            case 3:
                if (enemy as BloodboilKnight)
                {
                    enemy.mHealth = 350;
                    enemy.bRes = 10;
                    hasChanged = true;
                }
                break;

            case 4:
                break;

            case 5:
                break;

            case 6:
                if (enemy is OriginiumSpiderAlpha alp)
                {
                    alp.mHealth = 60;
                    hasChanged = true;
                }
                break;

            case 7:
                if (enemy is Sudaram sr)
                {
                    sr.originiumPollutionBonusASPD = 100f;
                    sr.originiumPollutionDamageMultiplier = 0f;
                    hasChanged = true;
                }
                else if (enemy as OriginiumSpider || enemy as OriginiumSpiderAlpha)
                {
                    enemy.bAtk = (short)(enemy.bAtk * 0.85f);
                    hasChanged = true;
                }
                else if (enemy as Sentinel)
                {
                    enemy.bDef += 20;
                    enemy.bRes += 30;
                    hasChanged = true;
                }
                break;

            case 8:
                if (enemy is Sudaram s)
                {
                    s.detectionRange *= 0.6f;
                    s.originiumPollutionBonusASPD += 40f;
                    s.originiumPollutionDamageMultiplier = 0f;
                    s.mHealth *= 0.75f;
                    hasChanged = true;
                }
                else if (enemy as OriginiumSpiderAlpha)
                {
                    enemy.bAtk = (short)(enemy.bAtk * 0.85f);
                    hasChanged = true;
                }
                else if (enemy is BloodboilKnight b)
                {
                    b.maxStackCount *= 2;
                    b.mspdAddPerEnemyKilled /= 2;
                    b.aspdAddPerEnemyKilled /= 2;
                    b.atkAddPerEnemyKilled /= 2;
                    hasChanged = true;
                }
                break;

            case 9:
                if (enemy as Matterllurgist)
                {
                    enemy.ASPD += 40;
                    hasChanged = true;
                }

                if (enemy as Hound || enemy as Wetwork || enemy as Archer || enemy as BloodthirstyHeir)
                {
                    enemy.mHealth *= 1.3f;
                    hasChanged = true;
                }
                break;

            case 10:
                if (enemy is Sudaram sud)
                {
                    enemy.bAtk = (short)(enemy.bAtk * 0.6f);
                    enemy.mHealth = 150;
                    enemy.bDef /= 2;
                    enemy.bRes = 0;
                    sud.originiumPollutionDamageMultiplier = 0f;
                    hasChanged = true;
                }
                else if (enemy is SaintStatue ss10)
                {
                    ss10.DefBuffFlat = 35f;
                    ss10.ResBuffFlat = 25f;
                    ss10.HealOnDeathPercentage = 0.65f;
                }
                break;

            case 11:
                break;

            case 12:
                if (enemy is Sudaram su)
                {
                    su.mHealth = 200;
                    su.originiumPollutionBonusASPD = 100;
                    su.originiumPollutionDamageMultiplier = 0f;
                    hasChanged = true;
                }
                break;

            case 13:
                if (enemy as HibernatorKnight)
                {
                    enemy.bDef += 40;
                    enemy.bRes += 40;
                    hasChanged = true;
                }
                else if (enemy is Sudaram su12)
                {
                    su12.originiumPollutionBonusASPD += 50;
                    hasChanged = true;
                }
                else if (enemy is Sentinel sen12)
                {
                    sen12.mHealth += 100;
                    sen12.AtkBuffOnAlert += 0.3f;
                    sen12.SpeedBuffOnAlert += 0.2f;
                    hasChanged = true;
                }
                break;

            case 14:
                if (enemy is HibernatorKnight hk)
                {
                    hk.ASPD += 30;
                    hk.Wake_AtkBuff += 0.5f;
                    hk.Wake_MspdBuff += 0.5f;
                    hk.Wake_Buff_Duration = 7f;
                    hasChanged = true;
                }
                break;

            case 15:
                if (enemy as Sentinel)
                {
                    enemy.b_moveSpeed += 50f;
                    hasChanged = true;
                }
                break;

            case 16:
                if (enemy is SaintStatue ss15)
                {
                    ss15.mHealth = 400;
                    ss15.DefBuffFlat = 10f;
                    ss15.ResBuffFlat = 10f;
                    ss15.DefFlatBuffOnDeathPercentage = 20f;
                    hasChanged = true;
                }
                else if (enemy as HibernatorKnight)
                {
                    enemy.mHealth = 280;
                    hasChanged = true;
                }
                else if (enemy as Gloompincer)
                {
                    enemy.resPen += 20;
                    hasChanged = true;
                }
                break;
        }
    }

}