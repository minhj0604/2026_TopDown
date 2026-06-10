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
    [SerializeField] private WeaponData lobbyWeaponCandidate1;
    [SerializeField] private WeaponData lobbyWeaponCandidate2;
    [SerializeField] private WeaponData lobbyWeaponCandidate3;

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
    [SerializeField] private float lungeStopDistanceFromEnemy = 0.28f;
    [SerializeField] private float rapierSlashAngle = 92f;
    [SerializeField] private float rapierSlashTilt = 16f;
    [SerializeField] private float rapierThrustWidth = 0.22f;
    [SerializeField] private float scytheSlashAngle = 178f;
    [SerializeField] private float scytheSlashTilt = 12f;
    [SerializeField] private float parryHitboxMultiplier = 1.45f;
    [SerializeField] private float minimumParryHitboxRadius = 0.65f;
    [SerializeField] private bool showDebugHitArea = true;

    [Header("Exoskeleton Focus")]
    [SerializeField] private float focusKeepTime = 1.4f;
    [SerializeField] private float focusStopDistance = 0.38f;
    [SerializeField] private float focusChaseSpeed = 6.5f;
    [SerializeField] private float focusChaseMaxTime = 0.28f;
    [SerializeField] private float focusChaseInvincibleTime = 0.35f;
    [SerializeField] private float focusCameraZoomPerHit = 0.015f;
    [SerializeField] private float focusCameraMaxZoomIn = 0.08f;

    [Header("Rapier Camera")]
    [SerializeField] private float rapierCameraZoomStep2 = 0.018f;
    [SerializeField] private float rapierCameraZoomStep3 = 0.038f;
    [SerializeField] private float rapierFinalShakeTime = 0.14f;
    [SerializeField] private float rapierFinalShakePower = 0.065f;

    [Header("Scythe Blink")]
    [SerializeField] private float scytheCameraZoomStep1 = 0.015f;
    [SerializeField] private float scytheCameraZoomStep2 = 0.035f;
    [SerializeField] private float scytheEarlyShakeTime = 0.08f;
    [SerializeField] private float scytheEarlyShakePower = 0.028f;
    [SerializeField] private float scytheFinalShakeTime = 0.17f;
    [SerializeField] private float scytheFinalShakePower = 0.08f;

    [Header("Action Camera Lead")]
    [SerializeField] private float focusChaseCameraLeadDistance = 0.18f;
    [SerializeField] private float focusChaseCameraLeadTime = 0.16f;

    [Header("Range Preview")]
    [SerializeField] private bool showRangePreview = true;
    [SerializeField] private Color rangePreviewColor = new Color(1f, 0.25f, 0.15f, 0.55f);
    [SerializeField] private float rangePreviewWidth = 0.03f;
    [SerializeField] private int rangePreviewSegments = 48;

    [Header("Hitbox Flash")]
    [SerializeField] private bool showHitboxFlash = true;
    [SerializeField] private Color hitboxFlashColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private float hitboxFlashWidth = 0.06f;
    [SerializeField] private float hitboxFlashTime = 0.08f;

    [Header("Hit Camera Shake")]
    [SerializeField] private bool shakeCameraOnHit = true;
    [SerializeField] private float hitShakeTime = 0.08f;
    [SerializeField] private float hitShakePower = 0.035f;
    [SerializeField] private float exoskeletonFinalShakeTime = 0.16f;
    [SerializeField] private float exoskeletonFinalShakePower = 0.075f;

    [Header("Charge Attack")]
    [SerializeField] private float chargeHoldTime = 0.75f;
    [SerializeField] private float chargeRecoveryTime = 0.18f;
    [SerializeField] private float exoskeletonChargeDistance = 2.8f;
    [SerializeField] private float exoskeletonChargeDuration = 0.18f;
    [SerializeField] private float exoskeletonChargeRadius = 0.52f;
    [SerializeField] private float exoskeletonChargeDamageMultiplier = 1.35f;
    [SerializeField] private float exoskeletonChargeKnockback = 2.4f;
    [SerializeField] private float exoskeletonChargeGroggyTime = 0.2f;
    [SerializeField] private float rapierChargeRange = 2.15f;
    [SerializeField] private float rapierChargeAngle = 118f;
    [SerializeField] private int rapierChargeHitCount = 5;
    [SerializeField] private float rapierChargeHitInterval = 0.055f;
    [SerializeField] private float rapierChargeDamageMultiplier = 0.42f;
    [SerializeField] private float rapierChargeKnockback = 0.45f;
    [SerializeField] private float rapierChargeGroggyTime = 0.05f;
    [SerializeField] private float scytheChargeRange = 4.2f;
    [SerializeField] private float scytheChargeSpeed = 9.5f;
    [SerializeField] private float scytheChargeRadius = 0.42f;
    [SerializeField] private float scytheChargeDamageMultiplier = 1.15f;
    [SerializeField] private float scytheChargeKnockback = 1.2f;
    [SerializeField] private float scytheChargeGroggyTime = 0.08f;
    [SerializeField] private Color chargeAttackFlashColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color chargeReadyColor = new Color(1f, 0.92f, 0.2f, 1f);

    public bool IsAttacking => isAttacking;
    public int ComboStep => comboStep;
    public int CurrentWeaponSlot => currentWeaponIndex + 1;
    public WeaponData CurrentWeapon => currentWeapon;

    private Animator animator;
    private PlayerController controller;
    private PlayerPermanentProgress permanentProgress;
    private PlayerParry parry;
    private PlayerHealth health;
    private Rigidbody2D rb;
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool isInComboRecovery = false;
    private bool isComboDelay = false;
    private bool bufferedInput = false;
    private bool hasExecutedHitThisAttack = false;
    private Coroutine resetRoutine;
    private Coroutine attackSafetyRoutine;
    private Coroutine comboDelayRoutine;
    private Coroutine lungeRoutine;
    private Coroutine focusChaseRoutine;
    private Coroutine chargeAttackRoutine;
    private Vector2 attackDirection = Vector2.down;
    private int currentWeaponIndex = 0;
    private LineRenderer rangePreview;
    private LineRenderer hitboxFlash;
    private SpriteRenderer hitboxFlashSprite;
    private float swapAttackBonusTimer = 0f;
    private float queuedAttackTimer = 0f;
    private float hitboxFlashTimer = 0f;
    private float focusTimer = 0f;
    private int focusHitCount = 0;
    private int attackSerial = 0;
    private bool focusChaseContactGuard = false;
    private bool focusFinalHitLanded = false;
    private bool attackHoldActive = false;
    private bool chargeReadyVisualActive = false;
    private float attackHoldStartedAt = 0f;
    private SpriteRenderer playerSprite;
    private Color playerSpriteOriginalColor = Color.white;
    private WeaponData[] lobbyWeaponPool;
    private MonoBehaviour focusTarget;

    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly List<MonoBehaviour> damagedTargets = new List<MonoBehaviour>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
        permanentProgress = GetComponent<PlayerPermanentProgress>();
        parry = GetComponent<PlayerParry>();
        health = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody2D>();
        playerSprite = GetComponent<SpriteRenderer>();
        clockOutput = GetComponent<ClockOutputSystem>();
        if (playerSprite != null)
            playerSpriteOriginalColor = playerSprite.color;

        if (weaponSlot1 == null)
            weaponSlot1 = currentWeapon;

        SetupLobbyWeaponPool();
        EquipWeapon(0, false);
        SetupRangePreview();
    }

    private void LateUpdate()
    {
        if (swapAttackBonusTimer > 0f)
            swapAttackBonusTimer -= Time.deltaTime;
        if (queuedAttackTimer > 0f)
            queuedAttackTimer -= Time.deltaTime;

        UpdateAttackHoldFallback();
        UpdateHitboxFlashTimer();
        UpdateFocusTimer();
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
        if (value.isPressed)
        {
            BeginAttackHold();
            return;
        }

        CompleteAttackHold();
    }

    public void OnChargeAttack(InputValue value)
    {
        if (value.isPressed)
        {
            BeginAttackHold();
            return;
        }

        CompleteAttackHold();
    }

    public void OnPrevious(InputValue value)
    {
        if (!value.isPressed) return;
        EquipNextWeapon();
    }

    public void OnNext(InputValue value)
    {
        if (!value.isPressed) return;
        EquipNextWeapon();
    }

    public void OnCrouch(InputValue value)
    {
        if (!value.isPressed) return;
        EquipNextWeapon();
    }

    public void OnWeaponSwap(InputValue value)
    {
        if (!value.isPressed) return;
        EquipNextWeapon();
    }

    private void BeginAttackHold()
    {
        if (attackHoldActive)
            return;

        if (!CanHoldAttackInput())
        {
            QueueAttackInput();
            return;
        }

        attackHoldActive = true;
        attackHoldStartedAt = Time.time;
        queuedAttackTimer = 0f;
        bufferedInput = false;
    }

    private void CompleteAttackHold()
    {
        if (!attackHoldActive)
            return;

        float heldTime = Time.time - attackHoldStartedAt;
        attackHoldActive = false;
        RestoreChargeReadyVisual();

        if (heldTime >= chargeHoldTime && TryStartChargeAttack())
            return;

        QueueAttackInput();
    }

    private void UpdateAttackHoldFallback()
    {
        if (!attackHoldActive)
            return;

        if (Time.time - attackHoldStartedAt >= chargeHoldTime)
            ApplyChargeReadyVisual();

        if (Time.time <= attackHoldStartedAt)
            return;

        if (!IsAnyAttackControlHeld())
            CompleteAttackHold();
    }

    private void ApplyChargeReadyVisual()
    {
        if (chargeReadyVisualActive || playerSprite == null)
            return;

        playerSpriteOriginalColor = playerSprite.color;
        playerSprite.color = chargeReadyColor;
        chargeReadyVisualActive = true;
    }

    private void RestoreChargeReadyVisual()
    {
        if (!chargeReadyVisualActive)
            return;

        if (playerSprite != null)
            playerSprite.color = playerSpriteOriginalColor;

        chargeReadyVisualActive = false;
    }

    private bool CanHoldAttackInput()
    {
        return currentWeapon != null
            && !isAttacking
            && !isInComboRecovery
            && !isComboDelay
            && chargeAttackRoutine == null;
    }

    private bool IsAnyAttackControlHeld()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;
        if (Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.numpadEnterKey.isPressed))
            return true;
        if (Keyboard.current != null && Keyboard.current.fKey.isPressed)
            return true;
        if (Gamepad.current != null && (Gamepad.current.buttonWest.isPressed || Gamepad.current.rightShoulder.isPressed))
            return true;

        return false;
    }

    private void QueueAttackInput()
    {
        if (isInComboRecovery)
            return;

        if (isComboDelay)
        {
            queuedAttackTimer = attackInputBufferTime;
            bufferedInput = true;
            return;
        }

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
        if (isInComboRecovery || isComboDelay) return;
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
        comboStep++;
        int currentMaxCombo = GetCurrentMaxCombo();
        if (comboStep > currentMaxCombo)
            comboStep = 1;

        animator.speed = GetCurrentAnimationSpeed();

        isAttacking = true;
        bufferedInput = false;
        hasExecutedHitThisAttack = false;
        int currentAttackSerial = ++attackSerial;

        if (ShouldChaseFocusTargetBeforeAttack())
        {
            if (focusChaseRoutine != null)
                StopCoroutine(focusChaseRoutine);
            focusChaseRoutine = StartCoroutine(FocusChaseThenBeginAttack(currentAttackSerial));
            return;
        }

        BeginAttackAnimation(currentAttackSerial);
    }

    private void BeginAttackAnimation(int currentAttackSerial)
    {
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

        attackHoldActive = false;
        RestoreChargeReadyVisual();
        currentWeapon = nextWeapon;
        currentWeaponIndex = slotIndex;
        animator.speed = 1f;

        if (resetCombo)
        {
            swapAttackBonusTimer = swapAttackBonusTime;
            ClearFocusTarget(false, true);
            ResetCombo(true);
        }
    }

    private void EquipNextWeapon()
    {
        if (isAttacking) return;

        int slotCount = 3;
        for (int i = 1; i <= slotCount; i++)
        {
            int nextIndex = (currentWeaponIndex + i) % slotCount;
            if (GetWeaponInSlot(nextIndex) == null)
                continue;

            EquipWeapon(nextIndex, true);
            return;
        }
    }

    private WeaponData GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex == 0) return weaponSlot1;
        if (slotIndex == 1) return weaponSlot2;
        if (slotIndex == 2) return weaponSlot3;
        return null;
    }

    public void ApplyLobbyWeaponSlots(int firstWeaponIndex, int secondWeaponIndex)
    {
        if (isAttacking) return;
        if (lobbyWeaponPool == null || lobbyWeaponPool.Length < 3) return;
        if (firstWeaponIndex == secondWeaponIndex) return;
        if (firstWeaponIndex < 0 || firstWeaponIndex >= lobbyWeaponPool.Length) return;
        if (secondWeaponIndex < 0 || secondWeaponIndex >= lobbyWeaponPool.Length) return;

        WeaponData firstWeapon = lobbyWeaponPool[firstWeaponIndex];
        WeaponData secondWeapon = lobbyWeaponPool[secondWeaponIndex];
        if (firstWeapon == null || secondWeapon == null) return;

        weaponSlot1 = firstWeapon;
        weaponSlot2 = secondWeapon;
        currentWeapon = weaponSlot1;
        currentWeaponIndex = 0;
        animator.speed = 1f;
        ResetCombo(true);
    }

    public void SetLobbyWeaponSlot(int loadoutSlot, int weaponIndex)
    {
        if (lobbyWeaponPool == null || lobbyWeaponPool.Length < 3) return;
        if (weaponIndex < 0 || weaponIndex >= lobbyWeaponPool.Length) return;
        WeaponData selectedWeapon = lobbyWeaponPool[weaponIndex];
        if (selectedWeapon == null) return;

        WeaponData otherSlotWeapon = loadoutSlot == 1 ? weaponSlot2 : weaponSlot1;
        if (otherSlotWeapon == selectedWeapon)
        {
            for (int i = 0; i < lobbyWeaponPool.Length; i++)
            {
                WeaponData replacement = lobbyWeaponPool[i];
                if (replacement == null || replacement == selectedWeapon) continue;
                otherSlotWeapon = replacement;
                break;
            }
        }

        if (loadoutSlot == 1)
        {
            weaponSlot1 = selectedWeapon;
            weaponSlot2 = otherSlotWeapon;
        }
        else if (loadoutSlot == 2)
        {
            weaponSlot1 = otherSlotWeapon;
            weaponSlot2 = selectedWeapon;
        }
        else
        {
            return;
        }

        currentWeapon = loadoutSlot == 1 ? weaponSlot1 : weaponSlot2;
        currentWeaponIndex = loadoutSlot - 1;
        animator.speed = 1f;
        ClearFocusTarget(false, true);
        ResetCombo(true);
    }

    public WeaponData GetLobbyWeaponCandidate(int weaponIndex)
    {
        if (lobbyWeaponPool == null) return null;
        if (weaponIndex < 0 || weaponIndex >= lobbyWeaponPool.Length) return null;
        return lobbyWeaponPool[weaponIndex];
    }

    public int GetLobbyWeaponSlotIndex(int loadoutSlot)
    {
        WeaponData equippedWeapon = loadoutSlot == 1 ? weaponSlot1 : weaponSlot2;
        return FindCandidateIndex(equippedWeapon, loadoutSlot == 1 ? 0 : 1);
    }

    private void SetupLobbyWeaponPool()
    {
        if (lobbyWeaponCandidate1 == null)
            lobbyWeaponCandidate1 = weaponSlot1;
        if (lobbyWeaponCandidate2 == null)
            lobbyWeaponCandidate2 = weaponSlot2;
        if (lobbyWeaponCandidate3 == null)
            lobbyWeaponCandidate3 = weaponSlot3;

        lobbyWeaponPool = new WeaponData[] { lobbyWeaponCandidate1, lobbyWeaponCandidate2, lobbyWeaponCandidate3 };
    }

    private int FindCandidateIndex(WeaponData weapon, int fallbackIndex)
    {
        if (lobbyWeaponPool == null) return fallbackIndex;

        for (int i = 0; i < lobbyWeaponPool.Length; i++)
        {
            if (lobbyWeaponPool[i] == weapon)
                return i;
        }

        return Mathf.Clamp(fallbackIndex, 0, lobbyWeaponPool.Length - 1);
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        ResetCombo(true);
    }

    private IEnumerator AttackSafetyTimer(int expectedAttackSerial)
    {
        float speed = GetCurrentAnimationSpeed();
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
        TryParryEnemyAttackInArea(hitCenter, radius);

        int hitCount = Physics2D.OverlapCircleNonAlloc(hitCenter, radius, hitBuffer, hitLayers);
        damagedTargets.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i];
            if (hitCollider == null) continue;
            if (!IsColliderInsideCurrentHitShape(hitCollider, hitCenter, radius)) continue;

            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                IDamageable damageable = behaviour as IDamageable;
                if (damageable == null) continue;
                if (damagedTargets.Contains(behaviour)) continue;

                Vector2 hitPoint = hitCollider.ClosestPoint(hitCenter);
                Vector2 hitDirection = GetHitDirection(behaviour);
                damageable.TakeDamage(GetCurrentAttackPower(), hitPoint, hitDirection * GetCurrentKnockbackMultiplier());
                ApplyShortGroggy(behaviour);
                damagedTargets.Add(behaviour);
                break;
            }
        }

        if (damagedTargets.Count > 0)
        {
            MonoBehaviour firstHitTarget = damagedTargets[0];
            if (IsExoskeletonFinalHit() || IsRapierFinalHit() || IsScytheFinalHit())
                focusFinalHitLanded = true;

            if (IsRoomEnemyDead(firstHitTarget) && !focusFinalHitLanded)
            {
                ClearFocusTarget(false, true);
            }
            else
            {
                UpdateFocusTarget(firstHitTarget);
                if (!IsCurrentFocusActive())
                    StartAttackLunge();
            }
        }
        else
        {
            ClearFocusTarget(false, true);
        }

        if (clockOutput != null && damagedTargets.Count > 0)
        {
            bool usedSwapBonus = swapAttackBonusTimer > 0f;
            clockOutput.GainFromAttackHit(damagedTargets.Count, usedSwapBonus);
            if (usedSwapBonus)
                swapAttackBonusTimer = 0f;
        }

        if (damagedTargets.Count > 0)
        {
            if (IsExoskeletonFinalHit())
                ShakeCamera(exoskeletonFinalShakeTime, exoskeletonFinalShakePower);
            else if (IsRapierFinalHit())
                ShakeCamera(rapierFinalShakeTime, rapierFinalShakePower);
            else if (IsScytheFinalHit())
                ShakeCamera(scytheFinalShakeTime, scytheFinalShakePower);
            else if (IsCurrentWeaponScythe())
                ShakeCamera(scytheEarlyShakeTime, scytheEarlyShakePower);
            else
                ShakeCameraOnHit();
        }
    }

    private bool TryStartChargeAttack()
    {
        if (currentWeapon == null || isAttacking || isInComboRecovery || isComboDelay || chargeAttackRoutine != null)
            return false;

        Vector2 direction = GetFacingDirection();
        ResetCombo(true);
        ClearFocusTarget(false, true);

        if (currentWeapon.watchSkillType == WatchSkillType.ParryCounter)
            chargeAttackRoutine = StartCoroutine(ExoskeletonChargeAttackRoutine(direction));
        else if (IsCurrentWeaponRapier())
            chargeAttackRoutine = StartCoroutine(RapierChargeAttackRoutine(direction));
        else if (IsCurrentWeaponScythe())
            chargeAttackRoutine = StartCoroutine(ScytheChargeAttackRoutine(direction));
        else
            return false;

        return true;
    }

    private IEnumerator ExoskeletonChargeAttackRoutine(Vector2 direction)
    {
        BeginChargeAttackState(direction);
        if (health != null)
            health.MakeInvincible(exoskeletonChargeDuration + 0.08f);

        damagedTargets.Clear();
        float distance = exoskeletonChargeDistance;
        float timer = 0f;

        while (timer < exoskeletonChargeDuration)
        {
            float stepTime = Time.fixedDeltaTime;
            timer += stepTime;

            if (rb != null && distance > 0f)
                rb.position += direction * (distance * stepTime / exoskeletonChargeDuration);

            Vector2 center = rb != null ? rb.position : (Vector2)transform.position;
            DamageTargetsInCircle(center, exoskeletonChargeRadius, GetBaseWeaponAttackPower() * exoskeletonChargeDamageMultiplier, direction * exoskeletonChargeKnockback, exoskeletonChargeGroggyTime, false);
            ShowCircleChargeFlash(center, exoskeletonChargeRadius);
            yield return new WaitForFixedUpdate();
        }

        FinishChargeAttack(damagedTargets.Count, exoskeletonFinalShakeTime, exoskeletonFinalShakePower);
    }

    private IEnumerator RapierChargeAttackRoutine(Vector2 direction)
    {
        BeginChargeAttackState(direction);
        int totalHits = 0;
        int slashCount = Mathf.Max(1, rapierChargeHitCount);

        for (int i = 0; i < slashCount; i++)
        {
            float tilt = i % 2 == 0 ? -rapierSlashTilt : rapierSlashTilt;
            Vector2 slashDirection = RotateVector(direction, tilt);
            totalHits += DamageTargetsInCone(
                GetAttackShapeOrigin(),
                slashDirection,
                rapierChargeRange,
                rapierChargeAngle,
                GetBaseWeaponAttackPower() * rapierChargeDamageMultiplier,
                direction * rapierChargeKnockback,
                rapierChargeGroggyTime,
                true);

            ShowConeChargeFlash(GetAttackShapeOrigin(), slashDirection, rapierChargeRange, rapierChargeAngle);
            ShakeCamera(0.035f, 0.018f);
            yield return new WaitForSeconds(rapierChargeHitInterval);
        }

        FinishChargeAttack(totalHits, rapierFinalShakeTime, rapierFinalShakePower);
    }

    private IEnumerator ScytheChargeAttackRoutine(Vector2 direction)
    {
        BeginChargeAttackState(direction);
        damagedTargets.Clear();

        GameObject slashObject = new GameObject("ScytheChargeSlash");
        LineRenderer slashRenderer = slashObject.AddComponent<LineRenderer>();
        SetupCircleRenderer(slashRenderer, chargeAttackFlashColor, hitboxFlashWidth, 40);
        slashRenderer.loop = false;
        slashRenderer.positionCount = 2;

        Vector2 start = GetAttackShapeOrigin() + direction * 0.35f;
        float traveled = 0f;
        float damage = GetBaseWeaponAttackPower() * scytheChargeDamageMultiplier;

        while (traveled < scytheChargeRange)
        {
            float moveDistance = scytheChargeSpeed * Time.fixedDeltaTime;
            traveled += moveDistance;
            Vector2 center = start + direction * traveled;
            Vector2 side = new Vector2(-direction.y, direction.x);

            slashRenderer.SetPosition(0, center - side * scytheChargeRadius);
            slashRenderer.SetPosition(1, center + side * scytheChargeRadius);

            DamageTargetsInCircle(center, scytheChargeRadius, damage, direction * scytheChargeKnockback, scytheChargeGroggyTime, false);
            yield return new WaitForFixedUpdate();
        }

        Destroy(slashObject);
        FinishChargeAttack(damagedTargets.Count, scytheFinalShakeTime, scytheFinalShakePower);
    }

    private void BeginChargeAttackState(Vector2 direction)
    {
        attackDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.down;
        isAttacking = true;
        isInComboRecovery = false;
        isComboDelay = false;
        bufferedInput = false;
        hasExecutedHitThisAttack = true;
        queuedAttackTimer = 0f;
        attackSerial++;

        if (controller != null)
            controller.RefreshAfterAttack();
    }

    private void FinishChargeAttack(int hitCount, float shakeTime, float shakePower)
    {
        if (clockOutput != null && hitCount > 0)
            clockOutput.GainFromAttackHit(hitCount, false);

        if (hitCount > 0)
            ShakeCamera(shakeTime, shakePower);

        chargeAttackRoutine = StartCoroutine(ChargeRecoveryRoutine());
    }

    private IEnumerator ChargeRecoveryRoutine()
    {
        yield return new WaitForSeconds(chargeRecoveryTime);
        chargeAttackRoutine = null;
        ResetCombo(true);
    }

    private int DamageTargetsInCircle(Vector2 center, float radius, float damage, Vector2 knockback, float groggyTime, bool allowRepeatHits)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, radius, hitBuffer, hitLayers);
        int damageCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i];
            if (hitCollider == null || hitCollider.transform == transform) continue;

            damageCount += DamageCollider(hitCollider, center, damage, knockback, groggyTime, allowRepeatHits);
        }

        return damageCount;
    }

    private int DamageTargetsInCone(Vector2 origin, Vector2 direction, float range, float angle, float damage, Vector2 knockback, float groggyTime, bool allowRepeatHits)
    {
        Vector2 center = origin + direction.normalized * (range * 0.5f);
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, range, hitBuffer, hitLayers);
        int damageCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i];
            if (hitCollider == null || hitCollider.transform == transform) continue;

            Vector2 point = hitCollider.ClosestPoint(origin);
            Vector2 toPoint = point - origin;
            if (toPoint.sqrMagnitude > range * range)
                continue;
            if (toPoint.sqrMagnitude > 0.001f && Vector2.Angle(direction, toPoint) > angle * 0.5f)
                continue;

            damageCount += DamageCollider(hitCollider, origin, damage, knockback, groggyTime, allowRepeatHits);
        }

        return damageCount;
    }

    private int DamageCollider(Collider2D hitCollider, Vector2 hitOrigin, float damage, Vector2 knockback, float groggyTime, bool allowRepeatHits)
    {
        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            IDamageable damageable = behaviour as IDamageable;
            if (damageable == null) continue;
            if (!allowRepeatHits && damagedTargets.Contains(behaviour)) continue;

            Vector2 hitPoint = hitCollider.ClosestPoint(hitOrigin);
            damageable.TakeDamage(damage, hitPoint, knockback);
            ApplyGroggy(behaviour, groggyTime);
            if (!damagedTargets.Contains(behaviour))
                damagedTargets.Add(behaviour);
            return 1;
        }

        return 0;
    }

    private void ApplyGroggy(MonoBehaviour hitBehaviour, float groggyTime)
    {
        IEnemyStatusReceiver statusReceiver = hitBehaviour as IEnemyStatusReceiver;
        if (statusReceiver == null || groggyTime <= 0f) return;

        statusReceiver.ApplyGroggy(groggyTime);
    }

    private void ShowCircleChargeFlash(Vector2 center, float radius)
    {
        if (!showHitboxFlash || hitboxFlash == null) return;

        DrawCircle(hitboxFlash, center, radius, chargeAttackFlashColor, hitboxFlashWidth);
        hitboxFlash.enabled = true;
        hitboxFlashTimer = hitboxFlashTime;
    }

    private void ShowConeChargeFlash(Vector2 origin, Vector2 direction, float range, float angle)
    {
        if (!showHitboxFlash || hitboxFlash == null) return;

        DrawChargeCone(hitboxFlash, origin, direction, range, angle, chargeAttackFlashColor, hitboxFlashWidth);
        hitboxFlash.enabled = true;
        hitboxFlashTimer = hitboxFlashTime;
    }

    private bool TryParryEnemyAttackInArea(Vector2 hitCenter, float attackRadius)
    {
        if (parry == null)
            return false;

        float parryRadius = Mathf.Max(attackRadius * parryHitboxMultiplier, minimumParryHitboxRadius);
        int parryHitCount = Physics2D.OverlapCircleNonAlloc(hitCenter, parryRadius, hitBuffer, hitLayers);

        for (int i = 0; i < parryHitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i];
            if (hitCollider == null) continue;

            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
            if (TryParryEnemyAttack(behaviours))
                return true;
        }

        return false;
    }

    private bool TryParryEnemyAttack(MonoBehaviour[] behaviours)
    {
        if (behaviours == null)
            return false;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (TryParryEnemyAttack(behaviours[i]))
                return true;
        }

        return false;
    }

    private bool TryParryEnemyAttack(MonoBehaviour behaviour)
    {
        if (parry == null || behaviour == null)
            return false;

        IParryableEnemyAttack parryableAttack = behaviour as IParryableEnemyAttack;
        if (parryableAttack == null)
            return false;

        Vector2 parryDirection = behaviour.transform.position - transform.position;
        if (parryDirection.sqrMagnitude <= 0.01f)
            parryDirection = attackDirection;

        return parry.TryParryAttack(parryableAttack, behaviour, parryDirection.normalized);
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
        else
        {
            isAttacking = false;
            StartComboDelay();
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
        isComboDelay = false;
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
        if (comboDelayRoutine != null)
        {
            StopCoroutine(comboDelayRoutine);
            comboDelayRoutine = null;
        }

        if (controller != null)
            controller.RefreshAfterAttack();

        yield return new WaitForSeconds(comboEndRecoveryTime);

        bool shouldBounceCameraBack = focusFinalHitLanded && (IsExoskeletonFinalHit() || IsRapierFinalHit());
        bool shouldSmoothCameraBack = focusFinalHitLanded && IsScytheFinalHit();
        ClearFocusTarget(shouldBounceCameraBack, !shouldBounceCameraBack && !shouldSmoothCameraBack);
        focusFinalHitLanded = false;
        ResetCombo(true);
    }

    private void ResetCombo(bool clearQueuedAttack)
    {
        comboStep = 0;
        attackHoldActive = false;
        RestoreChargeReadyVisual();
        isAttacking = false;
        isInComboRecovery = false;
        isComboDelay = false;
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
        if (comboDelayRoutine != null)
        {
            StopCoroutine(comboDelayRoutine);
            comboDelayRoutine = null;
        }
        if (lungeRoutine != null)
        {
            StopCoroutine(lungeRoutine);
            lungeRoutine = null;
        }
        if (focusChaseRoutine != null)
        {
            StopCoroutine(focusChaseRoutine);
            focusChaseRoutine = null;
        }
        if (clearQueuedAttack && focusTarget != null)
            ClearFocusTarget(false, true);

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

    private float GetCurrentAnimationSpeed()
    {
        if (currentWeapon == null || currentWeapon.attackSpeed <= 0f)
            return 1f;

        return currentWeapon.attackSpeed * currentWeapon.GetComboSpeedMultiplier(comboStep);
    }

    private float GetCurrentAttackPower()
    {
        float attackPower = currentWeapon != null ? currentWeapon.attackPower : 0f;
        if (currentWeapon != null)
            attackPower *= currentWeapon.GetComboDamageMultiplier(comboStep);
        if (permanentProgress != null)
            attackPower *= permanentProgress.AttackDamageMultiplier;

        return attackPower;
    }

    private float GetBaseWeaponAttackPower()
    {
        float attackPower = currentWeapon != null ? currentWeapon.attackPower : 0f;
        if (permanentProgress != null)
            attackPower *= permanentProgress.AttackDamageMultiplier;

        return attackPower;
    }

    private float GetCurrentKnockbackMultiplier()
    {
        return currentWeapon != null ? currentWeapon.GetComboKnockbackMultiplier(comboStep) : 1f;
    }

    private float GetCurrentGroggyTime()
    {
        return currentWeapon != null ? currentWeapon.GetComboGroggyTime(comboStep) : 0.06f;
    }

    private float GetCurrentComboDelay()
    {
        return currentWeapon != null ? currentWeapon.GetComboDelay(comboStep) : 0.12f;
    }

    private float GetCurrentLungeDistance()
    {
        return currentWeapon != null ? currentWeapon.GetComboLungeDistance(comboStep) : 0.08f;
    }

    private float GetCurrentLungeTime()
    {
        return currentWeapon != null ? currentWeapon.GetComboLungeTime(comboStep) : 0.08f;
    }

    private void StartComboDelay()
    {
        if (comboDelayRoutine != null)
            StopCoroutine(comboDelayRoutine);

        if (bufferedInput)
            queuedAttackTimer = Mathf.Max(queuedAttackTimer, GetCurrentComboDelay() + 0.15f);

        comboDelayRoutine = StartCoroutine(ComboDelayRoutine());
    }

    private IEnumerator ComboDelayRoutine()
    {
        isComboDelay = true;
        yield return new WaitForSeconds(GetCurrentComboDelay());

        isComboDelay = false;
        comboDelayRoutine = null;

        if (queuedAttackTimer > 0f)
        {
            queuedAttackTimer = 0f;
            StartNextCombo();
        }
        else
        {
            StartComboResetTimer();
        }
    }

    private void StartAttackLunge()
    {
        if (rb == null) return;

        float distance = GetCurrentLungeDistance();
        Vector2 direction = GetLungeDirection();
        distance = GetAdjustedLungeDistance(distance, direction);
        if (distance <= 0f) return;

        if (lungeRoutine != null)
            StopCoroutine(lungeRoutine);
        lungeRoutine = StartCoroutine(AttackLungeRoutine(direction, distance, GetCurrentLungeTime()));
    }

    private bool ShouldChaseFocusTargetBeforeAttack()
    {
        if (IsExoskeletonFocusActive() && comboStep > 1)
            return true;

        if (IsRapierFocusActive() && comboStep == 4)
            return true;

        return IsScytheFocusActive() && comboStep == 3;
    }

    private IEnumerator FocusChaseThenBeginAttack(int currentAttackSerial)
    {
        if (rb == null)
        {
            BeginAttackAnimation(currentAttackSerial);
            yield break;
        }

        float timer = 0f;
        focusChaseContactGuard = true;
        if (health != null)
            health.MakeInvincible(focusChaseInvincibleTime);

        ApplyFocusChaseCameraLead();

        while (IsCurrentFocusActive() && timer < focusChaseMaxTime)
        {
            if (currentAttackSerial != attackSerial)
            {
                focusChaseContactGuard = false;
                focusChaseRoutine = null;
                yield break;
            }

            Vector2 toTarget = focusTarget.transform.position - transform.position;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget <= focusStopDistance)
                break;

            Vector2 direction = toTarget.normalized;
            attackDirection = direction;
            float moveDistance = Mathf.Min(distanceToTarget - focusStopDistance, focusChaseSpeed * Time.fixedDeltaTime);
            rb.position += direction * moveDistance;

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        focusChaseRoutine = null;
        focusChaseContactGuard = false;
        focusFinalHitLanded = false;
        if (currentAttackSerial == attackSerial)
            BeginAttackAnimation(currentAttackSerial);
    }

    private IEnumerator AttackLungeRoutine(Vector2 lungeDirection, float distance, float duration)
    {
        float remainingDistance = distance;
        float timer = 0f;

        while (timer < duration)
        {
            float stepTime = Time.fixedDeltaTime;
            timer += stepTime;
            float moveDistance = distance * stepTime / duration;
            if (moveDistance > remainingDistance)
                moveDistance = remainingDistance;

            rb.position += lungeDirection * moveDistance;
            remainingDistance -= moveDistance;
            yield return new WaitForFixedUpdate();
        }

        lungeRoutine = null;
    }

    private Vector2 GetLungeDirection()
    {
        if (IsCurrentFocusActive())
        {
            Vector2 toTarget = focusTarget.transform.position - transform.position;
            if (toTarget.sqrMagnitude > 0.01f)
                return toTarget.normalized;
        }

        return attackDirection.sqrMagnitude > 0.01f ? attackDirection.normalized : Vector2.down;
    }

    private Vector2 GetHitDirection(MonoBehaviour hitTarget)
    {
        if (hitTarget != null)
        {
            Vector2 toTarget = hitTarget.transform.position - transform.position;
            if (toTarget.sqrMagnitude > 0.01f)
                return toTarget.normalized;
        }

        return attackDirection.sqrMagnitude > 0.01f ? attackDirection.normalized : Vector2.down;
    }

    private float GetAdjustedLungeDistance(float wantedDistance, Vector2 direction)
    {
        if (wantedDistance <= 0f)
            return 0f;

        if (IsCurrentFocusActive())
        {
            float distanceToTarget = Vector2.Distance(transform.position, focusTarget.transform.position);
            return Mathf.Clamp(distanceToTarget - focusStopDistance, 0f, wantedDistance);
        }

        return GetSafeLungeDistance(wantedDistance, direction);
    }

    private float GetSafeLungeDistance(float wantedDistance, Vector2 direction)
    {
        Vector2 origin = rb != null ? rb.position : (Vector2)transform.position;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, 0.12f, direction, wantedDistance + lungeStopDistanceFromEnemy, hitLayers);

        float safeDistance = wantedDistance;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform == transform) continue;
            if (!ColliderHasDamageable(hitCollider)) continue;

            safeDistance = Mathf.Min(safeDistance, hits[i].distance - lungeStopDistanceFromEnemy);
        }

        return Mathf.Max(0f, safeDistance);
    }

    private bool ColliderHasDamageable(Collider2D hitCollider)
    {
        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IDamageable)
                return true;
        }

        return false;
    }

    private void UpdateFocusTarget(MonoBehaviour hitTarget)
    {
        if (!CanCurrentWeaponUseFocusTarget())
            return;
        if (hitTarget == null)
            return;
        if (IsRoomEnemyDead(hitTarget))
            return;
        if (focusTarget != null && focusTarget != hitTarget && !IsFocusTargetDead())
        {
            focusTimer = focusKeepTime;
            return;
        }

        focusTarget = hitTarget;
        focusTimer = focusKeepTime;
        focusHitCount++;
        if (currentWeapon.watchSkillType == WatchSkillType.ParryCounter)
            ApplyFocusCameraZoom();
        else if (IsCurrentWeaponRapier() && (comboStep == 2 || comboStep == 3))
            ApplyRapierCameraZoom();
        else if (IsCurrentWeaponScythe() && comboStep < GetCurrentMaxCombo())
            ApplyScytheCameraZoom();
    }

    private void UpdateFocusTimer()
    {
        if (focusTarget == null)
            return;

        if (IsFocusTargetDead())
        {
            if (!focusFinalHitLanded)
                ClearFocusTarget(false, true);
            return;
        }

        focusTimer -= Time.deltaTime;
        if (focusTimer <= 0f)
            ClearFocusTarget(false);
    }

    private bool IsExoskeletonFocusActive()
    {
        return currentWeapon != null
            && currentWeapon.watchSkillType == WatchSkillType.ParryCounter
            && focusTarget != null
            && !IsFocusTargetDead();
    }

    private bool IsRapierFocusActive()
    {
        return currentWeapon != null
            && currentWeapon.watchSkillType == WatchSkillType.JustEvadeTimeStop
            && focusTarget != null
            && !IsFocusTargetDead();
    }

    private bool IsScytheFocusActive()
    {
        return IsCurrentWeaponScythe()
            && focusTarget != null
            && !IsFocusTargetDead();
    }

    private bool IsCurrentFocusActive()
    {
        return (IsExoskeletonFocusActive() || IsRapierFocusActive() || IsScytheFocusActive())
            && focusTarget != null
            && !IsFocusTargetDead();
    }

    private bool CanCurrentWeaponUseFocusTarget()
    {
        if (currentWeapon == null)
            return false;

        return currentWeapon.watchSkillType == WatchSkillType.ParryCounter
            || currentWeapon.watchSkillType == WatchSkillType.JustEvadeTimeStop
            || currentWeapon.watchSkillType == WatchSkillType.MarkAndBlink;
    }

    private bool IsFocusTargetDead()
    {
        if (focusTarget == null)
            return true;

        IRoomEnemy roomEnemy = focusTarget as IRoomEnemy;
        return roomEnemy != null && roomEnemy.IsDead;
    }

    private bool IsRoomEnemyDead(MonoBehaviour target)
    {
        IRoomEnemy roomEnemy = target as IRoomEnemy;
        return roomEnemy != null && roomEnemy.IsDead;
    }

    private void ClearFocusTarget(bool bounceCameraBack)
    {
        ClearFocusTarget(bounceCameraBack, false);
    }

    private void ClearFocusTarget(bool bounceCameraBack, bool snapCameraBack)
    {
        focusTarget = null;
        focusTimer = 0f;
        focusHitCount = 0;
        focusChaseContactGuard = false;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl != null)
            cameraControl.ClearFocusZoom(bounceCameraBack, snapCameraBack);
    }

    private void ApplyFocusCameraZoom()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        float zoomIn = Mathf.Min(focusCameraMaxZoomIn, focusHitCount * focusCameraZoomPerHit);
        cameraControl.SetFocusZoom(zoomIn);
    }

    private void ApplyRapierCameraZoom()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        float zoomIn = comboStep == 3 ? rapierCameraZoomStep3 : rapierCameraZoomStep2;
        cameraControl.SetFocusZoom(zoomIn);
    }

    private void ApplyScytheCameraZoom()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        float zoomIn = comboStep == 2 ? scytheCameraZoomStep2 : scytheCameraZoomStep1;
        cameraControl.SetFocusZoom(zoomIn);
    }

    private void ApplyFocusChaseCameraLead()
    {
        if (!ShouldLeadCameraForFocusChase()) return;
        if (focusTarget == null) return;

        Vector2 leadDirection = focusTarget.transform.position - transform.position;
        if (leadDirection.sqrMagnitude <= 0.01f)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        cameraControl.LeadToward(leadDirection, focusChaseCameraLeadDistance, focusChaseCameraLeadTime);
    }

    private bool ShouldLeadCameraForFocusChase()
    {
        return IsExoskeletonFinalHit() || IsRapierFinalHit() || IsScytheFinalHit();
    }

    public bool ShouldIgnoreContactDamageFrom(MonoBehaviour enemy)
    {
        return IsCurrentFocusActive() && enemy != null && enemy == focusTarget;
    }

    private bool IsExoskeletonFinalHit()
    {
        return currentWeapon != null
            && currentWeapon.watchSkillType == WatchSkillType.ParryCounter
            && comboStep >= GetCurrentMaxCombo();
    }

    private bool IsRapierFinalHit()
    {
        return IsCurrentWeaponRapier() && comboStep >= GetCurrentMaxCombo();
    }

    private bool IsScytheFinalHit()
    {
        return IsCurrentWeaponScythe() && comboStep >= GetCurrentMaxCombo();
    }

    private void ApplyShortGroggy(MonoBehaviour hitBehaviour)
    {
        IEnemyStatusReceiver statusReceiver = hitBehaviour as IEnemyStatusReceiver;
        if (statusReceiver == null) return;

        float groggyTime = GetCurrentGroggyTime();
        if (groggyTime > 0f)
            statusReceiver.ApplyGroggy(groggyTime);
    }

    private void SetupRangePreview()
    {
        LineRenderer legacyRenderer = GetComponent<LineRenderer>();
        if (legacyRenderer != null)
            legacyRenderer.enabled = false;

        rangePreview = CreateChildLineRenderer("AttackRangePreview");
        SetupCircleRenderer(rangePreview, rangePreviewColor, rangePreviewWidth, 20);
        rangePreview.enabled = false;

        hitboxFlash = CreateChildLineRenderer("AttackHitboxFlash");
        SetupCircleRenderer(hitboxFlash, hitboxFlashColor, hitboxFlashWidth, 25);
        hitboxFlash.enabled = false;

        EnsureHitboxFlashSprite();
    }

    private LineRenderer CreateChildLineRenderer(string objectName)
    {
        Transform existingChild = transform.Find(objectName);
        GameObject rendererObject = existingChild != null
            ? existingChild.gameObject
            : new GameObject(objectName);

        rendererObject.transform.SetParent(transform);
        rendererObject.transform.localPosition = Vector3.zero;

        LineRenderer renderer = rendererObject.GetComponent<LineRenderer>();
        if (renderer == null)
            renderer = rendererObject.AddComponent<LineRenderer>();

        return renderer;
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

        bool shouldShow = showRangePreview && currentWeapon != null && isAttacking && chargeAttackRoutine == null;
        rangePreview.enabled = shouldShow;
        if (!shouldShow) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);
        DrawCurrentHitShape(rangePreview, hitCenter, radius, rangePreviewColor, rangePreviewWidth);
    }

    private void ShowHitboxFlash(Vector2 hitCenter, float radius)
    {
        if (!showHitboxFlash) return;

        if (hitboxFlash != null)
        {
            DrawCurrentHitShape(hitboxFlash, hitCenter, radius, hitboxFlashColor, hitboxFlashWidth);
            hitboxFlash.enabled = true;
        }

        if (!IsCurrentWeaponRapier() && !IsCurrentWeaponScythe())
            ShowHitboxFlashSprite(hitCenter, radius);
        hitboxFlashTimer = hitboxFlashTime;
    }

    private void ShowHitboxFlashSprite(Vector2 hitCenter, float radius)
    {
        EnsureHitboxFlashSprite();
        if (hitboxFlashSprite == null) return;

        hitboxFlashSprite.transform.position = new Vector3(hitCenter.x, hitCenter.y, -1f);
        hitboxFlashSprite.transform.localScale = Vector3.one * radius * 2f;
        hitboxFlashSprite.enabled = true;
    }

    private void EnsureHitboxFlashSprite()
    {
        if (hitboxFlashSprite != null) return;

        GameObject flashObject = new GameObject("HitboxFlash");
        flashObject.transform.SetParent(transform);
        hitboxFlashSprite = flashObject.AddComponent<SpriteRenderer>();
        hitboxFlashSprite.sprite = CreateCircleSprite();
        hitboxFlashSprite.color = new Color(hitboxFlashColor.r, hitboxFlashColor.g, hitboxFlashColor.b, 0.75f);
        hitboxFlashSprite.sortingLayerName = "Default";
        hitboxFlashSprite.sortingOrder = 1000;
        hitboxFlashSprite.enabled = false;
    }

    private void UpdateHitboxFlashTimer()
    {
        if (hitboxFlashTimer <= 0f)
        {
            HideHitboxFlash();
            return;
        }

        hitboxFlashTimer -= Time.deltaTime;
        if (hitboxFlashTimer <= 0f)
            HideHitboxFlash();
    }

    private void HideHitboxFlash()
    {
        if (hitboxFlash != null)
            hitboxFlash.enabled = false;
        if (hitboxFlashSprite != null)
            hitboxFlashSprite.enabled = false;
    }

    private void DrawCircle(LineRenderer renderer, Vector2 center, float radius, Color color, float width)
    {
        int segmentCount = Mathf.Max(12, rangePreviewSegments);
        if (renderer.positionCount != segmentCount)
            renderer.positionCount = segmentCount;

        renderer.loop = true;
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

    private void DrawCurrentHitShape(LineRenderer renderer, Vector2 center, float radius, Color color, float width)
    {
        if (IsCurrentWeaponRapier())
        {
            if (comboStep == 1)
            {
                DrawRapierThrust(renderer, color, width);
                return;
            }

            float tilt = comboStep == 2 ? -rapierSlashTilt : comboStep == 3 ? rapierSlashTilt : 0f;
            float angle = comboStep == 4 ? 135f : rapierSlashAngle;
            DrawRapierArc(renderer, GetCurrentAttackRange(), angle, tilt, color, width);
            return;
        }

        if (IsCurrentWeaponScythe())
        {
            if (comboStep >= GetCurrentMaxCombo())
            {
                DrawCircle(renderer, center, radius, color, width);
                return;
            }

            float tilt = comboStep == 1 ? -scytheSlashTilt : scytheSlashTilt;
            DrawRapierArc(renderer, GetCurrentAttackRange(), scytheSlashAngle, tilt, color, width);
            return;
        }

        DrawCircle(renderer, center, radius, color, width);
    }

    private void DrawRapierArc(LineRenderer renderer, float range, float angle, float tilt, Color color, float width)
    {
        int segmentCount = Mathf.Max(10, rangePreviewSegments / 2);
        renderer.loop = false;
        renderer.positionCount = segmentCount + 2;
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.startColor = color;
        renderer.endColor = color;

        Vector2 origin = GetAttackShapeOrigin();
        renderer.SetPosition(0, origin);

        float halfAngle = angle * 0.5f;
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float currentAngle = -halfAngle + angle * t + tilt;
            Vector2 direction = RotateVector(GetFacingDirection(), currentAngle);
            renderer.SetPosition(i + 1, origin + direction * range);
        }
    }

    private void DrawChargeCone(LineRenderer renderer, Vector2 origin, Vector2 direction, float range, float angle, Color color, float width)
    {
        int segmentCount = Mathf.Max(10, rangePreviewSegments / 2);
        renderer.loop = false;
        renderer.positionCount = segmentCount + 2;
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.startColor = color;
        renderer.endColor = color;

        renderer.SetPosition(0, origin);
        float halfAngle = angle * 0.5f;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float currentAngle = -halfAngle + angle * t;
            Vector2 pointDirection = RotateVector(direction, currentAngle);
            renderer.SetPosition(i + 1, origin + pointDirection * range);
        }
    }

    private void DrawRapierThrust(LineRenderer renderer, Color color, float width)
    {
        renderer.loop = false;
        renderer.positionCount = 5;
        renderer.startWidth = width;
        renderer.endWidth = width;
        renderer.startColor = color;
        renderer.endColor = color;

        Vector2 origin = GetAttackShapeOrigin();
        Vector2 forward = GetFacingDirection();
        Vector2 side = new Vector2(-forward.y, forward.x);
        float length = GetCurrentAttackRange();
        float halfWidth = rapierThrustWidth * 0.5f;

        Vector2 p0 = origin + side * halfWidth;
        Vector2 p1 = origin + forward * length + side * halfWidth;
        Vector2 p2 = origin + forward * length - side * halfWidth;
        Vector2 p3 = origin - side * halfWidth;

        renderer.SetPosition(0, p0);
        renderer.SetPosition(1, p1);
        renderer.SetPosition(2, p2);
        renderer.SetPosition(3, p3);
        renderer.SetPosition(4, p0);
    }

    private void GetCurrentHitArea(out Vector2 hitCenter, out float radius)
    {
        float range = GetCurrentAttackRange();
        radius = Mathf.Max(range * 0.5f, minimumHitRadius);
        if (IsCurrentWeaponRapier() || IsCurrentWeaponScythe())
            radius = Mathf.Max(range, minimumHitRadius);

        if (IsCurrentWeaponRapier() || IsCurrentWeaponScythe())
        {
            hitCenter = GetAttackShapeOrigin() + GetFacingDirection() * (range * 0.5f);
            return;
        }

        hitCenter = attackOrigin != null
            ? (Vector2)attackOrigin.position
            : (Vector2)transform.position + attackDirection * radius;
    }

    private float GetCurrentAttackRange()
    {
        return currentWeapon != null
            ? Mathf.Max(currentWeapon.attackRange * currentWeapon.GetComboRangeMultiplier(comboStep), minimumHitRadius)
            : minimumHitRadius;
    }

    private bool IsColliderInsideCurrentHitShape(Collider2D hitCollider, Vector2 hitCenter, float radius)
    {
        if (!IsCurrentWeaponRapier() && !IsCurrentWeaponScythe())
            return true;

        Vector2 origin = GetAttackShapeOrigin();
        Vector2 point = hitCollider.ClosestPoint(hitCenter);
        Vector2 toPoint = point - origin;
        float distance = toPoint.magnitude;
        float range = GetCurrentAttackRange();

        if (distance <= 0.001f)
            return true;

        if (IsCurrentWeaponScythe())
        {
            if (comboStep >= GetCurrentMaxCombo())
                return distance <= range;

            if (distance > range)
                return false;

            float scytheTilt = comboStep == 1 ? -scytheSlashTilt : scytheSlashTilt;
            Vector2 scytheSlashCenter = RotateVector(GetFacingDirection(), scytheTilt);
            return Vector2.Angle(scytheSlashCenter, toPoint) <= scytheSlashAngle * 0.5f;
        }

        if (comboStep == 1)
        {
            Vector2 forward = GetFacingDirection();
            float forwardDistance = Vector2.Dot(toPoint, forward);
            if (forwardDistance < 0f || forwardDistance > range)
                return false;

            Vector2 side = new Vector2(-forward.y, forward.x);
            float sideDistance = Mathf.Abs(Vector2.Dot(toPoint, side));
            return sideDistance <= rapierThrustWidth;
        }

        if (distance > range)
            return false;

        float tilt = comboStep == 2 ? -rapierSlashTilt : comboStep == 3 ? rapierSlashTilt : 0f;
        float angle = comboStep == 4 ? 135f : rapierSlashAngle;
        Vector2 slashCenter = RotateVector(GetFacingDirection(), tilt);
        return Vector2.Angle(slashCenter, toPoint) <= angle * 0.5f;
    }

    private bool IsCurrentWeaponRapier()
    {
        return currentWeapon != null && currentWeapon.watchSkillType == WatchSkillType.JustEvadeTimeStop;
    }

    private bool IsCurrentWeaponScythe()
    {
        return currentWeapon != null && currentWeapon.watchSkillType == WatchSkillType.MarkAndBlink;
    }

    private Vector2 GetAttackShapeOrigin()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    private Vector2 GetFacingDirection()
    {
        return attackDirection.sqrMagnitude > 0.01f ? attackDirection.normalized : Vector2.down;
    }

    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos).normalized;
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
                    float alpha = distance >= outlineRadius ? 1f : 0.65f;
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
        ShakeCamera(hitShakeTime, hitShakePower);
    }

    private void ShakeCamera(float duration, float power)
    {
        if (!shakeCameraOnHit) return;
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake shake = mainCamera.GetComponent<SimpleCameraShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        shake.Shake(duration, power);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugHitArea) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, radius);
    }
}
