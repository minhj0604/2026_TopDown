using UnityEngine;

public class ClockOutputSystem : MonoBehaviour
{
    private enum StyleAction
    {
        None,
        Attack,
        SwapAttack,
        Dodge,
        Parry
    }

    [Header("Gauge")]
    [SerializeField] private float maxOutput = 100f;
    [SerializeField] private float currentOutput = 50f;

    [Header("Output Gain")]
    [SerializeField] private float attackOutputGain = 2.5f;
    [SerializeField] private float swapAttackOutputGain = 5f;
    [SerializeField] private float dodgeOutputGain = 8f;
    [SerializeField] private float parryOutputGain = 11f;

    [Header("Style Score")]
    [SerializeField] private float maxStyleScore = 700f;
    [SerializeField] private float attackStyleGain = 20f;
    [SerializeField] private float swapAttackStyleGain = 45f;
    [SerializeField] private float multiHitStyleBonus = 8f;
    [SerializeField] private float dodgeStyleGain = 70f;
    [SerializeField] private float parryStyleGain = 110f;
    [SerializeField] private float styleDecayDelay = 2.2f;
    [SerializeField] private float styleDecayPerSecond = 55f;
    [SerializeField] private float damageStyleLoss = 180f;
    [Range(0.1f, 1f)]
    [SerializeField] private float repeatedAttackPenalty = 0.82f;

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = true;

    public float CurrentOutput => currentOutput;
    public float MaxOutput => maxOutput;
    public float StyleScore => styleScore;
    public int StyleChain => styleChain;
    public string StyleRankName => GetStyleRankName();
    public float StyleMultiplier => GetStyleMultiplier();

    private float styleScore;
    private float styleTimer;
    private int styleChain;
    private StyleAction lastAction = StyleAction.None;
    private int repeatedActionCount;

    private void Update()
    {
        if (styleTimer > 0f)
        {
            styleTimer -= Time.deltaTime;
            return;
        }

        if (styleScore <= 0f)
            return;

        styleScore = Mathf.Max(0f, styleScore - styleDecayPerSecond * Time.deltaTime);
        if (styleScore <= 0f)
        {
            styleChain = 0;
            lastAction = StyleAction.None;
            repeatedActionCount = 0;
        }
    }

    public void GainFromAttackHit(int hitCount, bool swapAttackBonus)
    {
        int hits = Mathf.Max(1, hitCount);
        StyleAction action = swapAttackBonus ? StyleAction.SwapAttack : StyleAction.Attack;
        float styleGain = attackStyleGain + multiHitStyleBonus * Mathf.Max(0, hits - 1);
        float outputGain = attackOutputGain * hits;

        if (swapAttackBonus)
        {
            styleGain += swapAttackStyleGain;
            outputGain += swapAttackOutputGain;
        }

        RegisterStyleAction(action, styleGain, outputGain, hits);
    }

    public void GainFromDodge()
    {
        RegisterStyleAction(StyleAction.Dodge, dodgeStyleGain, dodgeOutputGain, 2);
    }

    public void GainFromParry()
    {
        RegisterStyleAction(StyleAction.Parry, parryStyleGain, parryOutputGain, 3);
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
        styleScore = Mathf.Max(0f, styleScore - damageStyleLoss);
        styleChain = 0;
        styleTimer = 0f;
        lastAction = StyleAction.None;
        repeatedActionCount = 0;
    }

    private void RegisterStyleAction(StyleAction action, float styleGain, float outputGain, int chainAdd)
    {
        float repetitionPenalty = GetRepetitionPenalty(action);
        styleScore = Mathf.Clamp(styleScore + styleGain * repetitionPenalty, 0f, maxStyleScore);
        AddOutput(outputGain * repetitionPenalty * GetStyleMultiplier());

        styleChain += Mathf.Max(1, chainAdd);
        styleTimer = styleDecayDelay;
    }

    private float GetRepetitionPenalty(StyleAction action)
    {
        if (action == StyleAction.Attack && lastAction == StyleAction.Attack)
            repeatedActionCount++;
        else
            repeatedActionCount = 0;

        lastAction = action;

        if (action != StyleAction.Attack || repeatedActionCount <= 0)
            return 1f;

        return Mathf.Pow(repeatedAttackPenalty, repeatedActionCount);
    }

    private void AddOutput(float amount)
    {
        currentOutput = Mathf.Clamp(currentOutput + amount, 0f, maxOutput);
    }

    private float GetStyleMultiplier()
    {
        if (styleScore >= 560f) return 2f;
        if (styleScore >= 380f) return 1.6f;
        if (styleScore >= 220f) return 1.35f;
        if (styleScore >= 100f) return 1.15f;
        return 1f;
    }

    private string GetStyleRankName()
    {
        if (styleScore >= 560f) return "S";
        if (styleScore >= 380f) return "A";
        if (styleScore >= 220f) return "B";
        if (styleScore >= 100f) return "C";
        return "D";
    }

}
