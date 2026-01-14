using Binance.Net.Enums;
using Binance.Net.Interfaces;

public class IchimokuBL_TWENTYSIX : TechnicalDataCollections
{
    public IchimokuBL_TWENTYSIX(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.TryGetValue(interval, out var value))
            {
                var adxObj = new IchimokuBaseLine(value, 26);
                technicalDatas.Add(interval, adxObj);
            }
        }
    }
    protected override string indicatorName => "ichimoku_base_line";
}


public class IchimokuBaseLine : TechnicalDataBase
{
    private int duration_converse;
    
    public IchimokuBaseLine(List<IBinanceKline> klines, int dur) : base(klines)
    {
        var candleCount = klines.Count;
        duration_converse = 9;
        
        if (candleCount > dur && candleCount > duration_converse)
        {
            var close = candleDatas.Select(x => x.ClosePrice).TakeLast(dur).ToArray();
            var low = close.Min();
            var high = close.Max();

            mainValue = (low + high) / 2;
        }
        else
        {
            mainValue = 0M;
        }
        
    }

    protected override string indicatorName=> "ichimoku_base_line";

    public override StateAction GetStateAction()
    {
        var price = candleDatas[^1].ClosePrice;
        var baseline = mainValue;
        var close = candleDatas.Select(x => x.ClosePrice).TakeLast(duration_converse).ToArray();
        var low = close.Min();
        var high = close.Max();
        var converseline = (low + high) / 2;
        var spanA = (converseline + baseline) / 2;
        close = candleDatas.Select(x => x.ClosePrice).TakeLast(duration_converse * 2).ToArray();
        low = close.Min();
        high = close.Max();
        var spanB = (low + high) / 2;
        if (price > baseline && converseline > price && spanA > price && spanA > spanB) return StateAction.Buy;
        if (price < baseline && converseline < price && spanA < price && spanA < spanB) return StateAction.Sell;
        return StateAction.Neutral;
    }
}