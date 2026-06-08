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
}
