using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerParry : MonoBehaviour
{
    [SerializeField] private float counterReadyTime = 1f;
    [SerializeField] private float successSlowScale = 0.35f;
    [SerializeField] private float successSlowTime = 0.7f;
    [SerializeField] private float successInvincibleTime = 0.22f;
    [SerializeField] private float successShakeTime = 0.09f;
    [SerializeField] private float successShakePower = 0.055f;
    [SerializeField] private float successToneTime = 0.7f;

    public bool HasCounterReady => counterReadyTimer > 0f;
    public MonoBehaviour LastParriedTarget => lastParriedTarget;

    private ClockOutputSystem clockOutput;
    private PlayerHealth health;
    private CombatToneFeedback toneFeedback;
    private float counterReadyTimer;
    private IParryableEnemyAttack lastParriedAttack;
    private MonoBehaviour lastParriedTarget;

    private void Awake()
    {
        clockOutput = GetComponent<ClockOutputSystem>();
        health = GetComponent<PlayerHealth>();
        toneFeedback = GetComponent<CombatToneFeedback>();
        if (toneFeedback == null)
            toneFeedback = gameObject.AddComponent<CombatToneFeedback>();
    }

    private void Update()
    {
        if (counterReadyTimer > 0f)
            counterReadyTimer -= Time.deltaTime;
        if (lastParriedAttack != null && !lastParriedAttack.IsParryableAttackActive)
            lastParriedAttack = null;
    }

    public void OnInteract(InputValue value)
    {
        // Parry is triggered by the player's attack hitbox, not by pressing E.
    }

    public bool TryParryAttack(IParryableEnemyAttack enemyAttack, MonoBehaviour enemyBehaviour, Vector2 parryDirection)
    {
        if (enemyAttack == null) return false;
        if (!enemyAttack.IsParryableAttackActive) return false;
        if (enemyAttack == lastParriedAttack) return false;

        lastParriedAttack = enemyAttack;
        lastParriedTarget = enemyBehaviour;
        counterReadyTimer = counterReadyTime;
        enemyAttack.OnParried(parryDirection);

        if (clockOutput != null)
            clockOutput.GainFromParry();
        if (health != null)
            health.MakeInvincible(successInvincibleTime);

        ShakeCameraOnSuccess();
        if (toneFeedback != null)
            toneFeedback.Play(successToneTime);
        StartCoroutine(SlowRoutine());
        Debug.Log("Attack Parry Success.", this);
        return true;
    }

    public bool ConsumeCounterReady()
    {
        if (counterReadyTimer <= 0f)
            return false;

        counterReadyTimer = 0f;
        return true;
    }

    private void ShakeCameraOnSuccess()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake shake = mainCamera.GetComponent<SimpleCameraShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        shake.Shake(successShakeTime, successShakePower);
    }

    private IEnumerator SlowRoutine()
    {
        float previousScale = Time.timeScale;
        Time.timeScale = successSlowScale;
        yield return new WaitForSecondsRealtime(successSlowTime);
        if (Mathf.Approximately(Time.timeScale, successSlowScale))
            Time.timeScale = previousScale;
    }
}
