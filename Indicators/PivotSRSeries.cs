using Binance.Net.Enums;
using Binance.Net.Interfaces;

public enum PivotSupportRegist
{
    P, R7, R6, R5, R4, R3, R2, R1, S1, S2, S3, S4, S5, S6, S7
}

public static class PivotSRSeries
{
    public static Dictionary<PivotSupportRegist, decimal> DefaultSRSeries(this IBinanceExchangeKlineManager conn, string coinName, KlineInterval interval, int count)
    {
        var ret = new Dictionary<PivotSupportRegist, decimal>();
        if (conn.GetKlines(coinName, interval, true, out var list) == false) return ret;
        var listCount = list.Count;
        var lastcandle = list[^1];
        var low = decimal.MaxValue;
        var high = decimal.MinValue;
        var close = lastcandle.ClosePrice;
        for (int i = listCount - count - 1; i < listCount - 1; ++i)
        {
            high = Math.Max(high, list[i].HighPrice);
            low = Math.Min(low, list[i].LowPrice);
        }

        var highlowGap = high - low;
        var pp = (high + low + close) / 3;
        var r1 = 2 * pp - low;
        var r2 = pp + highlowGap;
        var r3 = r1 + highlowGap;
        var s1 = 2 * pp - high;
        var s2 = pp - highlowGap;
        var s3 = s1 - highlowGap;
        ret[PivotSupportRegist.P] = pp;
        ret[PivotSupportRegist.R1] = r1;
        ret[PivotSupportRegist.R2] = r2;
        ret[PivotSupportRegist.R3] = r3;
        ret[PivotSupportRegist.S1] = s1;
        ret[PivotSupportRegist.S2] = s2;
        ret[PivotSupportRegist.S3] = s3;
        #if LINUX_MINSU
        ret[PivotSupportRegist.R4] = 0;
        ret[PivotSupportRegist.R5] = 0;
        ret[PivotSupportRegist.R6] = 0;
        ret[PivotSupportRegist.R7] = 0;
        ret[PivotSupportRegist.S4] = 0;
        ret[PivotSupportRegist.S5] = 0;
        ret[PivotSupportRegist.S6] = 0;
        ret[PivotSupportRegist.S7] = 0;
        #else
        ret[PivotSupportRegist.R4] = 0;
        ret[PivotSupportRegist.R5] = 0;
        ret[PivotSupportRegist.R6] = 0;
        ret[PivotSupportRegist.R7] = 0;
        ret[PivotSupportRegist.S4] = 0;
        ret[PivotSupportRegist.S5] = 0;
        ret[PivotSupportRegist.S6] = 0;
        ret[PivotSupportRegist.S7] = 0;
        #endif
        return ret;
    }

    public static Dictionary<PivotSupportRegist, decimal> FibonattiSRSeries(this IBinanceExchangeKlineManager conn, string coinName,
        KlineInterval interval, int count)
    {
        
        if (conn.GetKlines(coinName, interval, true, out var list) == false) return new Dictionary<PivotSupportRegist, decimal>();
        return FibonattiSRSeries(list, count);
    }

    public static Dictionary<PivotSupportRegist, decimal> FibonattiSRSeries(List<IBinanceKline> candles, int count)
    {
        var ret = new Dictionary<PivotSupportRegist, decimal>();
        var listCount = candles.Count;
        var low = decimal.MaxValue;
        var high = decimal.MinValue;
        var lastcandle = candles[^1];
        var close = lastcandle.ClosePrice;
        for (int i = listCount - count - 1; i < listCount - 1; ++i)
        {
            high = Math.Max(high, candles[i].HighPrice);
            low = Math.Min(low, candles[i].LowPrice);
        }
        var highlowGap = high - low;
        var pp = (high + low + close) / 3;
        var r1 = pp + highlowGap * 0.382M;
        var r2 = pp + highlowGap * 0.618M;
        var r3 = pp + highlowGap;
        var s1 = pp - highlowGap * 0.382M;
        var s2 = pp - highlowGap * 0.618M;
        var s3 = pp - highlowGap;
        ret[PivotSupportRegist.P] = pp;
        ret[PivotSupportRegist.R1] = r1;
        ret[PivotSupportRegist.R2] = r2;
        ret[PivotSupportRegist.R3] = r3;
        ret[PivotSupportRegist.S1] = s1;
        ret[PivotSupportRegist.S2] = s2;
        ret[PivotSupportRegist.S3] = s3;
#if LINUX_MINSU
        ret[PivotSupportRegist.R4] = 0;
        ret[PivotSupportRegist.R5] = 0;
        ret[PivotSupportRegist.R6] = 0;
        ret[PivotSupportRegist.R7] = 0;
        ret[PivotSupportRegist.S4] = 0;
        ret[PivotSupportRegist.S5] = 0;
        ret[PivotSupportRegist.S6] = 0;
        ret[PivotSupportRegist.S7] = 0;
#else
        var r4 = pp + highlowGap * 1.618M;
        var r5 = pp + highlowGap * 1.762M;
        var r6 = pp + highlowGap * 2.762M;
        var r7 = pp + highlowGap * 3.762M;
        var s4 = pp - highlowGap * 1.618M;
        var s5 = pp - highlowGap * 1.762M;
        var s6 = pp - highlowGap * 2.762M;
        var s7 = pp - highlowGap * 3.762M;
        ret[PivotSupportRegist.R4] = r4;
        ret[PivotSupportRegist.R5] = r5;
        ret[PivotSupportRegist.R6] = r6;
        ret[PivotSupportRegist.R7] = r7;
        ret[PivotSupportRegist.S4] = s4;
        ret[PivotSupportRegist.S5] = s5;
        ret[PivotSupportRegist.S6] = s6;
        ret[PivotSupportRegist.S7] = s7;
#endif
        return ret;
    }

    public static Dictionary<PivotSupportRegist, decimal> Averages(
        params Dictionary<PivotSupportRegist, decimal>[] import)
    {
        var mergelist = new List<Dictionary<PivotSupportRegist, decimal>>(import);
        var averages = mergelist.SelectMany(dic => dic).GroupBy(kvp => kvp.Key)
            .ToDictionary(g => g.Key, g => g.Average(kvp => kvp.Value));
        return averages;
    }
}