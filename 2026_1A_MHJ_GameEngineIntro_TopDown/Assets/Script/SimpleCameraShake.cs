using UnityEngine;

public class SimpleCameraShake : MonoBehaviour
{
    [SerializeField] private float focusZoomSpeed = 1.6f;
    [SerializeField] private float focusBounceAmount = 0.045f;
    [SerializeField] private float focusBounceTime = 0.1f;
    [SerializeField] private float actionLeadMoveSpeed = 12f;
    [SerializeField] private float actionLeadReturnSpeed = 8f;

    private Camera controlledCamera;
    private float baseOrthographicSize;
    private float targetOrthographicSize;
    private Coroutine zoomBounceRoutine;
    private Coroutine roomClearZoomRoutine;
    private Coroutine actionLeadRoutine;
    private Vector3 lastOffset;
    private Vector3 lastActionLeadOffset;
    private Vector3 targetActionLeadOffset;
    private float shakeTimer;
    private float shakeDuration;
    private float shakePower;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        if (controlledCamera != null)
        {
            baseOrthographicSize = controlledCamera.orthographicSize;
            targetOrthographicSize = baseOrthographicSize;
        }
    }

    private void LateUpdate()
    {
        UpdateFocusZoom();

        transform.position -= lastOffset;
        transform.position -= lastActionLeadOffset;
        lastOffset = Vector3.zero;
        lastActionLeadOffset = Vector3.Lerp(
            lastActionLeadOffset,
            targetActionLeadOffset,
            Time.deltaTime * GetActionLeadSpeed());
        transform.position += lastActionLeadOffset;

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

    public void LeadToward(Vector2 direction, float amount, float holdTime)
    {
        if (direction.sqrMagnitude <= 0.01f || amount <= 0f)
            return;

        if (actionLeadRoutine != null)
            StopCoroutine(actionLeadRoutine);

        actionLeadRoutine = StartCoroutine(ActionLeadRoutine(direction.normalized * amount, holdTime));
    }

    public void SetFocusZoom(float zoomInAmount)
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null)
            return;

        if (baseOrthographicSize <= 0f)
            baseOrthographicSize = controlledCamera.orthographicSize;

        if (zoomBounceRoutine != null)
        {
            StopCoroutine(zoomBounceRoutine);
            zoomBounceRoutine = null;
        }

        targetOrthographicSize = Mathf.Max(0.5f, baseOrthographicSize - Mathf.Max(0f, zoomInAmount));
    }

    public void ClearFocusZoom(bool bounceBack)
    {
        ClearFocusZoom(bounceBack, false);
    }

    public void ClearFocusZoom(bool bounceBack, bool snapBack)
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null)
            return;

        targetOrthographicSize = baseOrthographicSize > 0f
            ? baseOrthographicSize
            : controlledCamera.orthographicSize;

        if (zoomBounceRoutine != null)
            StopCoroutine(zoomBounceRoutine);

        if (snapBack)
        {
            controlledCamera.orthographicSize = targetOrthographicSize;
            zoomBounceRoutine = null;
            return;
        }

        if (bounceBack)
            zoomBounceRoutine = StartCoroutine(FocusBounceRoutine(targetOrthographicSize));
        else
            zoomBounceRoutine = null;
    }

    public void PlayRoomClearZoomOut(float zoomOutAmount, float zoomOutTime, float holdTime, float returnTime)
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();
        if (controlledCamera == null)
            return;

        if (baseOrthographicSize <= 0f)
            baseOrthographicSize = controlledCamera.orthographicSize;

        if (zoomBounceRoutine != null)
        {
            StopCoroutine(zoomBounceRoutine);
            zoomBounceRoutine = null;
        }
        if (roomClearZoomRoutine != null)
            StopCoroutine(roomClearZoomRoutine);

        roomClearZoomRoutine = StartCoroutine(RoomClearZoomRoutine(
            Mathf.Max(0f, zoomOutAmount),
            Mathf.Max(0.01f, zoomOutTime),
            Mathf.Max(0f, holdTime),
            Mathf.Max(0.01f, returnTime)));
    }

    private void UpdateFocusZoom()
    {
        if (controlledCamera == null)
            return;

        controlledCamera.orthographicSize = Mathf.Lerp(
            controlledCamera.orthographicSize,
            targetOrthographicSize,
            Time.deltaTime * focusZoomSpeed);
    }

    private float GetActionLeadSpeed()
    {
        return targetActionLeadOffset.sqrMagnitude > lastActionLeadOffset.sqrMagnitude
            ? actionLeadMoveSpeed
            : actionLeadReturnSpeed;
    }

    private System.Collections.IEnumerator ActionLeadRoutine(Vector2 offset, float holdTime)
    {
        targetActionLeadOffset = new Vector3(offset.x, offset.y, 0f);
        yield return new WaitForSeconds(Mathf.Max(0.01f, holdTime));

        targetActionLeadOffset = Vector3.zero;
        actionLeadRoutine = null;
    }

    private System.Collections.IEnumerator FocusBounceRoutine(float baseSize)
    {
        float startSize = controlledCamera.orthographicSize;
        float overshootSize = baseSize + focusBounceAmount;
        float undershootSize = Mathf.Max(0.5f, baseSize - focusBounceAmount * 0.35f);

        yield return ZoomStep(startSize, overshootSize, focusBounceTime);
        yield return ZoomStep(overshootSize, undershootSize, focusBounceTime);
        yield return ZoomStep(undershootSize, baseSize, focusBounceTime * 1.4f);

        controlledCamera.orthographicSize = baseSize;
        targetOrthographicSize = baseSize;
        zoomBounceRoutine = null;
    }

    private System.Collections.IEnumerator RoomClearZoomRoutine(float zoomOutAmount, float zoomOutTime, float holdTime, float returnTime)
    {
        float baseSize = baseOrthographicSize > 0f ? baseOrthographicSize : controlledCamera.orthographicSize;
        float zoomedOutSize = baseSize + zoomOutAmount;
        targetOrthographicSize = zoomedOutSize;

        yield return ZoomStepUnscaled(controlledCamera.orthographicSize, zoomedOutSize, zoomOutTime);
        if (holdTime > 0f)
            yield return new WaitForSecondsRealtime(holdTime);

        targetOrthographicSize = baseSize;
        yield return ZoomStepUnscaled(controlledCamera.orthographicSize, baseSize, returnTime);

        controlledCamera.orthographicSize = baseSize;
        targetOrthographicSize = baseSize;
        roomClearZoomRoutine = null;
    }

    private System.Collections.IEnumerator ZoomStep(float from, float to, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            controlledCamera.orthographicSize = Mathf.Lerp(from, to, t);
            yield return null;
        }
    }

    private System.Collections.IEnumerator ZoomStepUnscaled(float from, float to, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            controlledCamera.orthographicSize = Mathf.Lerp(from, to, t);
            yield return null;
        }
    }
}
