using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Weapon")]
    public WeaponData currentWeapon;
    public WeaponData weaponSlot1;
    public WeaponData weaponSlot2;
    public WeaponData weaponSlot3;

    [Header("Clock Gauge")]
    [SerializeField] private ClockOutputSystem clockOutput;
    [SerializeField] private float swapAttackBonusTime = 1.2f;

    [Header("Combo")]
    public int maxCombo = 3;
    [SerializeField] private int animatorComboStateCount = 5;
    public float comboResetTime = 0.8f;
    [SerializeField] private float comboEndRecoveryTime = 0.35f;
    [SerializeField] private float attackSafetyTime = 1.2f;
    [SerializeField] private float attackEndBackupTime = 0.3f;
    [SerializeField] private float attackInputBufferTime = 0.45f;

    [Header("Animator Parameters")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string comboStepParam = "ComboStep";

    [Header("Hit Check")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float minimumHitRadius = 0.15f;
    [SerializeField] private bool showDebugHitArea = true;

    [Header("Range Preview")]
    [SerializeField] private bool showRangePreview = true;
    [SerializeField] private Color rangePreviewColor = new Color(1f, 0.25f, 0.15f, 0.55f);
    [SerializeField] private float rangePreviewWidth = 0.03f;
    [SerializeField] private int rangePreviewSegments = 48;

    [Header("Hitbox Flash")]
    [SerializeField] private bool showHitboxFlash = true;
    [SerializeField] private Color hitboxFlashColor = new Color(1f, 0f, 0f, 0.9f);
    [SerializeField] private float hitboxFlashWidth = 0.06f;
    [SerializeField] private float hitboxFlashTime = 0.2f;

    [Header("Hit Camera Shake")]
    [SerializeField] private bool shakeCameraOnHit = true;
    [SerializeField] private float hitShakeTime = 0.08f;
    [SerializeField] private float hitShakePower = 0.035f;

    public bool IsAttacking => isAttacking;
    public int ComboStep => comboStep;
    public int CurrentWeaponSlot => currentWeaponIndex + 1;
    public WeaponData CurrentWeapon => currentWeapon;

    private Animator animator;
    private PlayerController controller;
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool isInComboRecovery = false;
    private bool bufferedInput = false;
    private bool hasExecutedHitThisAttack = false;
    private Coroutine resetRoutine;
    private Coroutine attackSafetyRoutine;
    private Vector2 attackDirection = Vector2.down;
    private int currentWeaponIndex = 0;
    private LineRenderer rangePreview;
    private LineRenderer hitboxFlash;
    private SpriteRenderer hitboxFlashSprite;
    private float swapAttackBonusTimer = 0f;
    private float queuedAttackTimer = 0f;
    private float hitboxFlashTimer = 0f;
    private int attackSerial = 0;

    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly List<MonoBehaviour> damagedTargets = new List<MonoBehaviour>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
        clockOutput = GetComponent<ClockOutputSystem>();

        if (weaponSlot1 == null)
            weaponSlot1 = currentWeapon;

        EquipWeapon(0, false);
        SetupRangePreview();
    }

    private void LateUpdate()
    {
        if (swapAttackBonusTimer > 0f)
            swapAttackBonusTimer -= Time.deltaTime;
        if (queuedAttackTimer > 0f)
            queuedAttackTimer -= Time.deltaTime;

        UpdateHitboxFlashTimer();
        ProcessQueuedAttack();
        UpdateRangePreview();
    }

    public void OnMove(InputValue value)
    {
        Vector2 moveInput = value.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
            attackDirection = moveInput.normalized;
    }

    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        QueueAttackInput();
    }

    public void OnPrevious(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(0, true);
    }

    public void OnNext(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(1, true);
    }

    public void OnCrouch(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(2, true);
    }

    private void QueueAttackInput()
    {
        if (isInComboRecovery)
            return;

        if (isAttacking && comboStep >= GetCurrentMaxCombo())
        {
            bufferedInput = false;
            queuedAttackTimer = 0f;
            return;
        }

        queuedAttackTimer = attackInputBufferTime;
        if (isAttacking)
            bufferedInput = true;

        ProcessQueuedAttack();
    }

    private void ProcessQueuedAttack()
    {
        if (queuedAttackTimer <= 0f) return;
        if (isInComboRecovery) return;
        if (isAttacking)
        {
            bufferedInput = true;
            return;
        }

        queuedAttackTimer = 0f;
        StartNextCombo();
    }

    private void StartNextCombo()
    {
        if (currentWeapon != null && currentWeapon.attackSpeed > 0f)
            animator.speed = currentWeapon.attackSpeed;

        comboStep++;
        int currentMaxCombo = GetCurrentMaxCombo();
        if (comboStep > currentMaxCombo)
            comboStep = 1;

        isAttacking = true;
        bufferedInput = false;
        hasExecutedHitThisAttack = false;
        int currentAttackSerial = ++attackSerial;

        animator.SetInteger(comboStepParam, comboStep);
        animator.SetTrigger(attackTrigger);

        if (attackSafetyRoutine != null)
            StopCoroutine(attackSafetyRoutine);
        attackSafetyRoutine = StartCoroutine(AttackSafetyTimer(currentAttackSerial));

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
    }

    private void EquipWeapon(int slotIndex, bool resetCombo)
    {
        if (isAttacking) return;

        WeaponData nextWeapon = GetWeaponInSlot(slotIndex);
        if (nextWeapon == null) return;

        currentWeapon = nextWeapon;
        currentWeaponIndex = slotIndex;
        animator.speed = 1f;

        if (resetCombo)
        {
            swapAttackBonusTimer = swapAttackBonusTime;
            ResetCombo(true);
        }
    }

    private WeaponData GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex == 0) return weaponSlot1;
        if (slotIndex == 1) return weaponSlot2;
        if (slotIndex == 2) return weaponSlot3;
        return null;
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo(true);
    }

    private IEnumerator AttackSafetyTimer(int expectedAttackSerial)
    {
        float speed = currentWeapon != null && currentWeapon.attackSpeed > 0f
            ? currentWeapon.attackSpeed
            : 1f;
        float backupTime = Mathf.Min(attackSafetyTime, attackEndBackupTime);
        yield return new WaitForSeconds(backupTime / speed);

        if (isAttacking && expectedAttackSerial == attackSerial)
        {
            attackSafetyRoutine = null;
            OnAttackAnimationEnd();
            yield break;
        }

        attackSafetyRoutine = null;
    }

    public void ExecuteAttackHit()
    {
        if (!isAttacking) return;
        if (hasExecutedHitThisAttack) return;
        if (currentWeapon == null) return;

        hasExecutedHitThisAttack = true;
        GetCurrentHitArea(out Vector2 hitCenter, out float radius);
        ShowHitboxFlash(hitCenter, radius);

        int hitCount = Physics2D.OverlapCircleNonAlloc(hitCenter, radius, hitBuffer, hitLayers);
        damagedTargets.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i];
            if (hitCollider == null) continue;

            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                IDamageable damageable = behaviour as IDamageable;
                if (damageable == null) continue;
                if (damagedTargets.Contains(behaviour)) continue;

                Vector2 hitPoint = hitCollider.ClosestPoint(hitCenter);
                damageable.TakeDamage(currentWeapon.attackPower, hitPoint, attackDirection);
                damagedTargets.Add(behaviour);
                break;
            }
        }

        if (clockOutput != null && damagedTargets.Count > 0)
        {
            bool usedSwapBonus = swapAttackBonusTimer > 0f;
            clockOutput.GainFromAttackHit(damagedTargets.Count, usedSwapBonus);
            if (usedSwapBonus)
                swapAttackBonusTimer = 0f;
        }

        if (damagedTargets.Count > 0)
            ShakeCameraOnHit();
    }

    public void OnAttackAnimationEnd()
    {
        if (!isAttacking) return;
        if (isInComboRecovery) return;

        if (attackSafetyRoutine != null)
        {
            StopCoroutine(attackSafetyRoutine);
            attackSafetyRoutine = null;
        }

        bool isFinalComboStep = comboStep >= GetCurrentMaxCombo();

        if (isFinalComboStep)
        {
            StartCoroutine(ComboEndRecoveryRoutine());
        }
        else if (bufferedInput)
        {
            queuedAttackTimer = 0f;
            isAttacking = false;
            StartNextCombo();
        }
        else
        {
            isAttacking = false;
            StartComboResetTimer();
        }
    }

    private void StartComboResetTimer()
    {
        if (resetRoutine != null)
            StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(ComboResetTimer());
    }

    private IEnumerator ComboEndRecoveryRoutine()
    {
        isAttacking = false;
        isInComboRecovery = true;
        bufferedInput = false;
        hasExecutedHitThisAttack = false;
        animator.speed = 1f;

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
        if (attackSafetyRoutine != null)
        {
            StopCoroutine(attackSafetyRoutine);
            attackSafetyRoutine = null;
        }

        if (controller != null)
            controller.RefreshAfterAttack();

        yield return new WaitForSeconds(comboEndRecoveryTime);
        ResetCombo(true);
    }

    private void ResetCombo(bool clearQueuedAttack)
    {
        comboStep = 0;
        isAttacking = false;
        isInComboRecovery = false;
        bufferedInput = false;
        hasExecutedHitThisAttack = false;
        if (clearQueuedAttack)
            queuedAttackTimer = 0f;
        animator.SetInteger(comboStepParam, 0);
        animator.speed = 1f;
        resetRoutine = null;
        attackSerial++;

        if (attackSafetyRoutine != null)
        {
            StopCoroutine(attackSafetyRoutine);
            attackSafetyRoutine = null;
        }

        if (controller != null)
            controller.RefreshAfterAttack();
    }

    private int GetCurrentMaxCombo()
    {
        int weaponComboCount = maxCombo;
        if (currentWeapon != null && currentWeapon.comboCount > 0)
            weaponComboCount = currentWeapon.comboCount;

        return Mathf.Min(weaponComboCount, animatorComboStateCount);
    }

    private void SetupRangePreview()
    {
        rangePreview = GetComponent<LineRenderer>();
        if (rangePreview == null)
            rangePreview = gameObject.AddComponent<LineRenderer>();

        SetupCircleRenderer(rangePreview, rangePreviewColor, rangePreviewWidth, 20);
        rangePreview.enabled = false;

        hitboxFlash = gameObject.AddComponent<LineRenderer>();
        SetupCircleRenderer(hitboxFlash, hitboxFlashColor, hitboxFlashWidth, 25);
        hitboxFlash.enabled = false;

        GameObject flashObject = new GameObject("HitboxFlash");
        flashObject.transform.SetParent(transform);
        hitboxFlashSprite = flashObject.AddComponent<SpriteRenderer>();
        hitboxFlashSprite.sprite = CreateCircleSprite();
        hitboxFlashSprite.color = new Color(hitboxFlashColor.r, hitboxFlashColor.g, hitboxFlashColor.b, 0.35f);
        hitboxFlashSprite.sortingOrder = 100;
        hitboxFlashSprite.enabled = false;
    }

    private void SetupCircleRenderer(LineRenderer renderer, Color color, float width, int sortingOrder)
    {
        renderer.useWorldSpace = true;
        renderer.loop = true;
        renderer.positionCount = Mathf.Max(12, rangePreviewSegments);
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = sortingOrder;
    }

    private void UpdateRangePreview()
    {
        if (rangePreview == null) return;

        bool shouldShow = showRangePreview && currentWeapon != null;
        rangePreview.enabled = shouldShow;
        if (!shouldShow) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);
        DrawCircle(rangePreview, hitCenter, radius, rangePreviewColor, rangePreviewWidth);
    }

    private void ShowHitboxFlash(Vector2 hitCenter, float radius)
    {
        if (!showHitboxFlash || hitboxFlash == null) return;

        DrawCircle(hitboxFlash, hitCenter, radius, hitboxFlashColor, hitboxFlashWidth);
        ShowHitboxFlashSprite(hitCenter, radius);
        hitboxFlashTimer = hitboxFlashTime;
        hitboxFlash.enabled = true;
    }

    private void ShowHitboxFlashSprite(Vector2 hitCenter, float radius)
    {
        if (hitboxFlashSprite == null) return;

        hitboxFlashSprite.transform.position = new Vector3(hitCenter.x, hitCenter.y, transform.position.z);
        hitboxFlashSprite.transform.localScale = Vector3.one * radius * 2f;
        hitboxFlashSprite.enabled = true;
    }

    private void UpdateHitboxFlashTimer()
    {
        if (hitboxFlashTimer <= 0f) return;

        hitboxFlashTimer -= Time.deltaTime;
        if (hitboxFlashTimer <= 0f && hitboxFlash != null)
            hitboxFlash.enabled = false;
        if (hitboxFlashTimer <= 0f && hitboxFlashSprite != null)
            hitboxFlashSprite.enabled = false;
    }

    private void DrawCircle(LineRenderer renderer, Vector2 center, float radius, Color color, float width)
    {
        int segmentCount = Mathf.Max(12, rangePreviewSegments);
        if (renderer.positionCount != segmentCount)
            renderer.positionCount = segmentCount;

        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.startColor = color;
        renderer.endColor = color;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (float)i / segmentCount * Mathf.PI * 2f;
            Vector3 point = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y + Mathf.Sin(angle) * radius,
                transform.position.z);
            renderer.SetPosition(i, point);
        }
    }

    private void GetCurrentHitArea(out Vector2 hitCenter, out float radius)
    {
        float range = currentWeapon != null
            ? Mathf.Max(currentWeapon.attackRange, minimumHitRadius)
            : minimumHitRadius;
        radius = Mathf.Max(range * 0.5f, minimumHitRadius);
        hitCenter = attackOrigin != null
            ? (Vector2)attackOrigin.position
            : (Vector2)transform.position + attackDirection * radius;
    }

    private Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;
        float outlineRadius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                Color pixel = Color.clear;

                if (distance <= radius)
                {
                    float alpha = distance >= outlineRadius ? 1f : 0.45f;
                    pixel = new Color(1f, 0f, 0f, alpha);
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void ShakeCameraOnHit()
    {
        if (!shakeCameraOnHit) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake shake = mainCamera.GetComponent<SimpleCameraShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        shake.Shake(hitShakeTime, hitShakePower);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugHitArea) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, radius);
    }
}
