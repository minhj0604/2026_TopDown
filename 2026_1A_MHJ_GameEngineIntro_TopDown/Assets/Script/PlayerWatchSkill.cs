using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWatchSkill : MonoBehaviour
{
    [Header("공통")]
    [SerializeField] private float defaultSkillCost = 30f;

    [Header("외골격: 반격 그로기")]
    [SerializeField] private float counterDelay = 0.2f;
    [SerializeField] private float counterGroggyDuration = 1.2f;

    [Header("레이피어: 저스트 회피 후 시간 정지")]
    [SerializeField] private float timeStopDuration = 1.5f;

    [Header("낫: 마킹 후 공간 도약")]
    [SerializeField] private float markRange = 4f;
    [SerializeField] private float blinkOffset = 0.45f;
    [SerializeField] private float blinkSlashRadius = 0.5f;
    [SerializeField] private float blinkInvincibleTime = 0.35f;

    private PlayerCombat combat;
    private PlayerDodge dodge;
    private PlayerParry parry;
    private ClockOutputSystem clockOutput;
    private PlayerHealth health;
    private Rigidbody2D rb;
    private bool isUsingSkill;
    private MonoBehaviour lastBlinkTarget;

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();
        parry = GetComponent<PlayerParry>();
        clockOutput = GetComponent<ClockOutputSystem>();
        health = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void OnSprint(InputValue value)
    {
        if (!value.isPressed) return;
        if (isUsingSkill) return;
        if (combat == null || combat.CurrentWeapon == null) return;

        switch (combat.CurrentWeapon.watchSkillType)
        {
            case WatchSkillType.ParryCounter:
                TryCounterGroggy();
                break;
            case WatchSkillType.JustEvadeTimeStop:
                TryTimeStop();
                break;
            case WatchSkillType.MarkAndBlink:
                TryMarkAndBlink();
                break;
        }
    }

    private float GetSkillCost(WeaponData weapon)
    {
        return weapon.gaugeCost > 0f ? weapon.gaugeCost : defaultSkillCost;
    }

    private bool TrySpendSkillCost()
    {
        float cost = GetSkillCost(combat.CurrentWeapon);
        if (clockOutput == null || clockOutput.TrySpend(cost))
            return true;

        Debug.Log("Not enough clock output.", this);
        return false;
    }

    private bool HasEnoughSkillCost()
    {
        float cost = GetSkillCost(combat.CurrentWeapon);
        return clockOutput == null || clockOutput.CanSpend(cost);
    }

    private void TryCounterGroggy()
    {
        if (!HasEnoughSkillCost())
        {
            Debug.Log("Not enough clock output.", this);
            return;
        }

        if (parry == null || !parry.ConsumeCounterReady())
        {
            Debug.Log("Counter requires a successful parry.", this);
            return;
        }

        if (!TrySpendSkillCost()) return;
        StartCoroutine(CounterGroggyRoutine());
    }

    private IEnumerator CounterGroggyRoutine()
    {
        isUsingSkill = true;
        Debug.Log("Counter charge.", this);
        yield return new WaitForSeconds(counterDelay);

        if (combat != null)
        {
            combat.ExecuteAttackHit();
            ApplyGroggyToNearbyEnemies();
        }

        Debug.Log("Counter groggy.", this);
        isUsingSkill = false;
    }

    private void ApplyGroggyToNearbyEnemies()
    {
        MonoBehaviour[] enemies = FindRoomEnemyBehaviours();
        Vector2 playerPosition = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            IRoomEnemy roomEnemy = enemies[i] as IRoomEnemy;
            IEnemyStatusReceiver statusReceiver = enemies[i] as IEnemyStatusReceiver;
            if (roomEnemy == null || statusReceiver == null || roomEnemy.IsDead)
                continue;

            float distance = Vector2.Distance(playerPosition, enemies[i].transform.position);
            if (distance <= markRange)
                statusReceiver.ApplyGroggy(counterGroggyDuration);
        }
    }

    private void TryTimeStop()
    {
        if (!HasEnoughSkillCost())
        {
            Debug.Log("Not enough clock output.", this);
            return;
        }

        if (dodge == null || !dodge.ConsumeJustDodge())
        {
            Debug.Log("Time Stop requires a recent dodge.", this);
            return;
        }

        if (!TrySpendSkillCost()) return;
        StartCoroutine(TimeStopRoutine());
    }

    private IEnumerator TimeStopRoutine()
    {
        isUsingSkill = true;

        MonoBehaviour[] enemies = FindRoomEnemyBehaviours();
        for (int i = 0; i < enemies.Length; i++)
        {
            IEnemyStatusReceiver statusReceiver = enemies[i] as IEnemyStatusReceiver;
            if (statusReceiver != null)
                statusReceiver.SetTimeStopped(true);
        }

        Debug.Log("Time Stop.", this);
        yield return new WaitForSeconds(timeStopDuration);

        for (int i = 0; i < enemies.Length; i++)
        {
            IEnemyStatusReceiver statusReceiver = enemies[i] as IEnemyStatusReceiver;
            if (statusReceiver != null)
                statusReceiver.SetTimeStopped(false);
        }

        isUsingSkill = false;
    }

    private void TryMarkAndBlink()
    {
        MonoBehaviour target = FindNearestEnemy();
        if (target == null)
        {
            Debug.Log("No marked target in range.", this);
            return;
        }

        if (!TrySpendSkillCost()) return;

        Vector2 playerPosition = transform.position;
        Vector2 targetPosition = target.transform.position;
        Vector2 direction = (targetPosition - playerPosition).normalized;
        Vector2 blinkPosition = targetPosition - direction * blinkOffset;

        if (rb != null)
            rb.position = blinkPosition;
        else
            transform.position = new Vector3(blinkPosition.x, blinkPosition.y, transform.position.z);

        lastBlinkTarget = target;
        ApplyBlinkSlash(blinkPosition);
        if (health != null)
            health.MakeInvincible(blinkInvincibleTime);

        Debug.Log($"Blink to marked target: {target.name}", this);
    }

    private void ApplyBlinkSlash(Vector2 center)
    {
        MonoBehaviour[] enemies = FindRoomEnemyBehaviours();
        for (int i = 0; i < enemies.Length; i++)
        {
            IRoomEnemy roomEnemy = enemies[i] as IRoomEnemy;
            IDamageable damageable = enemies[i] as IDamageable;
            if (roomEnemy == null || damageable == null || roomEnemy.IsDead)
                continue;

            float distance = Vector2.Distance(center, enemies[i].transform.position);
            if (distance <= blinkSlashRadius)
                damageable.TakeDamage(combat.CurrentWeapon.attackPower, center, Vector2.zero);
        }
    }

    private MonoBehaviour FindNearestEnemy()
    {
        MonoBehaviour[] enemies = FindRoomEnemyBehaviours();
        Vector2 playerPosition = transform.position;

        for (int pass = 0; pass < 2; pass++)
        {
            MonoBehaviour nearestEnemy = FindNearestEnemyInPass(enemies, playerPosition, pass == 0);
            if (nearestEnemy != null)
                return nearestEnemy;
        }

        return null;
    }

    private MonoBehaviour FindNearestEnemyInPass(MonoBehaviour[] enemies, Vector2 playerPosition, bool skipLastTarget)
    {
        MonoBehaviour nearestEnemy = null;
        float nearestDistance = markRange;

        for (int i = 0; i < enemies.Length; i++)
        {
            IRoomEnemy roomEnemy = enemies[i] as IRoomEnemy;
            IDamageable damageable = enemies[i] as IDamageable;
            if (roomEnemy == null || damageable == null || roomEnemy.IsDead)
                continue;
            if (skipLastTarget && enemies[i] == lastBlinkTarget)
                continue;

            Vector2 enemyPosition = enemies[i].transform.position;
            float distance = Vector2.Distance(playerPosition, enemyPosition);
            if (distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemies[i];
            }
        }

        return nearestEnemy;
    }

    private MonoBehaviour[] FindRoomEnemyBehaviours()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        int enemyCount = 0;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRoomEnemy)
                enemyCount++;
        }

        MonoBehaviour[] enemies = new MonoBehaviour[enemyCount];
        int index = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRoomEnemy)
            {
                enemies[index] = behaviours[i];
                index++;
            }
        }

        return enemies;
    }
}
