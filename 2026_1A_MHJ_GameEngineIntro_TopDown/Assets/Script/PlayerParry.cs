using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerParry : MonoBehaviour
{
    [SerializeField] private float parryCheckRange = 0.85f;
    [SerializeField] private float counterReadyTime = 1f;
    [SerializeField] private float successSlowScale = 0.35f;
    [SerializeField] private float successSlowTime = 0.18f;

    public bool HasCounterReady => counterReadyTimer > 0f;

    private ClockOutputSystem clockOutput;
    private float counterReadyTimer;

    private void Awake()
    {
        clockOutput = GetComponent<ClockOutputSystem>();
    }

    private void Update()
    {
        if (counterReadyTimer > 0f)
            counterReadyTimer -= Time.deltaTime;
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;

        if (IsEnemyAttackNear())
        {
            counterReadyTimer = counterReadyTime;
            if (clockOutput != null)
                clockOutput.GainFromParry();
            StartCoroutine(SlowRoutine());
            Debug.Log("Parry Success.", this);
        }
        else
        {
            Debug.Log("Parry Miss.", this);
        }
    }

    public bool ConsumeCounterReady()
    {
        if (counterReadyTimer <= 0f)
            return false;

        counterReadyTimer = 0f;
        return true;
    }

    private bool IsEnemyAttackNear()
    {
        EnemyDummy[] enemies = FindObjectsByType<EnemyDummy>(FindObjectsSortMode.None);
        Vector2 playerPosition = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || enemies[i].IsDead || !enemies[i].IsAttackActive)
                continue;

            float range = Mathf.Max(parryCheckRange, enemies[i].AttackRange);
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
