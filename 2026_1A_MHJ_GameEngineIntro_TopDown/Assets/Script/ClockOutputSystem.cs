using UnityEngine;

public class ClockOutputSystem : MonoBehaviour
{
    [Header("Gauge")]
    [SerializeField] private float maxOutput = 100f;
    [SerializeField] private float currentOutput = 50f;

    [Header("Style Gain")]
    [SerializeField] private float attackHitGain = 4f;
    [SerializeField] private float swapAttackBonusGain = 8f;
    [SerializeField] private float dodgeGain = 12f;
    [SerializeField] private float parryGain = 18f;
    [SerializeField] private float actionChainTime = 2.2f;
    [SerializeField] private float styleStepBonus = 0.12f;
    [SerializeField] private float maxStyleMultiplier = 2.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = true;

    public float CurrentOutput => currentOutput;
    public float MaxOutput => maxOutput;
    public int StyleChain => styleChain;
    public float StyleMultiplier => GetStyleMultiplier();

    private int styleChain;
    private float styleTimer;

    private void Update()
    {
        if (styleTimer <= 0f)
            return;

        styleTimer -= Time.deltaTime;
        if (styleTimer <= 0f)
            styleChain = 0;
    }

    public void GainFromAttackHit(int hitCount, bool swapAttackBonus)
    {
        int actionCount = Mathf.Max(1, hitCount);
        float gain = attackHitGain * actionCount;
        if (swapAttackBonus)
            gain += swapAttackBonusGain;

        AddStyledOutput(gain, actionCount);
    }

    public void GainFromDodge()
    {
        AddStyledOutput(dodgeGain, 1);
    }

    public void GainFromParry()
    {
        AddStyledOutput(parryGain, 2);
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

    public void BreakStyleChain()
    {
        styleChain = 0;
        styleTimer = 0f;
    }

    private void AddOutput(float amount)
    {
        currentOutput = Mathf.Clamp(currentOutput + amount, 0f, maxOutput);
    }

    private void AddStyledOutput(float amount, int chainAdd)
    {
        float multiplier = GetStyleMultiplier();
        AddOutput(amount * multiplier);

        styleChain += Mathf.Max(1, chainAdd);
        styleTimer = actionChainTime;
    }

    private float GetStyleMultiplier()
    {
        return Mathf.Min(maxStyleMultiplier, 1f + styleChain * styleStepBonus);
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(20f, 205f, 260f, 78f), GUI.skin.box);
        GUILayout.Label($"Clock Output: {currentOutput:0} / {maxOutput:0}");
        GUILayout.Label($"Style Chain: {styleChain}  x{GetStyleMultiplier():0.0}");
        GUILayout.EndArea();
    }
}
