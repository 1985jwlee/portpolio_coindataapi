using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;


public class AO : TechnicalDataCollections
{
    public AO(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var aoObj = new AwesomeOscillator(requestCandle[interval]);
                technicalDatas.Add(interval, aoObj);
            }
        }
    }

    protected override string indicatorName => "awesome";
}

public class AwesomeOscillator : TechnicalDataBase
{
    protected override string indicatorName => "awesome";
    private decimal saucer;
    
    public AwesomeOscillator(List<IBinanceKline> candles) : base(candles)
    {
        var medianprice = candles.Select(x => (x.HighPrice + x.LowPrice) / 2).ToArray();
        var candleCount = candleDatas.Count;
        var sma5 = new decimal[candleCount];
        var sma34 = new decimal[candleCount];
        var ao = new decimal[candleCount];
        Core.Sma(medianprice, 0, candleCount - 1, sma5, out var begin1, out var numElements1, 5);
        Core.Sma(medianprice, 0, candleCount-1, sma34, out var begin2, out var numElements2, 34);
        var shiftsma5 = sma5.ShiftDefaultValues(begin1);
        var shiftsma34 = sma34.ShiftDefaultValues(begin2);
        for (int i = 0; i < candleCount; ++i)
        {
            ao[i] = shiftsma5[i] - shiftsma34[i];
        }
        
        mainValue = ao[^1];
        saucer = ao[DetectSaucerPattern(ao)[^1]];
    }
    
    

    public override StateAction GetStateAction()
    {
        if (mainValue > 0 && saucer > 0) return StateAction.Buy;
        if (mainValue < 0 && saucer < 0) return StateAction.Sell;
        return StateAction.Neutral;
    }
    
    private List<int> DetectSaucerPattern(decimal[] aoValues)
    {
        var saucerSignals = new List<int>();

        for (int i = 2; i < aoValues.Length; i++)
        {
            // 제로선 위에서 Saucer 패턴 탐지
            if (aoValues[i] > 0 && aoValues[i - 1] < aoValues[i] && aoValues[i - 2] > aoValues[i - 1])
            {
                saucerSignals.Add(i);
            }
            // 제로선 아래에서 Saucer 패턴 탐지
            else if (aoValues[i] < 0 && aoValues[i - 1] > aoValues[i] && aoValues[i - 2] < aoValues[i - 1])
            {
                saucerSignals.Add(i);
            }
        }
        return saucerSignals;
    }
    
}