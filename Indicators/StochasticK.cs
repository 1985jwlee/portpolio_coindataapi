
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class StochasticK_FOURTEEN_THREE_THREE : TechnicalDataCollections
{
    public StochasticK_FOURTEEN_THREE_THREE(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var stochObj = new StochasticK(14, 3, 3, requestCandle[interval]);
                technicalDatas.Add(interval, stochObj);
            }
        }
    }

    protected override string indicatorName => "stoch_k";
}

public class StochasticK : TechnicalDataBase
{
    private int durationFastK;
    private int durationSlowK;
    private int durationD;
    private decimal signalValue;
    
    protected override string indicatorName => $"stoch_k";

    public StochasticK(int fk, int sk, int d, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        durationFastK = fk;
        durationSlowK = sk;
        durationD = d;
        var candleCount = candleDatas.Count;
        var k_value = new decimal[candleCount];
        var d_value = new decimal[candleCount];
        var matype = Core.MAType.Ema;
        var retCode = Core.Stoch(candles.High, candles.Low, candles.Close, 0, candleCount - 1, k_value, d_value, out _, 
            out var outNbElem, matype, matype, 
            durationFastK, durationSlowK, durationD);
        if (retCode == Core.RetCode.Success)
        {
            var idx = outNbElem - 1;
            mainValue = k_value[idx];
            signalValue = d_value[idx];
        }
    }
    
    public StochasticK(int fk, int sk, int d, List<IBinanceKline> candle) : base(candle)
    {
        durationFastK = fk;
        durationSlowK = sk;
        durationD = d;
        
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var k_value = new decimal[candleCount];
        var d_value = new decimal[candleCount];
        var matype = Core.MAType.Ema;
        var retCode = Core.Stoch(high, low, close, 0, candleCount - 1, k_value, d_value, out _, 
            out var outNbElem, matype, matype, 
            durationFastK, durationSlowK, durationD);
        if (retCode == Core.RetCode.Success)
        {
            var idx = outNbElem - 1;
            mainValue = k_value[idx];
            signalValue = d_value[idx];
        }
    }

    public override StateAction GetStateAction()
    {
        var trend = GetTrendStyle();
        if (trend == TrendStyle.Upward && mainValue < 20 && mainValue > signalValue) return StateAction.Buy;
        if (trend == TrendStyle.Downward && mainValue > 80 && mainValue < signalValue) return StateAction.Sell;
        return StateAction.Neutral;
    }
}