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
