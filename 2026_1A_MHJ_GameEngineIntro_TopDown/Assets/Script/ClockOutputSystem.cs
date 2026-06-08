using UnityEngine;

public class ClockOutputSystem : MonoBehaviour
{
    [Header("출력 게이지")]
    [SerializeField] private float maxOutput = 100f;
    [SerializeField] private float currentOutput = 50f;

    [Header("충전량")]
    [SerializeField] private float attackHitGain = 8f;
    [SerializeField] private float swapAttackBonusGain = 8f;
    [SerializeField] private float dodgeGain = 6f;

    [Header("테스트 표시")]
    [SerializeField] private bool showDebugUI = true;

    public float CurrentOutput => currentOutput;
    public float MaxOutput => maxOutput;

    public void GainFromAttackHit(int hitCount, bool swapAttackBonus)
    {
        float gain = attackHitGain * Mathf.Max(1, hitCount);
        if (swapAttackBonus)
            gain += swapAttackBonusGain;

        AddOutput(gain);
    }

    public void GainFromDodge()
    {
        AddOutput(dodgeGain);
    }

    public bool TrySpend(float amount)
    {
        if (amount <= 0f)
            return true;

        if (!CanSpend(amount))
            return false;

        currentOutput -= amount;
        return true;
    }

    public bool CanSpend(float amount)
    {
        return amount <= 0f || currentOutput >= amount;
    }

    private void AddOutput(float amount)
    {
        currentOutput = Mathf.Clamp(currentOutput + amount, 0f, maxOutput);
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(20f, 205f, 260f, 55f), GUI.skin.box);
        GUILayout.Label($"Clock Output: {currentOutput:0} / {maxOutput:0}");
        GUILayout.EndArea();
    }
}
