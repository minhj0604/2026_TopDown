using UnityEngine;

public class SimpleCameraShake : MonoBehaviour
{
    private Vector3 lastOffset;
    private float shakeTimer;
    private float shakeDuration;
    private float shakePower;

    private void LateUpdate()
    {
        transform.position -= lastOffset;
        lastOffset = Vector3.zero;

        if (shakeTimer <= 0f)
            return;

        shakeTimer -= Time.deltaTime;
        if (shakeTimer <= 0f)
            return;

        float progress = shakeDuration > 0f ? shakeTimer / shakeDuration : 0f;
        float currentPower = shakePower * Mathf.Clamp01(progress);
        Vector2 offset = Random.insideUnitCircle * currentPower;

        lastOffset = new Vector3(offset.x, offset.y, 0f);
        transform.position += lastOffset;
    }

    public void Shake(float duration, float power)
    {
        if (duration <= 0f || power <= 0f)
            return;

        shakeDuration = duration;
        shakeTimer = duration;
        shakePower = power;
    }
}
