using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 터치 이펙트 오브젝트를 미리 생성해두고 재사용하는 풀입니다.
/// 짧은 시간에 반복 생성되는 UI 이펙트에서 Instantiate / Destroy 사용을 줄이기 위해 사용합니다.
/// </summary>
public class TouchEffectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private TouchEffect touchEffectPrefab;
    [SerializeField] private RectTransform effectParent;
    [SerializeField] private int initialPoolSize = 20;

    private readonly List<TouchEffect> pool = new List<TouchEffect>();

    private void Awake()
    {
        if (effectParent == null)
            effectParent = transform as RectTransform;

        CreateInitialPool();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Play(Input.mousePosition);
        }
    }

    private void CreateInitialPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewEffect();
        }
    }

    public void Play(Vector2 screenPosition)
    {
        TouchEffect effect = GetAvailableEffect();

        Vector2 anchoredPosition = ConvertScreenToAnchoredPosition(screenPosition);
        effect.Play(anchoredPosition);
    }

    private TouchEffect GetAvailableEffect()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }

        return CreateNewEffect();
    }

    private TouchEffect CreateNewEffect()
    {
        TouchEffect effect = Instantiate(touchEffectPrefab, effectParent);
        effect.gameObject.SetActive(false);
        pool.Add(effect);

        return effect;
    }

    private Vector2 ConvertScreenToAnchoredPosition(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectParent,
            screenPosition,
            null,
            out Vector2 anchoredPosition
        );

        return anchoredPosition;
    }
}
