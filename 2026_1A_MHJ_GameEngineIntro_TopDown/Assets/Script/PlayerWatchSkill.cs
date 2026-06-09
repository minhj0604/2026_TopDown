using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerWatchSkill : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] private float defaultSkillCost = 30f;

    [Header("Exoskeleton Counter")]
    [SerializeField] private float counterDelay = 0.2f;
    [SerializeField] private float counterSmashDamageMultiplier = 3.2f;
    [SerializeField] private float counterSmashKnockback = 5.5f;
    [SerializeField] private float counterSmashGroggyDuration = 2f;
    [SerializeField] private float counterSmashStopDistance = 0.45f;
    [SerializeField] private float counterSmashDashSpeed = 9f;
    [SerializeField] private float counterSmashMaxDashTime = 0.35f;
    [SerializeField] private float counterSmashInvincibleTime = 0.45f;
    [SerializeField] private float counterSmashCameraLeadDistance = 0.24f;
    [SerializeField] private float counterSmashCameraLeadTime = 0.18f;
    [SerializeField] private float counterSmashShakeTime = 0.2f;
    [SerializeField] private float counterSmashShakePower = 0.11f;

    [Header("Rapier Time Stop")]
    [SerializeField] private float timeStopDuration = 2f;
    [SerializeField] private float timeStopReadyWindow = 0.65f;
    [SerializeField] private float timeStopReadySlowScale = 0.22f;
    [SerializeField] private float timeStopReadyCameraZoom = 0.13f;
    [SerializeField] private Color timeStopReadyTint = new Color(0.05f, 0.05f, 0.05f, 1f);
    [SerializeField] private float timeStopStartShakeTime = 0.08f;
    [SerializeField] private float timeStopStartShakePower = 0.04f;
    [SerializeField] private float timeStopEndShakeTime = 0.13f;
    [SerializeField] private float timeStopEndShakePower = 0.07f;

    [Header("Scythe Chain Slash")]
    [SerializeField] private float markRange = 4f;
    [SerializeField] private float blinkOffset = 0.45f;
    [SerializeField] private float blinkSlashRadius = 0.9f;
    [SerializeField] private float blinkInvincibleTime = 0.35f;
    [SerializeField] private float scytheChainReturnDelay = 0.85f;
    [SerializeField] private float scytheChainInputCooldown = 0.12f;
    [SerializeField] private float scytheChainDamageMultiplier = 1.25f;
    [SerializeField] private float scytheChainKnockback = 2.6f;
    [SerializeField] private float scytheChainReturnInvincibleTime = 0.3f;
    [SerializeField] private float scytheChainShakeTime = 0.12f;
    [SerializeField] private float scytheChainShakePower = 0.065f;
    [SerializeField] private float scytheChainMoveTime = 0.12f;
    [SerializeField] private float scytheChainReturnMoveTime = 0.14f;
    [SerializeField] private float scytheChainCameraLeadDistance = 0.75f;
    [SerializeField] private float scytheChainCameraLeadDistanceRatio = 0.28f;
    [SerializeField] private float scytheChainCameraLeadMaxDistance = 1.15f;
    [SerializeField] private float scytheChainCameraLeadTime = 0.16f;

    private PlayerCombat combat;
    private PlayerDodge dodge;
    private PlayerParry parry;
    private ClockOutputSystem clockOutput;
    private PlayerHealth health;
    private Rigidbody2D rb;
    private bool isUsingSkill;
    private bool timeStopReady;
    private Coroutine timeStopReadyRoutine;
    private MonoBehaviour lastBlinkTarget;
    private bool scytheChainActive;
    private bool scytheChainJumping;
    private float scytheChainCooldownTimer;
    private Coroutine scytheChainReturnRoutine;
    private Vector2 scytheChainStartPosition;
    private MonoBehaviour scytheChainFirstTarget;
    private SpriteRenderer[] tintedRenderers = new SpriteRenderer[0];
    private Color[] originalTintColors = new Color[0];
    private Tilemap[] tintedTilemaps = new Tilemap[0];
    private Color[] originalTilemapColors = new Color[0];

    private void Awake()
    {
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();
        parry = GetComponent<PlayerParry>();
        clockOutput = GetComponent<ClockOutputSystem>();
        health = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();

        if (dodge != null)
            dodge.JustDodged += OnJustDodged;
    }

    private void OnDestroy()
    {
        if (dodge != null)
            dodge.JustDodged -= OnJustDodged;
    }

    private void Update()
    {
        if (scytheChainCooldownTimer > 0f)
            scytheChainCooldownTimer -= Time.deltaTime;
    }

    public void OnSprint(InputValue value)
    {
        if (!value.isPressed) return;
        TryUseCurrentWatchSkill();
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;
        TryUseCurrentWatchSkill();
    }

    private void TryUseCurrentWatchSkill()
    {
        if (combat == null || combat.CurrentWeapon == null) return;
        if (isUsingSkill && combat.CurrentWeapon.watchSkillType != WatchSkillType.MarkAndBlink)
            return;

        switch (combat.CurrentWeapon.watchSkillType)
        {
            case WatchSkillType.ParryCounter:
                TryCounterSmash();
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

    private void TryCounterSmash()
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
        StartCoroutine(CounterSmashRoutine());
    }

    private IEnumerator CounterSmashRoutine()
    {
        isUsingSkill = true;
        MonoBehaviour target = GetCounterTarget();
        if (target == null)
        {
            Debug.Log("No counter target.", this);
            isUsingSkill = false;
            yield break;
        }

        if (health != null)
            health.MakeInvincible(counterSmashInvincibleTime);

        ApplyCounterCameraLead(target);
        yield return DashToCounterTarget(target);
        yield return new WaitForSeconds(counterDelay);

        ApplyCounterSmash(target);
        ShakeCounterCamera();

        Debug.Log("Counter smash.", this);
        isUsingSkill = false;
    }

    private MonoBehaviour GetCounterTarget()
    {
        MonoBehaviour target = parry != null ? parry.LastParriedTarget : null;
        IRoomEnemy roomEnemy = target as IRoomEnemy;
        if (target != null && roomEnemy != null && !roomEnemy.IsDead)
            return target;

        return FindNearestEnemy();
    }

    private IEnumerator DashToCounterTarget(MonoBehaviour target)
    {
        if (rb == null || target == null)
            yield break;

        float timer = 0f;
        while (target != null && timer < counterSmashMaxDashTime)
        {
            Vector2 targetPosition = target.transform.position;
            Vector2 toTarget = targetPosition - rb.position;
            float distance = toTarget.magnitude;
            if (distance <= counterSmashStopDistance)
                break;

            Vector2 direction = toTarget.normalized;
            float moveDistance = Mathf.Min(distance - counterSmashStopDistance, counterSmashDashSpeed * Time.fixedDeltaTime);
            rb.position += direction * moveDistance;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private void ApplyCounterSmash(MonoBehaviour target)
    {
        if (target == null || combat == null || combat.CurrentWeapon == null)
            return;

        IRoomEnemy roomEnemy = target as IRoomEnemy;
        if (roomEnemy != null && roomEnemy.IsDead)
            return;

        Vector2 hitDirection = target.transform.position - transform.position;
        if (hitDirection.sqrMagnitude <= 0.01f)
            hitDirection = Vector2.down;

        IDamageable damageable = target as IDamageable;
        if (damageable != null)
        {
            float damage = combat.CurrentWeapon.attackPower * counterSmashDamageMultiplier;
            damageable.TakeDamage(damage, target.transform.position, hitDirection.normalized * counterSmashKnockback);
        }

        IEnemyStatusReceiver statusReceiver = target as IEnemyStatusReceiver;
        if (statusReceiver != null)
            statusReceiver.ApplyGroggy(counterSmashGroggyDuration);
    }

    private void ApplyCounterCameraLead(MonoBehaviour target)
    {
        if (target == null) return;

        Vector2 direction = target.transform.position - transform.position;
        if (direction.sqrMagnitude <= 0.01f)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        cameraControl.LeadToward(direction, counterSmashCameraLeadDistance, counterSmashCameraLeadTime);
    }

    private void ShakeCounterCamera()
    {
        ShakeCamera(counterSmashShakeTime, counterSmashShakePower);
    }

    private void TryTimeStop()
    {
        if (!HasEnoughSkillCost())
        {
            Debug.Log("Not enough clock output.", this);
            return;
        }

        if (dodge == null || !timeStopReady || !dodge.ConsumeJustDodge())
        {
            Debug.Log("Time Stop requires the just-dodge timing window.", this);
            return;
        }

        if (!TrySpendSkillCost()) return;
        StopTimeStopReadySlowOnly();
        StartCoroutine(TimeStopRoutine());
    }

    private void OnJustDodged()
    {
        if (combat == null || combat.CurrentWeapon == null) return;
        if (combat.CurrentWeapon.watchSkillType != WatchSkillType.JustEvadeTimeStop) return;

        if (timeStopReadyRoutine != null)
            StopCoroutine(timeStopReadyRoutine);
        timeStopReadyRoutine = StartCoroutine(TimeStopReadyRoutine());
    }

    private IEnumerator TimeStopReadyRoutine()
    {
        timeStopReady = true;
        ApplyTimeStopReadyTint();
        SetTimeStopReadyCameraZoom();

        Time.timeScale = timeStopReadySlowScale;

        yield return new WaitForSecondsRealtime(timeStopReadyWindow);

        if (Mathf.Approximately(Time.timeScale, timeStopReadySlowScale))
            Time.timeScale = 1f;

        ClearTimeStopReadyCameraZoom(false);
        RestoreTimeStopReadyTint();
        timeStopReady = false;
        timeStopReadyRoutine = null;
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
        if (health != null)
            health.MakeInvincible(timeStopDuration + 0.2f);

        ShakeCamera(timeStopStartShakeTime, timeStopStartShakePower);

        yield return new WaitForSeconds(timeStopDuration);

        for (int i = 0; i < enemies.Length; i++)
        {
            IEnemyStatusReceiver statusReceiver = enemies[i] as IEnemyStatusReceiver;
            if (statusReceiver != null)
                statusReceiver.SetTimeStopped(false);
        }

        ShakeCamera(timeStopEndShakeTime, timeStopEndShakePower);
        ClearTimeStopReadyCameraZoom(false);
        RestoreTimeStopReadyTint();

        isUsingSkill = false;
    }

    private bool IsRoomEnemyDead(MonoBehaviour target)
    {
        IRoomEnemy roomEnemy = target as IRoomEnemy;
        return roomEnemy != null && roomEnemy.IsDead;
    }

    private void ApplyTimeStopReadyTint()
    {
        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        tintedRenderers = new SpriteRenderer[renderers.Length];
        originalTintColors = new Color[renderers.Length];
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer == playerRenderer)
                continue;
            if (renderer.transform.IsChildOf(transform))
                continue;

            tintedRenderers[count] = renderer;
            originalTintColors[count] = renderer.color;
            renderer.color = InvertColor(renderer.color);
            count++;
        }

        System.Array.Resize(ref tintedRenderers, count);
        System.Array.Resize(ref originalTintColors, count);

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        tintedTilemaps = new Tilemap[tilemaps.Length];
        originalTilemapColors = new Color[tilemaps.Length];
        int tilemapCount = 0;

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null)
                continue;

            tintedTilemaps[tilemapCount] = tilemap;
            originalTilemapColors[tilemapCount] = tilemap.color;
            tilemap.color = InvertColor(tilemap.color);
            tilemapCount++;
        }

        System.Array.Resize(ref tintedTilemaps, tilemapCount);
        System.Array.Resize(ref originalTilemapColors, tilemapCount);
    }

    private void RestoreTimeStopReadyTint()
    {
        for (int i = 0; i < tintedRenderers.Length; i++)
        {
            if (tintedRenderers[i] != null)
                tintedRenderers[i].color = originalTintColors[i];
        }

        tintedRenderers = new SpriteRenderer[0];
        originalTintColors = new Color[0];

        for (int i = 0; i < tintedTilemaps.Length; i++)
        {
            if (tintedTilemaps[i] != null)
                tintedTilemaps[i].color = originalTilemapColors[i];
        }

        tintedTilemaps = new Tilemap[0];
        originalTilemapColors = new Color[0];
    }

    private Color InvertColor(Color color)
    {
        return new Color(
            1f - color.r,
            1f - color.g,
            1f - color.b,
            color.a);
    }

    private void StopTimeStopReadyWindow()
    {
        if (timeStopReadyRoutine != null)
        {
            StopCoroutine(timeStopReadyRoutine);
            timeStopReadyRoutine = null;
        }

        RestoreTimeStopReadyTint();
        timeStopReady = false;
        Time.timeScale = 1f;
        ClearTimeStopReadyCameraZoom(false);
    }

    private void StopTimeStopReadySlowOnly()
    {
        if (timeStopReadyRoutine != null)
        {
            StopCoroutine(timeStopReadyRoutine);
            timeStopReadyRoutine = null;
        }

        timeStopReady = false;
        Time.timeScale = 1f;
    }

    private void SetTimeStopReadyCameraZoom()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        float zoomAmount = Mathf.Max(timeStopReadyCameraZoom, 0.13f);
        cameraControl.SetFocusZoom(zoomAmount);
    }

    private void ClearTimeStopReadyCameraZoom(bool snapBack)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl != null)
            cameraControl.ClearFocusZoom(false, snapBack);
    }

    private void ShakeCamera(float duration, float power)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake shake = mainCamera.GetComponent<SimpleCameraShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        shake.Shake(duration, power);
    }

    private void TryMarkAndBlink()
    {
        if (scytheChainCooldownTimer > 0f || scytheChainJumping)
            return;

        if (!HasEnoughSkillCost())
        {
            Debug.Log("Not enough clock output.", this);
            return;
        }

        MonoBehaviour target = scytheChainActive ? FindFarthestEnemyFromFirstScytheTarget() : FindNearestEnemy(false);
        if (target == null)
        {
            Debug.Log("No marked target in range.", this);
            return;
        }

        if (!TrySpendSkillCost()) return;

        if (!scytheChainActive)
        {
            scytheChainActive = true;
            scytheChainStartPosition = rb != null ? rb.position : (Vector2)transform.position;
            scytheChainFirstTarget = target;
        }

        scytheChainCooldownTimer = scytheChainInputCooldown;
        isUsingSkill = true;
        StartCoroutine(ScytheChainJumpRoutine(target));
    }

    private IEnumerator ScytheChainJumpRoutine(MonoBehaviour target)
    {
        scytheChainJumping = true;
        if (target != null)
        {
            Vector2 playerPosition = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 targetPosition = target.transform.position;
            Vector2 leadDirection = targetPosition - playerPosition;
            Vector2 direction = leadDirection.sqrMagnitude > 0.01f ? leadDirection.normalized : Vector2.down;
            Vector2 blinkPosition = targetPosition - direction * blinkOffset;

            ApplyScytheChainCameraLead(leadDirection);
            if (health != null)
                health.MakeInvincible(blinkInvincibleTime);

            yield return MoveScytheChainPlayer(blinkPosition, scytheChainMoveTime);

            lastBlinkTarget = target;
            ApplyBlinkSlash(targetPosition);
            ShakeCamera(scytheChainShakeTime, scytheChainShakePower);
            Debug.Log($"Scythe chain slash: {target.name}", this);
            ResetScytheChainReturnTimer();
        }

        scytheChainJumping = false;
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
            {
                Vector2 enemyPosition = enemies[i].transform.position;
                Vector2 hitDirection = enemyPosition - center;
                if (hitDirection.sqrMagnitude <= 0.01f)
                    hitDirection = enemyPosition - (Vector2)transform.position;
                if (hitDirection.sqrMagnitude <= 0.01f)
                    hitDirection = Vector2.down;

                float damage = combat.CurrentWeapon.attackPower * scytheChainDamageMultiplier;
                damageable.TakeDamage(damage, center, hitDirection.normalized * scytheChainKnockback);
            }
        }
    }

    private void ResetScytheChainReturnTimer()
    {
        if (scytheChainReturnRoutine != null)
            StopCoroutine(scytheChainReturnRoutine);

        scytheChainReturnRoutine = StartCoroutine(ScytheChainReturnRoutine());
    }

    private IEnumerator ScytheChainReturnRoutine()
    {
        yield return new WaitForSeconds(scytheChainReturnDelay);

        Vector2 returnDirection = scytheChainStartPosition - (rb != null ? rb.position : (Vector2)transform.position);
        ApplyScytheChainCameraLead(returnDirection);

        yield return MoveScytheChainPlayer(scytheChainStartPosition, scytheChainReturnMoveTime);

        if (health != null)
            health.MakeInvincible(scytheChainReturnInvincibleTime);

        scytheChainActive = false;
        scytheChainJumping = false;
        scytheChainFirstTarget = null;
        scytheChainReturnRoutine = null;
        isUsingSkill = false;
    }

    private IEnumerator MoveScytheChainPlayer(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = rb != null ? rb.position : (Vector2)transform.position;
        float moveDuration = Mathf.Max(0.04f, duration);
        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / moveDuration);
            float easedT = 1f - (1f - t) * (1f - t);
            Vector2 nextPosition = Vector2.Lerp(startPosition, targetPosition, easedT);

            if (rb != null)
                rb.MovePosition(nextPosition);
            else
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);

            yield return null;
        }

        if (rb != null)
            rb.position = targetPosition;
        else
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
    }

    private void ApplyScytheChainCameraLead(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.01f)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        cameraControl.LeadToward(direction, GetScytheChainCameraLeadDistance(direction), GetScytheChainCameraLeadTime());
    }

    private float GetScytheChainCameraLeadDistance(Vector2 direction)
    {
        float distanceBasedLead = direction.magnitude * Mathf.Max(0f, scytheChainCameraLeadDistanceRatio);
        float wantedLead = Mathf.Max(scytheChainCameraLeadDistance, distanceBasedLead);
        return Mathf.Min(wantedLead, scytheChainCameraLeadMaxDistance);
    }

    private float GetScytheChainCameraLeadTime()
    {
        return Mathf.Max(scytheChainCameraLeadTime, 0.1f);
    }

    private MonoBehaviour FindFarthestEnemyFromFirstScytheTarget()
    {
        if (scytheChainFirstTarget == null)
            return FindNearestEnemy(false);

        MonoBehaviour[] enemies = FindRoomEnemyBehaviours();
        Vector2 firstTargetPosition = scytheChainFirstTarget.transform.position;
        MonoBehaviour farthestEnemy = null;
        float farthestDistance = -1f;

        for (int pass = 0; pass < 2; pass++)
        {
            bool skipLastTarget = pass == 0;

            for (int i = 0; i < enemies.Length; i++)
            {
                IRoomEnemy roomEnemy = enemies[i] as IRoomEnemy;
                IDamageable damageable = enemies[i] as IDamageable;
                if (roomEnemy == null || damageable == null || roomEnemy.IsDead)
                    continue;
                if (skipLastTarget && enemies[i] == lastBlinkTarget)
                    continue;

                float distanceFromPlayer = Vector2.Distance(transform.position, enemies[i].transform.position);
                if (distanceFromPlayer > markRange)
                    continue;

                float distanceFromFirstTarget = Vector2.Distance(firstTargetPosition, enemies[i].transform.position);
                if (distanceFromFirstTarget > farthestDistance)
                {
                    farthestDistance = distanceFromFirstTarget;
                    farthestEnemy = enemies[i];
                }
            }

            if (farthestEnemy != null)
                return farthestEnemy;
        }

        return null;
    }

    private MonoBehaviour FindNearestEnemy(bool skipLastTargetFirstPass = true)
    {
        MonoBehaviour[] enemies = FindRoomEnemyBehaviours();
        Vector2 playerPosition = transform.position;

        for (int pass = 0; pass < 2; pass++)
        {
            MonoBehaviour nearestEnemy = FindNearestEnemyInPass(enemies, playerPosition, skipLastTargetFirstPass && pass == 0);
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
