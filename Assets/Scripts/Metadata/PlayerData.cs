using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public float MaxHealth;
    public float MoveSpeed;
    public float RotationSpeed;
    public float AttackInterval;
    public float Cooldown;
    public float LockOnRadius;
    public float LoseTargetRadius;
    public float MeleeRadius;
    public float BaseDamage;

    [Header("Upgrade Values")]
    public float DamageIncrement;
    public float AttackIntervalDecrement;
    public float CooldownDecrement;
    public float MoveSpeedIncrement;
    public float RangeIncrement;


    [Header("Power Up Stats")]
    public float HealAmount;
    public float InvincibilityDuration;
    public float AttackUpMultiplier;
    public float DefenseUpReduction; // damage taken = damage * (1 - reduction)
    public float DefenseDownDebuff; // enemy takes 1 + debuff damage
    public int InitialPowerUpCount;
}

[System.Serializable]
public class ArrowStats
{
    public string ArrowType;
    public string PrefabName;
    public float Cooldown;
    public float DamageMultiplier;
}
