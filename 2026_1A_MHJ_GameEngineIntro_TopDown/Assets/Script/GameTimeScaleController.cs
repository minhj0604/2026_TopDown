using System.Collections.Generic;
using UnityEngine;

public class GameTimeScaleController : MonoBehaviour
{
    public const int InvalidHandle = 0;

    private static GameTimeScaleController instance;
    private static int nextHandle = 1;

    private readonly List<SlowMotionRequest> requests = new List<SlowMotionRequest>();
    private float baseFixedDeltaTime;
    private float appliedScale = 1f;

    private class SlowMotionRequest
    {
        public int handle;
        public float scale;
        public float endTime;
    }

    public static int RequestSlowMotion(float scale, float duration)
    {
        if (duration <= 0f)
            return InvalidHandle;

        return Instance.AddRequest(scale, duration);
    }

    public static void CancelSlowMotion(int handle)
    {
        if (handle == InvalidHandle || instance == null)
            return;

        instance.RemoveRequest(handle);
    }

    public static void ClearAllSlowMotion()
    {
        if (instance == null)
            return;

        instance.requests.Clear();
        instance.ApplyScale(1f);
    }

    private static GameTimeScaleController Instance
    {
        get
        {
            if (instance != null)
                return instance;

            GameObject controllerObject = new GameObject("Game Time Scale Controller");
            instance = controllerObject.AddComponent<GameTimeScaleController>();
            DontDestroyOnLoad(controllerObject);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        float currentScale = Mathf.Max(Time.timeScale, 0.01f);
        baseFixedDeltaTime = Time.fixedDeltaTime / currentScale;
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;
        for (int i = requests.Count - 1; i >= 0; i--)
        {
            if (requests[i].endTime <= now)
                requests.RemoveAt(i);
        }

        RefreshScale();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        ApplyScale(1f);
        instance = null;
    }

    private int AddRequest(float scale, float duration)
    {
        SlowMotionRequest request = new SlowMotionRequest
        {
            handle = nextHandle++,
            scale = Mathf.Clamp(scale, 0.01f, 1f),
            endTime = Time.realtimeSinceStartup + duration
        };

        requests.Add(request);
        RefreshScale();
        return request.handle;
    }

    private void RemoveRequest(int handle)
    {
        for (int i = requests.Count - 1; i >= 0; i--)
        {
            if (requests[i].handle == handle)
                requests.RemoveAt(i);
        }

        RefreshScale();
    }

    private void RefreshScale()
    {
        float targetScale = 1f;
        for (int i = 0; i < requests.Count; i++)
            targetScale = Mathf.Min(targetScale, requests[i].scale);

        ApplyScale(targetScale);
    }

    private void ApplyScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0.01f, 1f);
        if (Mathf.Approximately(appliedScale, scale)
            && Mathf.Approximately(Time.timeScale, scale)
            && Mathf.Approximately(Time.fixedDeltaTime, baseFixedDeltaTime * scale))
        {
            return;
        }

        appliedScale = scale;
        Time.timeScale = scale;
        Time.fixedDeltaTime = baseFixedDeltaTime * scale;
    }
}
