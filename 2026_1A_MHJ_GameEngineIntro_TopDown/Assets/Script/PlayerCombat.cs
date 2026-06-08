using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("장착 무기")]
    public WeaponData currentWeapon;

    [Header("무기 스왑")]
    public WeaponData weaponSlot1;
    public WeaponData weaponSlot2;

    [Header("콤보 설정")]
    [Tooltip("최대 콤보 단계 (1~maxCombo 까지 순환)")]
    public int maxCombo = 3;
    [Tooltip("다음 입력을 받아주는 유예 시간(초). 이 시간이 지나면 콤보 초기화")]
    public float comboResetTime = 0.8f;

    [Header("Animator 파라미터 이름")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string comboStepParam = "ComboStep";

    [Header("타격 판정")]
    [Tooltip("비워두면 플레이어 앞쪽으로 무기 사거리만큼 자동 판정합니다.")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float minimumHitRadius = 0.15f;
    [SerializeField] private bool showDebugHitArea = true;

    [Header("범위 표시")]
    [SerializeField] private bool showRangePreview = true;
    [SerializeField] private Color rangePreviewColor = new Color(1f, 0.25f, 0.15f, 0.55f);
    [SerializeField] private float rangePreviewWidth = 0.03f;
    [SerializeField] private int rangePreviewSegments = 48;

    // 외부(PlayerController)에서 이동 잠금에 사용
    public bool IsAttacking => isAttacking;
    public int ComboStep => comboStep;
    public int CurrentWeaponSlot => currentWeaponIndex + 1;

    private Animator animator;
    private PlayerController controller;   // 공격 종료 후 이동 복귀 알림용
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool bufferedInput = false;   // 공격 중에 들어온 다음 입력 예약
    private Coroutine resetRoutine;
    private Vector2 attackDirection = Vector2.down;
    private int currentWeaponIndex = 0;
    private LineRenderer rangePreview;

    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly List<MonoBehaviour> damagedTargets = new List<MonoBehaviour>();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();

        if (weaponSlot1 == null)
            weaponSlot1 = currentWeapon;

        EquipWeapon(0, false);
        SetupRangePreview();
    }

    private void LateUpdate()
    {
        UpdateRangePreview();
    }

    // Input System: Move 액션에 함께 연결되어 마지막 입력 방향을 공격 방향으로 사용한다.
    public void OnMove(InputValue value)
    {
        Vector2 moveInput = value.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
            attackDirection = moveInput.normalized;
    }

    // Input System: Attack 액션에 연결 (PlayerInput - Send Messages 방식 기준)
    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TryAttack();
    }

    // Input System: Previous 액션에 연결. 기본 키 설정은 숫자 1.
    public void OnPrevious(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(0, true);
    }

    // Input System: Next 액션에 연결. 기본 키 설정은 숫자 2.
    public void OnNext(InputValue value)
    {
        if (!value.isPressed) return;
        EquipWeapon(1, true);
    }

    private void TryAttack()
    {
        // 공격 속도 보정 (무기가 있으면 attackSpeed를 애니메이터 speed에 반영)
        if (currentWeapon != null && currentWeapon.attackSpeed > 0f)
            animator.speed = currentWeapon.attackSpeed;

        if (isAttacking)
        {
            // 이미 모션 중이면 다음 콤보 입력을 버퍼링만 해둔다.
            bufferedInput = true;
            return;
        }

        StartNextCombo();
    }

    private void StartNextCombo()
    {
        comboStep++;
        if (comboStep > maxCombo)
            comboStep = 1;

        isAttacking = true;
        bufferedInput = false;

        animator.SetInteger(comboStepParam, comboStep);
        animator.SetTrigger(attackTrigger);

        // 유예 타이머 재시작
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(ComboResetTimer());
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
            ResetCombo();
    }

    private WeaponData GetWeaponInSlot(int slotIndex)
    {
        if (slotIndex == 0) return weaponSlot1;
        if (slotIndex == 1) return weaponSlot2;
        return null;
    }

    private IEnumerator ComboResetTimer()
    {
        yield return new WaitForSeconds(comboResetTime);
        // 유예 시간 안에 다음 입력이 없었으면 콤보 종료
        ResetCombo();
    }

    /// <summary>
    /// Animation Event: 타격 프레임에서 호출. 실제 데미지/히트박스 판정을 여기에 넣는다.
    /// </summary>
    public void ExecuteAttackHit()
    {
        if (currentWeapon == null) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);

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
    }

    /// <summary>
    /// Animation Event: 현재 공격 모션이 끝나는 마지막 프레임에서 호출.
    /// 버퍼된 입력이 있으면 다음 콤보로 이어지고, 없으면 콤보 종료.
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;

        if (bufferedInput)
        {
            StartNextCombo();
        }
        else
        {
            // 모션은 끝났지만 유예 시간 동안 콤보를 유지할지 결정.
            // 여기서는 모션 종료 시점부터 유예 타이머가 계속 돌도록 둔다.
            // (즉시 끊고 싶으면 아래 ResetCombo() 호출로 교체)
        }
    }

    private void ResetCombo()
    {
        comboStep = 0;
        isAttacking = false;
        bufferedInput = false;
        animator.SetInteger(comboStepParam, 0);
        animator.speed = 1f;
        resetRoutine = null;

        // 공격이 끝났으니 이동/idle 스프라이트 상태를 다시 맞춰준다.
        if (controller != null)
            controller.RefreshAfterAttack();
    }

    private void SetupRangePreview()
    {
        rangePreview = GetComponent<LineRenderer>();
        if (rangePreview == null)
            rangePreview = gameObject.AddComponent<LineRenderer>();

        rangePreview.useWorldSpace = true;
        rangePreview.loop = true;
        rangePreview.positionCount = Mathf.Max(12, rangePreviewSegments);
        rangePreview.startWidth = rangePreviewWidth;
        rangePreview.endWidth = rangePreviewWidth;
        rangePreview.startColor = rangePreviewColor;
        rangePreview.endColor = rangePreviewColor;
        rangePreview.material = new Material(Shader.Find("Sprites/Default"));
        rangePreview.sortingOrder = 20;
    }

    private void UpdateRangePreview()
    {
        if (rangePreview == null) return;

        bool shouldShow = showRangePreview && currentWeapon != null;
        rangePreview.enabled = shouldShow;
        if (!shouldShow) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);

        int segmentCount = Mathf.Max(12, rangePreviewSegments);
        if (rangePreview.positionCount != segmentCount)
            rangePreview.positionCount = segmentCount;

        rangePreview.startWidth = rangePreviewWidth;
        rangePreview.endWidth = rangePreviewWidth;
        rangePreview.startColor = rangePreviewColor;
        rangePreview.endColor = rangePreviewColor;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (float)i / segmentCount * Mathf.PI * 2f;
            Vector3 point = new Vector3(
                hitCenter.x + Mathf.Cos(angle) * radius,
                hitCenter.y + Mathf.Sin(angle) * radius,
                transform.position.z);
            rangePreview.SetPosition(i, point);
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

    private void OnDrawGizmosSelected()
    {
        if (!showDebugHitArea) return;

        GetCurrentHitArea(out Vector2 hitCenter, out float radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitCenter, radius);
    }
}
