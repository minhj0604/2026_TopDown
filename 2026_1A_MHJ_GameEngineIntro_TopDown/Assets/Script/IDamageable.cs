using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, Vector2 hitPoint, Vector2 hitDirection);
}
