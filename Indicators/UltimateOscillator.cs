
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class UlitimateOsc_SEVEN_FOURTEEN_TWENTYEIGHT : TechnicalDataCollections
{
    public UlitimateOsc_SEVEN_FOURTEEN_TWENTYEIGHT(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var ultObj = new UltimateOscillator(7, 14, 28, requestCandle[interval]);
                technicalDatas.Add(interval, ultObj);
            }
        }
    }

    protected override string indicatorName => "ultimate";
}

public class UltimateOscillator : TechnicalDataBase
{
    private int duration_1;
    private int duration_2;
    private int duration_3;
    
    protected override string indicatorName => $"ultimate";


    public UltimateOscillator(int dur1, int dur2, int dur3, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        duration_1 = dur1;
        duration_2 = dur2;
        duration_3 = dur3;
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        var retCode = Core.UltOsc(candles.High, candles.Low, candles.Close, 0, candleCount - 1, output, out _, out var outNbEle, duration_1,
            duration_2, duration_3);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
    }
    
    
    public UltimateOscillator(int dur1, int dur2, int dur3, List<IBinanceKline> candle) : base(candle)
    {
        duration_1 = dur1;
        duration_2 = dur2;
        duration_3 = dur3;
        
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        var retCode = Core.UltOsc(high, low, close, 0, candleCount - 1, output, out _, out var outNbEle, duration_1,
            duration_2, duration_3);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
    }

    public override StateAction GetStateAction()
    {
        var trend = GetTrendStyle();
        if (trend == TrendStyle.Upward && mainValue < 40) return StateAction.Buy;
        if (trend == TrendStyle.Downward && mainValue > 60) return StateAction.Sell;
        return StateAction.Neutral;
    }
}