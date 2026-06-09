using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hurtInvincibleTime = 1.5f;
    [SerializeField] private float hurtKnockbackDistance = 0.18f;
    [SerializeField] private float deathReturnDelay = 0.6f;
    [SerializeField] private Color hurtColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Low Health Feedback")]
    [SerializeField] private float lowHealthRatio = 0.3f;
    [SerializeField] private float lowHealthShakeInterval = 0.45f;
    [SerializeField] private float lowHealthShakeTime = 0.05f;
    [SerializeField] private float lowHealthShakePower = 0.018f;
    [SerializeField] private Color lowHealthVignetteColor = new Color(0.8f, 0f, 0f, 0.18f);
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
    private Texture2D vignetteTexture;
    private float lowHealthShakeTimer;

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

        UpdateLowHealthCameraShake();
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
        lowHealthShakeTimer = 0f;
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
        DrawLowHealthVignette();

        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(300f, 20f, 180f, 80f), GUI.skin.box);
        GUILayout.Label($"Player HP: {currentHealth:0}/{maxHealth:0}");
        GUILayout.Label(IsInvincible ? "Invincible" : "Vulnerable");
        if (GUILayout.Button("Reset HP"))
            ResetHealth();
        GUILayout.EndArea();
    }

    private void UpdateLowHealthCameraShake()
    {
        if (IsDead || !IsLowHealth())
        {
            lowHealthShakeTimer = 0f;
            return;
        }

        lowHealthShakeTimer -= Time.deltaTime;
        if (lowHealthShakeTimer > 0f)
            return;

        lowHealthShakeTimer = lowHealthShakeInterval;
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake shake = mainCamera.GetComponent<SimpleCameraShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        shake.Shake(lowHealthShakeTime, lowHealthShakePower);
    }

    private void DrawLowHealthVignette()
    {
        if (!IsLowHealth() || IsDead)
            return;

        if (vignetteTexture == null)
        {
            vignetteTexture = new Texture2D(1, 1);
            vignetteTexture.SetPixel(0, 0, Color.white);
            vignetteTexture.Apply();
        }

        float danger = 1f - Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth * lowHealthRatio));
        Color previousColor = GUI.color;
        GUI.color = new Color(lowHealthVignetteColor.r, lowHealthVignetteColor.g, lowHealthVignetteColor.b, lowHealthVignetteColor.a * danger);

        float edge = Mathf.Lerp(18f, 54f, danger);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, edge), vignetteTexture);
        GUI.DrawTexture(new Rect(0f, Screen.height - edge, Screen.width, edge), vignetteTexture);
        GUI.DrawTexture(new Rect(0f, 0f, edge, Screen.height), vignetteTexture);
        GUI.DrawTexture(new Rect(Screen.width - edge, 0f, edge, Screen.height), vignetteTexture);

        GUI.color = previousColor;
    }

    private bool IsLowHealth()
    {
        return currentHealth > 0f && currentHealth <= maxHealth * lowHealthRatio;
    }
}
