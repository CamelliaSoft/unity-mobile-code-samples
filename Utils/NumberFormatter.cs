using System;
using System.Text;

/// <summary>
/// 키우기/방치형 게임에서 커지는 재화 수치를 짧게 표기하기 위한 숫자 포맷 유틸입니다.
/// 
/// 예시:
/// 950 -> 950
/// 12,300 -> 1.23만
/// 345,000,000 -> 3.45억
/// 1e16 이상 -> 1.23e16
/// </summary>
public static class NumberFormatter
{
    private static readonly string[] KoreanUnits =
    {
        "",
        "만",
        "억",
        "조",
        "경",
        "해",
        "자",
        "양",
        "구",
        "간",
        "정",
        "재",
        "극"
    };

    /// <summary>
    /// 일반 재화 표기용입니다.
    /// 너무 긴 숫자를 UI에서 읽기 쉽게 줄여 보여줍니다.
    /// </summary>
    public static string FormatMoney(double value, string suffix = "원")
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0" + suffix;

        if (value < 0)
            return "-" + FormatMoney(Math.Abs(value), suffix);

        if (value < 1000)
            return value.ToString("N0") + suffix;

        return FormatKoreanUnit(value) + suffix;
    }

    /// <summary>
    /// 초당 수익, 터치 수익처럼 소수점이 조금 필요한 값에 사용합니다.
    /// </summary>
    public static string FormatIncome(double value, string suffix = "원")
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0" + suffix;

        if (value < 0)
            return "-" + FormatIncome(Math.Abs(value), suffix);

        if (value < 100)
            return value.ToString("N2") + suffix;

        if (value < 1000)
            return value.ToString("N1") + suffix;

        return FormatKoreanUnit(value) + suffix;
    }

    /// <summary>
    /// 랭킹 조건, 업적 조건처럼 단위만 간결하게 보여줄 때 사용합니다.
    /// </summary>
    public static string FormatShort(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0";

        if (value < 0)
            return "-" + FormatShort(Math.Abs(value));

        if (value < 1000)
            return value.ToString("N0");

        return FormatKoreanUnit(value);
    }

    /// <summary>
    /// 퍼센트 표기용입니다.
    /// 수익률, 버프 수치, 확률 표기 등에 사용합니다.
    /// </summary>
    public static string FormatPercent(double value, int decimalPoint = 2)
    {
        string format = "N" + Math.Max(0, decimalPoint);
        return value.ToString(format) + "%";
    }

    /// <summary>
    /// 큰 수를 한국식 단위로 변환합니다.
    /// 10,000 단위마다 만, 억, 조, 경 순서로 올라갑니다.
    /// </summary>
    private static string FormatKoreanUnit(double value)
    {
        int unitIndex = 0;

        while (value >= 10000d && unitIndex < KoreanUnits.Length - 1)
        {
            value /= 10000d;
            unitIndex++;
        }

        if (unitIndex >= KoreanUnits.Length - 1 && value >= 10000d)
            return value.ToString("0.##E+0");

        string numberText;

        if (value >= 100)
            numberText = value.ToString("N0");
        else if (value >= 10)
            numberText = value.ToString("N1");
        else
            numberText = value.ToString("N2");

        return TrimZero(numberText) + KoreanUnits[unitIndex];
    }

    /// <summary>
    /// 1.00만 → 1만
    /// 1.50만 → 1.5만
    /// 처럼 불필요한 0을 제거합니다.
    /// </summary>
    private static string TrimZero(string text)
    {
        if (!text.Contains("."))
            return text;

        return text.TrimEnd('0').TrimEnd('.');
    }

    /// <summary>
    /// 여러 단위를 함께 보여주고 싶을 때 사용하는 확장형 포맷입니다.
    /// 예: 1억 2345만
    /// 기본 UI에서는 너무 길어질 수 있어 필요한 곳에만 사용합니다.
    /// </summary>
    public static string FormatDetail(double value, int maxUnitCount = 2)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "0";

        if (value < 0)
            return "-" + FormatDetail(Math.Abs(value), maxUnitCount);

        if (value < 10000)
            return value.ToString("N0");

        long longValue = (long)Math.Floor(value);
        StringBuilder builder = new StringBuilder();

        int unitIndex = 0;
        int visibleUnitCount = 0;

        while (longValue > 0 && unitIndex < KoreanUnits.Length)
        {
            long part = longValue % 10000;

            if (part > 0)
            {
                string unitText = part.ToString("N0") + KoreanUnits[unitIndex];

                if (builder.Length == 0)
                    builder.Insert(0, unitText);
                else
                    builder.Insert(0, unitText + " ");

                visibleUnitCount++;

                if (visibleUnitCount >= maxUnitCount)
                    break;
            }

            longValue /= 10000;
            unitIndex++;
        }

        return builder.ToString();
    }
}
