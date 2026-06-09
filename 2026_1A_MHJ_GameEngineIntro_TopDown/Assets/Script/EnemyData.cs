using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game Data/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyName = "Enemy";
    public Sprite sprite;
    public Color color = Color.white;

    [Header("전투 스탯")]
    public float maxHealth = 30f;
    public float moveSpeed = 1f;
    public float contactDamage = 8f;
    public float contactDamageCooldown = 0.7f;
    public float knockbackForce = 2f;

    [Header("원거리 패턴")]
    public float preferredDistance = 2f;
    public float shootRange = 3.2f;
    public float shootStandTime = 0.25f;
    public int burstShotCount = 3;
    public float burstShotInterval = 0.18f;
    public float projectileDamage = 7f;
    public float projectileSpeed = 2.5f;
    public float shootInterval = 1.4f;

    [Header("돌진 패턴")]
    public float chargeStartRange = 1.8f;
    public float chargePrepareTime = 0.45f;
    public float chargeSpeed = 4f;
    public float chargeDuration = 0.35f;
    public float chargeCooldown = 1.4f;
}
