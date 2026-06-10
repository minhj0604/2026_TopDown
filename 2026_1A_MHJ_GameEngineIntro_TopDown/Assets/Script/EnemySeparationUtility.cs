using UnityEngine;

public static class EnemySeparationUtility
{
    public static Vector2 GetSeparation(MonoBehaviour self, Vector2 selfPosition, float range)
    {
        if (self == null || range <= 0f)
            return Vector2.zero;

        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        Vector2 separation = Vector2.zero;
        float sqrRange = range * range;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour other = behaviours[i];
            if (other == null || other == self || !other.gameObject.activeInHierarchy)
                continue;

            IRoomEnemy roomEnemy = other as IRoomEnemy;
            if (roomEnemy == null || roomEnemy.IsDead)
                continue;

            Vector2 away = selfPosition - (Vector2)other.transform.position;
            float sqrDistance = away.sqrMagnitude;
            if (sqrDistance > sqrRange)
                continue;

            if (sqrDistance <= 0.000001f)
                away = GetFallbackDirection(self, other);

            float distance = Mathf.Max(away.magnitude, 0.001f);
            separation += away / distance * (1f - distance / range);
        }

        return separation;
    }

    public static Vector2 AddSeparation(MonoBehaviour self, Vector2 moveDirection, float range, float weight)
    {
        Vector2 separation = GetSeparation(self, self.transform.position, range);
        Vector2 combined = moveDirection + separation * weight;
        return combined.sqrMagnitude > 0.0001f ? combined.normalized : moveDirection;
    }

    private static Vector2 GetFallbackDirection(MonoBehaviour self, MonoBehaviour other)
    {
        int hash = self.GetInstanceID() ^ other.GetInstanceID();
        float angle = (hash % 360) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
