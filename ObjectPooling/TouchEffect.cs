using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 터치 위치에서 재생되는 UI 이펙트입니다.
/// 재생이 끝나면 오브젝트를 비활성화하여 풀에 반환됩니다.
/// </summary>
public class TouchEffect : MonoBehaviour
{
    [SerializeField] private Image effectImage;
    [SerializeField] private Color effectColor = Color.white;
    [SerializeField] private float startSize = 100f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float fadeSpeed = 2f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (effectImage == null)
            effectImage = GetComponent<Image>();
    }

    public void Play(Vector2 anchoredPosition)
    {
        RectTransform rectTransform = transform as RectTransform;

        if (rectTransform != null)
            rectTransform.anchoredPosition = anchoredPosition;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        transform.localScale = new Vector3(startSize, startSize, 1f);

        if (effectImage != null)
            effectImage.color = effectColor;

        gameObject.SetActive(true);
        fadeCoroutine = StartCoroutine(FadeOutAndShrink());
    }

    private IEnumerator FadeOutAndShrink()
    {
        Vector3 initialScale = transform.localScale;
        Color initialColor = effectImage != null ? effectImage.color : Color.white;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = elapsed / duration;

            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);

            if (effectImage != null)
            {
                Color color = initialColor;
                color.a = Mathf.Lerp(initialColor.a, 0f, progress * fadeSpeed);
                effectImage.color = color;
            }

            yield return null;
        }

        fadeCoroutine = null;
        gameObject.SetActive(false);
    }
}
