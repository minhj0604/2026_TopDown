using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] private float dodgeDistance = 0.8f;
    [SerializeField] private float dodgeDuration = 0.15f;
    [SerializeField] private float justDodgeValidTime = 1f;
    [SerializeField] private float justDodgeCheckRange = 0.9f;
    [SerializeField] private float successSlowScale = 0.35f;
    [SerializeField] private float successSlowTime = 0.18f;

    public bool IsDodging => isDodging;

    private Rigidbody2D rb;
    private PlayerController controller;
    private ClockOutputSystem clockOutput;
    private PlayerHealth health;
    private Vector2 lastDirection = Vector2.down;
    private bool isDodging;
    private float justDodgeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        clockOutput = GetComponent<ClockOutputSystem>();
        health = GetComponent<PlayerHealth>();
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
            health.MakeInvincible(dodgeDuration);

        bool isJustDodge = IsEnemyAttackNear();
        if (isJustDodge)
        {
            justDodgeTimer = justDodgeValidTime;
            if (clockOutput != null)
                clockOutput.GainFromDodge();
            StartCoroutine(SlowRoutine());
            Debug.Log("Just Dodge.", this);
        }

        if (controller != null)
            controller.enabled = false;

        Vector2 start = rb.position;
        Vector2 target = start + lastDirection * dodgeDistance;
        float timer = 0f;

        while (timer < dodgeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dodgeDuration);
            rb.MovePosition(Vector2.Lerp(start, target, t));
            yield return null;
        }

        rb.MovePosition(target);

        if (controller != null)
            controller.enabled = true;

        isDodging = false;
    }

    private bool IsEnemyAttackNear()
    {
        EnemyDummy[] enemies = FindObjectsByType<EnemyDummy>(FindObjectsSortMode.None);
        Vector2 playerPosition = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || enemies[i].IsDead || !enemies[i].IsAttackActive)
                continue;

            float range = Mathf.Max(justDodgeCheckRange, enemies[i].AttackRange);
            float distance = Vector2.Distance(playerPosition, enemies[i].transform.position);
            if (distance <= range)
                return true;
        }

        return false;
    }

    private IEnumerator SlowRoutine()
    {
        float previousScale = Time.timeScale;
        Time.timeScale = successSlowScale;
        yield return new WaitForSecondsRealtime(successSlowTime);
        Time.timeScale = previousScale;
    }
}
