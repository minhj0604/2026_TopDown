using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("장착 무기")]
    public WeaponData currentWeapon;

    [Header("콤보 설정")]
    [Tooltip("최대 콤보 단계 (1~maxCombo 까지 순환)")]
    public int maxCombo = 3;
    [Tooltip("다음 입력을 받아주는 유예 시간(초). 이 시간이 지나면 콤보 초기화")]
    public float comboResetTime = 0.8f;

    [Header("Animator 파라미터 이름")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string comboStepParam = "ComboStep";

    // 외부(PlayerController)에서 이동 잠금에 사용
    public bool IsAttacking => isAttacking;
    public int ComboStep => comboStep;

    private Animator animator;
    private PlayerController controller;   // 공격 종료 후 이동 복귀 알림용
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool bufferedInput = false;   // 공격 중에 들어온 다음 입력 예약
    private Coroutine resetRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<PlayerController>();
    }

    // Input System: Attack 액션에 연결 (PlayerInput - Send Messages 방식 기준)
    public void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        TryAttack();
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

        // TODO: 무기 사거리/공격력 기반 히트 판정
        // 예) Physics2D.OverlapCircle / OverlapBox 로 적 탐지 후 currentWeapon.attackPower 적용
        // float power = currentWeapon.attackPower;
        // float range = currentWeapon.attackRange;
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
}
