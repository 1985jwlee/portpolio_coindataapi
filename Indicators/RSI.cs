
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class RSI_FOURTEEN : TechnicalDataCollections
{
    
    public RSI_FOURTEEN(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var rsiObj = new RSI(14, requestCandle[interval]);
                technicalDatas.Add(interval, rsiObj);
            }
        }
    }

    protected override string indicatorName => "rsi";
}

public class RSI : TechnicalDataBase
{
    private int duration;
    private decimal[] output;
    
    protected override string indicatorName => $"rsi";

    public RSI(int dur, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        duration = dur;
        var candleCount = candleDatas.Count;
        output = new decimal[candleCount];
        var retCode = Core.Rsi(candles.Close, 0, candleCount-1, output, out _, out var outNbEle,
            duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
    }
    
    public RSI(int dur, List<IBinanceKline> candle) : base(candle)
    {
        duration = dur;
        var closeDatas = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        output = new decimal[candleCount];
        var retCode = Core.Rsi(closeDatas, 0, candleCount-1, output, out _, out var outNbEle,
            duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
    }

    public override StateAction GetStateAction()
    {
        if (mainValue < 30 && GetTrendStyle() == TrendStyle.Upward) return StateAction.Buy;
        if (mainValue > 70 && GetTrendStyle() == TrendStyle.Downward) return StateAction.Sell;
        return StateAction.Neutral;
    }
}