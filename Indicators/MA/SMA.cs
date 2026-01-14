
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class SMA_SEVEN : MACollection<SMA>
{
    public SMA_SEVEN(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 7)
    {
    }

    protected override string indicatorName => "sma_7";
}

public class SMA_TWENTY : MACollection<SMA>
{
    public SMA_TWENTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 20)
    {
    }

    protected override string indicatorName  => "sma_20";
}

public class SMA_THIRTY : MACollection<SMA>
{
    public SMA_THIRTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 30)
    {
    }

    protected override string indicatorName  => "sma_30";
}

public class SMA_FIFTY : MACollection<SMA>
{
    public SMA_FIFTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 50)
    {
    }

    protected override string indicatorName  => "sma_50";
}

public class SMA_NINETY : MACollection<SMA>
{
    public SMA_NINETY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 90)
    {
    }

    protected override string indicatorName  => "sma_90";
}

public class SMA_TWOHUNDRED : MACollection<SMA>
{
    public SMA_TWOHUNDRED(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 200)
    {
    }

    protected override string indicatorName  => "sma_200";
}

public class SMA: MABase
{
    public SMA(BinanceKlineDataToDecimal candles, int dur) : base(candles.source, dur)
    {
        var candleCount = candleDatas.Count;
        if (candleCount > dur)
        {
            var output = new decimal[candleCount];
            var retCode = Core.Sma(candles.Close, 0, candleCount - 1, output, out _, out var outNbElement, duration);
            if (retCode == Core.RetCode.Success)
            {
                mainValue = output[outNbElement - 1];
            }
        }
        else
        {
            mainValue = 0M;
        }
    }
    
    public SMA(List<IBinanceKline> candle, int dur) : base(candle, dur)
    {
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        if (candleCount > dur)
        {
            var output = new decimal[candleCount];
            var retCode = Core.Sma(close, 0, candleCount - 1, output, out _, out var outNbElement, duration);
            if (retCode == Core.RetCode.Success)
            {
                mainValue = output[outNbElement - 1];
            }
        }
        else
        {
            mainValue = 0M;
        }
    }
    
    protected override string indicatorName => $"sma_{duration}";

}