using System;

public static class GameEvents
{
    public static event Action<Enemy> EnemySpawned;
    public static event Action<Enemy> EnemyDestroyed;

    public static void TriggerEnemySpawned(Enemy enemy) => EnemySpawned?.Invoke(enemy);
    public static void TriggerEnemyDestroyed(Enemy enemy) => EnemyDestroyed?.Invoke(enemy);
}
