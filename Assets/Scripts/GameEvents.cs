using System;

public enum StatType { Damage, Range, AttackInterval, Cooldown, MoveSpeed }
public enum ArrowType { Normal, Predictive, Piercing }
public enum EnemyType { Melee, Ranged, Healer, Tank, Boss }
public enum PowerUpType { Heal, Invincibility, AttackUp, DefenseUp, DefenseDown }

public static class GameEvents
{
    public static event Action<Enemy> EnemySpawned;
    public static event Action<Enemy> EnemyDestroyed;
    public static event Action PlayerDied;
    
    public static event Action PlayerStatsUpdated;
    public static event Action<ArrowType> SpecialArrowRequested;
    public static event Action<PowerUpType> PowerUpActivated;

    public static void TriggerEnemySpawned(Enemy enemy) => EnemySpawned?.Invoke(enemy);
    public static void TriggerEnemyDestroyed(Enemy enemy) => EnemyDestroyed?.Invoke(enemy);
    public static void TriggerPlayerDied() => PlayerDied?.Invoke();
    
    public static void TriggerStatsUpdated() => PlayerStatsUpdated?.Invoke();
    public static void TriggerSpecialArrow(ArrowType arrowType) => SpecialArrowRequested?.Invoke(arrowType);
    public static void TriggerPowerUp(PowerUpType type) => PowerUpActivated?.Invoke(type);
}
