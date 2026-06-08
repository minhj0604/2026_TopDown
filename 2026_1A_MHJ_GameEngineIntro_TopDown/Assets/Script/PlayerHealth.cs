using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hurtInvincibleTime = 0.45f;
    [SerializeField] private bool showDebugUI = true;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsInvincible => invincibleTimer > 0f;

    private float currentHealth;
    private float invincibleTimer;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvincible) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        MakeInvincible(hurtInvincibleTime);
        Debug.Log($"Player hit: -{damage} HP ({currentHealth}/{maxHealth})", this);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invincibleTimer = 0f;
    }

    public void MakeInvincible(float duration)
    {
        invincibleTimer = Mathf.Max(invincibleTimer, duration);
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(300f, 20f, 180f, 80f), GUI.skin.box);
        GUILayout.Label($"Player HP: {currentHealth:0}/{maxHealth:0}");
        GUILayout.Label(IsInvincible ? "Invincible" : "Vulnerable");
        if (GUILayout.Button("Reset HP"))
            ResetHealth();
        GUILayout.EndArea();
    }
}
