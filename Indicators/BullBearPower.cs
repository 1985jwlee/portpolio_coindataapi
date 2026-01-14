using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class BBP : TechnicalDataCollections
{
    public BBP(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var bboObj = new BullBearPower(requestCandle[interval]);
                technicalDatas.Add(interval, bboObj);
            }
        }
    }

    protected override string indicatorName => "bull_bear_power";
}

public class BullBearPower : TechnicalDataBase
{
    private decimal bullpower;
    private decimal bearpower;
    private decimal prevbullpower;
    private decimal prevbearpower;
    
    protected override string indicatorName => $"bull_bear_power";

    public BullBearPower(BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        var candleCount = candleDatas.Count;
        var ema = new decimal[candleCount];
        var currentHigh = candles.High[candleCount-1];
        var currentLow = candles.Low[candleCount - 1];
        var prevHigh = candles.High[candleCount-2];
        var prevLow = candles.Low[candleCount - 2];
        var retCode = Core.Ema(candles.Close, 0, candleCount - 1, ema, out _, out var nbElement, 13);
        if (retCode == Core.RetCode.Success)
        {
            bullpower = currentHigh - ema[nbElement - 1];
            bearpower = currentLow - ema[nbElement - 1];
            mainValue = bullpower + bearpower;
            prevbullpower = prevHigh - ema[nbElement - 2];
            prevbearpower = prevLow - ema[nbElement - 2];
        }
        
    }
    
    public BullBearPower(List<IBinanceKline> candles) : base(candles)
    {
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var candleCount = candleDatas.Count;
        var ema = new decimal[candleCount];
        var currentHigh = high[candleCount-1];
        var currentLow = low[candleCount - 1];
        var prevHigh = high[candleCount-2];
        var prevLow = low[candleCount - 2];
        var retCode = Core.Ema(close, 0, candleCount - 1, ema, out _, out var nbElement, 13);
        if (retCode == Core.RetCode.Success)
        {
            bullpower = currentHigh - ema[nbElement - 1];
            bearpower = currentLow - ema[nbElement - 1];
            mainValue = bullpower + bearpower;
            prevbullpower = prevHigh - ema[nbElement - 2];
            prevbearpower = prevLow - ema[nbElement - 2];
        }
    }

    public override StateAction GetStateAction()
    {
        var trend = GetTrendStyle();
        var bullbearpower = bullpower + bearpower;
        if (trend == TrendStyle.Upward && bearpower < 0 && (bearpower > prevbearpower)) return StateAction.Buy;
        if (trend == TrendStyle.Downward && bullpower > 0 && (bullpower < prevbullpower)) return StateAction.Sell;
        return StateAction.Neutral;
    }
}