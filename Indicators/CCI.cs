
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class CCI_TWENTY : TechnicalDataCollections
{
    public CCI_TWENTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var cciObj = new CCI(20, requestCandle[interval]);
                technicalDatas.Add(interval, cciObj);
            }
        }
    }

    protected override string indicatorName => "cci";
}

public class CCI : TechnicalDataBase
{
    private int duration;
    
    protected override string indicatorName => $"cci";
    
    
    public CCI(int dur, List<IBinanceKline> candles) : base(candles)
    {
        duration = dur;
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];

        var retCode = Core.Cci(high, low, close, 0, candleCount - 1, output, out _, out var outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
    }

    public override StateAction GetStateAction()
    {
        if (mainValue < -100 && GetTrendStyle() == TrendStyle.Upward) return StateAction.Buy;
        if (mainValue > 100 && GetTrendStyle() == TrendStyle.Downward) return StateAction.Sell;
        return StateAction.Neutral;
    }
}