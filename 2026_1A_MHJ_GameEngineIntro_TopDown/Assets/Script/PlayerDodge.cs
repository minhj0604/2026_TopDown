using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] private float dodgeDistance = 0.8f;
    [SerializeField] private float dodgeDuration = 0.15f;
    [SerializeField] private float dodgeInvincibleExtraTime = 0.08f;
    [SerializeField] private float justDodgeValidTime = 1f;
    [SerializeField] private float justDodgeCheckRange = 0.9f;
    [SerializeField] private float successSlowScale = 0.35f;
    [SerializeField] private float successSlowTime = 0.7f;
    [SerializeField] private float normalDodgeSlowScale = 0.62f;
    [SerializeField] private float normalDodgeSlowTime = 0.7f;
    [SerializeField] private float normalDodgeToneTime = 0.7f;
    [SerializeField] private float dodgeProjectileSuccessRange = 0.32f;
    [SerializeField] private float dodgeEnemySuccessRange = 0.48f;

    public bool IsDodging => isDodging;
    public bool HasJustDodge => justDodgeTimer > 0f;
    public event Action JustDodged;

    private Rigidbody2D rb;
    private PlayerController controller;
    private ClockOutputSystem clockOutput;
    private PlayerHealth health;
    private CombatToneFeedback toneFeedback;
    private Vector2 lastDirection = Vector2.down;
    private bool isDodging;
    private float justDodgeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        clockOutput = GetComponent<ClockOutputSystem>();
        health = GetComponent<PlayerHealth>();
        toneFeedback = GetComponent<CombatToneFeedback>();
        if (toneFeedback == null)
            toneFeedback = gameObject.AddComponent<CombatToneFeedback>();
    }

    private void Update()
    {
        if (justDodgeTimer > 0f)
            justDodgeTimer -= Time.deltaTime;
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        if (input.sqrMagnitude > 0.01f)
            lastDirection = input.normalized;
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        if (isDodging) return;

        StartCoroutine(DodgeRoutine());
    }

    public bool ConsumeJustDodge()
    {
        if (justDodgeTimer <= 0f)
            return false;

        justDodgeTimer = 0f;
        return true;
    }

    private IEnumerator DodgeRoutine()
    {
        isDodging = true;
        if (health != null)
            health.MakeInvincible(dodgeDuration + dodgeInvincibleExtraTime);

        bool isJustDodge = IsEnemyAttackNear();
        if (isJustDodge)
        {
            justDodgeTimer = justDodgeValidTime;
            if (clockOutput != null)
                clockOutput.GainFromDodge();
            JustDodged?.Invoke();
            GameTimeScaleController.RequestSlowMotion(successSlowScale, successSlowTime);
            Debug.Log("Just Dodge.", this);
        }

        if (controller != null)
            controller.enabled = false;

        Vector2 start = rb.position;
        Vector2 target = start + lastDirection * dodgeDistance;
        float timer = 0f;
        bool normalDodgeFeedbackPlayed = false;

        while (timer < dodgeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dodgeDuration);
            rb.MovePosition(Vector2.Lerp(start, target, t));

            if (!isJustDodge && !normalDodgeFeedbackPlayed && IsDodgeHazardOverlapping())
            {
                PlayNormalDodgeSuccessFeedback();
                normalDodgeFeedbackPlayed = true;
            }

            yield return null;
        }

        rb.MovePosition(target);

        if (controller != null)
            controller.enabled = true;

        isDodging = false;
    }

    private void PlayNormalDodgeSuccessFeedback()
    {
        if (toneFeedback != null)
            toneFeedback.Play(normalDodgeToneTime);
        GameTimeScaleController.RequestSlowMotion(normalDodgeSlowScale, normalDodgeSlowTime);
        Debug.Log("Dodge Success.", this);
    }

    private bool IsEnemyAttackNear()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        Vector2 playerPosition = transform.position;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            IParryableEnemyAttack enemyAttack = behaviour as IParryableEnemyAttack;
            if (enemyAttack == null || !enemyAttack.IsParryableAttackActive)
                continue;

            IRoomEnemy roomEnemy = behaviour as IRoomEnemy;
            if (roomEnemy != null && roomEnemy.IsDead)
                continue;

            IDodgeableEnemyAttack dodgeableAttack = behaviour as IDodgeableEnemyAttack;
            if (dodgeableAttack != null && dodgeableAttack.IsDodgeableAttackActiveFor(playerPosition))
                return true;

            float sqrDistance = ((Vector2)behaviour.transform.position - playerPosition).sqrMagnitude;
            float sqrRange = justDodgeCheckRange * justDodgeCheckRange;
            if (sqrDistance <= sqrRange)
                return true;
        }

        return false;
    }

    private bool IsDodgeHazardOverlapping()
    {
        Vector2 playerPosition = transform.position;

        EnemyProjectile[] projectiles = FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] == null)
                continue;

            if (IsInRange(playerPosition, projectiles[i].transform.position, dodgeProjectileSuccessRange))
                return true;
        }

        ExplodingEnemyProjectile[] explodingProjectiles = FindObjectsByType<ExplodingEnemyProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < explodingProjectiles.Length; i++)
        {
            if (explodingProjectiles[i] == null)
                continue;

            if (IsInRange(playerPosition, explodingProjectiles[i].transform.position, dodgeProjectileSuccessRange))
                return true;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            IRoomEnemy roomEnemy = behaviour as IRoomEnemy;
            if (roomEnemy == null || roomEnemy.IsDead)
                continue;

            if (IsInRange(playerPosition, behaviour.transform.position, dodgeEnemySuccessRange))
                return true;
        }

        return false;
    }

    private bool IsInRange(Vector2 origin, Vector2 target, float range)
    {
        return (target - origin).sqrMagnitude <= range * range;
    }

}
