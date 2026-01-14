using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;
public class ADI_FOURTEEN : TechnicalDataCollections
{
    public ADI_FOURTEEN(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.TryGetValue(interval, out var value))
            {
                var adxObj = new ADI(14, value);
                technicalDatas.Add(interval, adxObj);
            }
        }
    }

    protected override string indicatorName => "adx";
}

public class ADI : TechnicalDataBase
{
    private int duration;
    private decimal plusDI;
    private decimal minusDI;

    public ADI(int dur, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        duration = dur;
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        var retCode = Core.Adx(candles.High, candles.Low, candles.Close, 0, candleCount - 1, output, out _, out var outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }
        var plusdi = new decimal[candleCount];
        retCode = Core.PlusDI(candles.High, candles.Low, candles.Close, 0, candleCount - 1, plusdi, out _, out outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            plusDI = plusdi[outNbEle - 1];
        }
        var minusdi = new decimal[candleCount];
        retCode = Core.MinusDI(candles.High, candles.Low, candles.Close, 0, candleCount - 1, minusdi, out _, out outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            minusDI = minusdi[outNbEle - 1];
        }
    }

    public ADI(int dur, List<IBinanceKline> klines) : base(klines)
    {
        duration = dur;
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        var retCode = Core.Adx(high, low, close, 0, candleCount - 1, output, out _, out var outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = output[outNbEle - 1];
        }

        var plusdi = new decimal[candleCount];
        retCode = Core.PlusDI(high, low, close, 0, candleCount - 1, plusdi, out _, out outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            plusDI = plusdi[outNbEle - 1];
        }
        var minusdi = new decimal[candleCount];
        retCode = Core.MinusDI(high, low, close, 0, candleCount - 1, minusdi, out _, out outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            minusDI = minusdi[outNbEle - 1];
        }
    }

    public override StateAction GetStateAction()
    {
        if (mainValue > 20 && plusDI > minusDI) return StateAction.Buy;
        if (mainValue > 20 && plusDI < minusDI) return StateAction.Sell;
        return StateAction.Neutral;
    }
    
    protected override string indicatorName => "adx";
}