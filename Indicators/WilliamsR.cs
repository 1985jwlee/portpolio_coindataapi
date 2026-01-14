using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class WilliamsR_FOURTEEN : TechnicalDataCollections
{
    public WilliamsR_FOURTEEN(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var ultObj = new WilliamsR(14, requestCandle[interval]);
                technicalDatas.Add(interval, ultObj);
            }
        }
    }

    protected override string indicatorName => "williams_r";
}

public class WilliamsR : TechnicalDataBase
{
    private  int duration;
    public decimal previousValue;
    
    protected override string indicatorName => $"williams_r";

    
    public WilliamsR(int dur, List<IBinanceKline> candle) : base(candle)
    {
        duration = dur;
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        var retCode = Core.WillR(high, low, close, 0, candleCount - 1, output, out _, out var outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
            previousValue = output[outNbEle - 2];
        }
    }

    public override StateAction GetStateAction()
    {
        var trend = GetTrendStyle();
        if (previousValue < -80 && mainValue > -80 && trend == TrendStyle.Upward) return StateAction.Buy;
        if (previousValue > -20 && mainValue < -20 && trend == TrendStyle.Downward) return StateAction.Sell;
        return StateAction.Neutral;
    }
}