using System;
using UnityEngine;

/// <summary>
/// 모바일 키우기 게임에서 저장해야 하는 주요 데이터를 정리한 예시 클래스입니다.
/// 실제 프로젝트에서는 더 많은 변수가 있었지만, 포트폴리오용으로 핵심 구조만 분리했습니다.
/// </summary>
[Serializable]
public class SaveDataExample
{
    [Header("기본 재화")]
    public double money;
    public int diamond;

    [Header("성장 데이터")]
    public int specLevel;
    public int companyLevel;
    public int rankIndex;

    [Header("알바 / 캐릭터")]
    public int totalAlbaCount;
    public int[] albaCount;
    public int[] albaUnlockState;

    [Header("광고 / 결제")]
    public bool removeAds;
    public int[] packageBuyState;
    public int[] adCooldownSecond;
    public int[] adCooldownState;

    [Header("오프라인 보상")]
    public string lastPlayTime;
    public int offlineRewardPending;

    [Header("환경설정")]
    public float bgmVolume;
    public float sfxVolume;
    public int frameSetting;

    [Header("버전 / 마이그레이션")]
    public int saveVersion;

    public const int CurrentSaveVersion = 1;

    /// <summary>
    /// 신규 유저 또는 저장 데이터가 없을 때 사용할 기본 데이터입니다.
    /// </summary>
    public static SaveDataExample CreateDefault()
    {
        return new SaveDataExample
        {
            money = 0,
            diamond = 0,

            specLevel = 0,
            companyLevel = 0,
            rankIndex = 0,

            totalAlbaCount = 0,
            albaCount = new int[20],
            albaUnlockState = new int[20],

            removeAds = false,
            packageBuyState = new int[12],
            adCooldownSecond = new int[6],
            adCooldownState = new int[6],

            lastPlayTime = DateTime.Now.ToString("O"),
            offlineRewardPending = 0,

            bgmVolume = 1f,
            sfxVolume = 1f,
            frameSetting = 60,

            saveVersion = CurrentSaveVersion
        };
    }

    /// <summary>
    /// 앱 업데이트 후 배열 길이가 바뀌거나 신규 변수가 추가되었을 때
    /// 기존 유저 데이터가 깨지지 않도록 보정합니다.
    /// </summary>
    public void FixLegacyData()
    {
        albaCount = FixIntArray(albaCount, 20);
        albaUnlockState = FixIntArray(albaUnlockState, 20);

        packageBuyState = FixIntArray(packageBuyState, 12);
        adCooldownSecond = FixIntArray(adCooldownSecond, 6);
        adCooldownState = FixIntArray(adCooldownState, 6);

        if (string.IsNullOrEmpty(lastPlayTime))
            lastPlayTime = DateTime.Now.ToString("O");

        if (bgmVolume <= 0)
            bgmVolume = 1f;

        if (sfxVolume <= 0)
            sfxVolume = 1f;

        if (frameSetting <= 0)
            frameSetting = 60;

        if (saveVersion <= 0)
            saveVersion = CurrentSaveVersion;
    }

    private static int[] FixIntArray(int[] source, int targetLength)
    {
        int[] result = new int[targetLength];

        if (source == null)
            return result;

        int copyLength = Mathf.Min(source.Length, targetLength);

        for (int i = 0; i < copyLength; i++)
            result[i] = source[i];

        return result;
    }
}
