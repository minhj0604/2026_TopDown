public interface IRoomEnemy
{
    bool IsDead { get; }
    void ResetEnemy();
}

public interface IEnemyStatusReceiver
{
    void SetTimeStopped(bool isStopped);
    void ApplyGroggy(float duration);
}

public interface IParryableEnemyAttack
{
    bool IsParryableAttackActive { get; }
    void OnParried(UnityEngine.Vector2 parryDirection);
}
