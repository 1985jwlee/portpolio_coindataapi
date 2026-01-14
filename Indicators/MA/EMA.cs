using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class EMA_SEVEN : MACollection<EMA>
{
    public EMA_SEVEN(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 7)
    {
        
    }

    protected override string indicatorName => $"ema_7";
}

public class EMA_TWENTY : MACollection<EMA>
{
    public EMA_TWENTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 20)
    {
        
    }

    protected override string indicatorName => "ema_20";
}

public class EMA_THIRTY : MACollection<EMA>
{
    public EMA_THIRTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 30)
    {
    }

    protected override string indicatorName => "ema_30";
}

public class EMA_FIFTY : MACollection<EMA>
{
    public EMA_FIFTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 50)
    {
    }

    protected override string indicatorName => "ema_50";
}

public class EMA_NINETY : MACollection<EMA>
{
    public EMA_NINETY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 90)
    {
    }

    protected override string indicatorName => "ema_90";
}

public class EMA_TWOHUNDRED : MACollection<EMA>
{
    public EMA_TWOHUNDRED(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 200)
    {
    }

    protected override string indicatorName  => "ema_200";
}

public class EMA : MABase
{
    public decimal CurrentFrameEMA => mainValue;
    public decimal PrevFrameEMA { get; }
    

    public EMA(List<IBinanceKline> candle, int dur) : this(candle, IndicatorSourceType.CLOSE, dur)
    {
        
    }
    
    
    public EMA(List<IBinanceKline> candle, IndicatorSourceType srctype, int dur) : base(candle, dur)
    {
        if (dur > 1)
        {
            decimal[] input = null;
            switch (srctype)
            {
                case IndicatorSourceType.OPEN:
                    input = candleDatas.Select(x => x.OpenPrice).ToArray();
                    break;
                case IndicatorSourceType.CLOSE:
                    input = candleDatas.Select(x => x.ClosePrice).ToArray();
                    break;
                case IndicatorSourceType.OLHC4:
                    input = candleDatas.Select(x => (x.OpenPrice + x.LowPrice + x.HighPrice + x.ClosePrice) / 4).ToArray();
                    break;
            }
            var candleCount = candleDatas.Count;
            if (candleCount > dur)
            {
                var output = new decimal[candleCount];
                var retCode = Core.Ema(input, 0, candleCount - 1, output, out _, out var outNbElement, duration);
                if (retCode == Core.RetCode.Success)
                {
                    PrevFrameEMA = output[outNbElement - 2];
                    mainValue = (input[^1] - PrevFrameEMA) * (2M /(dur + 1)) + PrevFrameEMA;
                }
            }
            else
            {
                mainValue = 0M;
            }

            return;
        }
        
        switch (srctype)
        {
            case IndicatorSourceType.OPEN:
                var open = candleDatas.Select(x => x.OpenPrice).ToArray();
                PrevFrameEMA = open[^2];
                mainValue = open[^1];
                break;
            case IndicatorSourceType.CLOSE:
                
                var close = candleDatas.Select(x => x.ClosePrice).ToArray();
                PrevFrameEMA = close[^2];
                mainValue = close[^1];
                break;
            case IndicatorSourceType.OLHC4:
                var olhc4 = candleDatas.Select(x => (x.OpenPrice + x.LowPrice + x.HighPrice + x.ClosePrice) / 4).ToArray();
                PrevFrameEMA = olhc4[^2];
                mainValue = olhc4[^1];
                break;
        }

    }

    protected override string indicatorName => $"ema_{duration}";
}