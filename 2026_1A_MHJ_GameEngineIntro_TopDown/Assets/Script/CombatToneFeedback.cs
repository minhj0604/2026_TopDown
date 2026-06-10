using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CombatToneFeedback : MonoBehaviour
{
    [SerializeField] private Color darkenColor = new Color(0.62f, 0.62f, 0.68f, 1f);
    [SerializeField] private float defaultDuration = 0.7f;
    [SerializeField] private float fadeInTime = 0.22f;
    [SerializeField] private float fadeOutTime = 0.12f;

    private Coroutine toneRoutine;
    private SpriteRenderer[] tintedRenderers = new SpriteRenderer[0];
    private Color[] originalRendererColors = new Color[0];
    private Tilemap[] tintedTilemaps = new Tilemap[0];
    private Color[] originalTilemapColors = new Color[0];

    public void Play()
    {
        Play(defaultDuration);
    }

    public void Play(float duration)
    {
        if (toneRoutine != null)
        {
            StopCoroutine(toneRoutine);
            RestoreColors();
        }

        toneRoutine = StartCoroutine(ToneRoutine(Mathf.Max(0.03f, duration)));
    }

    public void StopAndRestore()
    {
        if (toneRoutine != null)
        {
            StopCoroutine(toneRoutine);
            toneRoutine = null;
        }

        RestoreColors();
    }

    private void OnDisable()
    {
        StopAndRestore();
    }

    private IEnumerator ToneRoutine(float duration)
    {
        CaptureTargets();

        float holdTime = Mathf.Max(0f, duration - fadeInTime - fadeOutTime);

        yield return FadeTone(0f, 1f, fadeInTime);
        if (holdTime > 0f)
            yield return new WaitForSecondsRealtime(holdTime);
        yield return FadeTone(1f, 0f, fadeOutTime);

        RestoreColors();
        toneRoutine = null;
    }

    private void CaptureTargets()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        tintedRenderers = new SpriteRenderer[renderers.Length];
        originalRendererColors = new Color[renderers.Length];
        int count = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.transform.IsChildOf(transform))
                continue;

            tintedRenderers[count] = renderer;
            originalRendererColors[count] = renderer.color;
            count++;
        }

        System.Array.Resize(ref tintedRenderers, count);
        System.Array.Resize(ref originalRendererColors, count);

        Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        tintedTilemaps = new Tilemap[tilemaps.Length];
        originalTilemapColors = new Color[tilemaps.Length];
        int tilemapCount = 0;

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            if (tilemap == null)
                continue;

            tintedTilemaps[tilemapCount] = tilemap;
            originalTilemapColors[tilemapCount] = tilemap.color;
            tilemapCount++;
        }

        System.Array.Resize(ref tintedTilemaps, tilemapCount);
        System.Array.Resize(ref originalTilemapColors, tilemapCount);
    }

    private IEnumerator FadeTone(float from, float to, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float amount = Mathf.SmoothStep(from, to, t);
            ApplyToneAmount(amount);
            yield return null;
        }

        ApplyToneAmount(to);
    }

    private void ApplyToneAmount(float amount)
    {
        for (int i = 0; i < tintedRenderers.Length; i++)
        {
            if (tintedRenderers[i] == null)
                continue;

            Color darkColor = MultiplyColor(originalRendererColors[i], darkenColor);
            tintedRenderers[i].color = Color.Lerp(originalRendererColors[i], darkColor, amount);
        }

        for (int i = 0; i < tintedTilemaps.Length; i++)
        {
            if (tintedTilemaps[i] == null)
                continue;

            Color darkColor = MultiplyColor(originalTilemapColors[i], darkenColor);
            tintedTilemaps[i].color = Color.Lerp(originalTilemapColors[i], darkColor, amount);
        }
    }

    private void RestoreColors()
    {
        for (int i = 0; i < tintedRenderers.Length; i++)
        {
            if (tintedRenderers[i] != null)
                tintedRenderers[i].color = originalRendererColors[i];
        }

        tintedRenderers = new SpriteRenderer[0];
        originalRendererColors = new Color[0];

        for (int i = 0; i < tintedTilemaps.Length; i++)
        {
            if (tintedTilemaps[i] != null)
                tintedTilemaps[i].color = originalTilemapColors[i];
        }

        tintedTilemaps = new Tilemap[0];
        originalTilemapColors = new Color[0];
    }

    private Color MultiplyColor(Color color, Color multiplier)
    {
        return new Color(
            color.r * multiplier.r,
            color.g * multiplier.g,
            color.b * multiplier.b,
            color.a);
    }
}
