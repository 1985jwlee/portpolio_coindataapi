
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class Momentum_TEN : TechnicalDataCollections
{
    public Momentum_TEN(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var momObj = new Momentum(10, requestCandle[interval]);
                technicalDatas.Add(interval, momObj);
            }
        }
    }

    protected override string indicatorName => "momentum";
}

public class Momentum: TechnicalDataBase
{
    private int duration;
    private decimal momentumSMA;
    
    protected override string indicatorName => $"momentum";
    

    public Momentum(int dur, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        duration = dur;
        var candleCount = candleDatas.Count;
        var momentumseries = new decimal[candleCount];
        var retCode = Core.Mom(candles.Close, 0, candleCount - 1, momentumseries, out _, out var outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = momentumseries[outNbEle - 1];
            momentumseries = momentumseries.ShiftDefaultValues(duration);
            var momentumSMAs = new decimal[momentumseries.Length];
            retCode = Core.Sma(momentumseries, 0, momentumseries.Length - 1, momentumSMAs, out _, out outNbEle, 14);
            if (retCode == Core.RetCode.Success)
            {
                momentumSMA = momentumSMAs[outNbEle - 1];
            }
        }
    }

    public Momentum(int dur, List<IBinanceKline> candles) : base(candles)
    {
        duration = dur;
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var momentumseries = new decimal[candleCount];
        var retCode = Core.Mom(close, 0, candleCount - 1, momentumseries, out _, out var outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = momentumseries[outNbEle - 1];
            momentumseries = momentumseries.ShiftDefaultValues(duration);
            var momentumSMAs = new decimal[momentumseries.Length];
            retCode = Core.Sma(momentumseries, 0, momentumseries.Length - 1, momentumSMAs, out _, out outNbEle, 14);
            if (retCode == Core.RetCode.Success)
            {
                momentumSMA = momentumSMAs[outNbEle - 1];
            }
        }
    }

    public override StateAction GetStateAction()
    {
        var trend = GetTrendStyle();
        
        if (trend == TrendStyle.Upward && (mainValue > 0 || mainValue > momentumSMA)) return StateAction.Buy;
        if (trend == TrendStyle.Downward && (mainValue < 0 || mainValue < momentumSMA)) return StateAction.Sell;
        return StateAction.Neutral;
    }
}