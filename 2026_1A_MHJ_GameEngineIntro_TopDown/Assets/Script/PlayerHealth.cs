using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hurtInvincibleTime = 0.45f;
    [SerializeField] private float hurtKnockbackDistance = 0.18f;
    [SerializeField] private float deathReturnDelay = 0.6f;
    [SerializeField] private Color hurtColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private bool showDebugUI = true;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsInvincible => invincibleTimer > 0f;
    public bool IsFullHealth => currentHealth >= maxHealth;

    private float currentHealth;
    private float invincibleTimer;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color normalColor = Color.white;
    private bool isReturningToLobby;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer != null)
            normalColor = spriteRenderer.color;

        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f && spriteRenderer != null)
                spriteRenderer.color = normalColor;
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (IsDead || IsInvincible) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        ClockOutputSystem clockOutput = GetComponent<ClockOutputSystem>();
        if (clockOutput != null)
            clockOutput.BreakStyleChain();
        MakeInvincible(hurtInvincibleTime);
        ApplyHitReaction(hitDirection);
        Debug.Log($"Player hit: -{damage} HP ({currentHealth}/{maxHealth})", this);

        if (IsDead && !isReturningToLobby)
            StartCoroutine(ReturnToLobbyAfterDeath());
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public bool TryHeal(float amount)
    {
        if (IsDead || IsFullHealth) return false;
        Heal(amount);
        return true;
    }

    public void IncreaseMaxHealth(float amount)
    {
        if (amount <= 0f) return;
        maxHealth += amount;
        currentHealth = maxHealth;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        invincibleTimer = 0f;
        isReturningToLobby = false;
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    public void MakeInvincible(float duration)
    {
        invincibleTimer = Mathf.Max(invincibleTimer, duration);
    }

    private void ApplyHitReaction(Vector2 hitDirection)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = hurtColor;

        if (rb == null || hitDirection.sqrMagnitude <= 0.01f)
            return;

        rb.MovePosition(rb.position + hitDirection.normalized * hurtKnockbackDistance);
    }

    private IEnumerator ReturnToLobbyAfterDeath()
    {
        isReturningToLobby = true;
        yield return new WaitForSeconds(deathReturnDelay);

        DungeonRunManager dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();
        if (dungeonRunManager != null)
            dungeonRunManager.EndRunByDeath();

        MoveToLobbySpawn();
        ResetHealth();
    }

    private void MoveToLobbySpawn()
    {
        Vector2 spawnPosition = Vector2.zero;
        LobbySpawnPoint lobbySpawnPoint = FindFirstObjectByType<LobbySpawnPoint>();
        if (lobbySpawnPoint != null)
            spawnPosition = lobbySpawnPoint.transform.position;

        if (rb != null)
            rb.position = spawnPosition;
        else
            transform.position = new Vector3(spawnPosition.x, spawnPosition.y, transform.position.z);
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
