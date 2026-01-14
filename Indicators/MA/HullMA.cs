using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class HMA_NINE : MACollection<HullMA>
{
    public HMA_NINE(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 9)
    {
    }

    protected override string indicatorName => "hull";
}

public class HullMA : MABase
{
    public HullMA(List<IBinanceKline> candle, int dur) : base(candle, dur)
    {
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        if (candleCount > dur)
        {
            var wma = new decimal[candleCount];
            var retCode = Core.Wma(close, 0, candleCount - 1, wma, out _, out var outNbElement, duration);
            if (retCode != Core.RetCode.Success) return;
            var halfwma = new decimal[candleCount];
            retCode = Core.Wma(close, 0, candleCount - 1, halfwma, out _, out outNbElement, duration / 2);
            if (retCode != Core.RetCode.Success) return;
            var sqrtwma = new decimal[candleCount - duration + 1];
            for (int i = 0; i < sqrtwma.Length; ++i)
            {
                sqrtwma[i] = 2 * halfwma[i] - wma[i];
            }

            var hma = new decimal[sqrtwma.Length];
            retCode = Core.Wma(sqrtwma, 0, sqrtwma.Length - 1, hma, out _, out outNbElement, (int)Math.Sqrt(duration));
            if (retCode == Core.RetCode.Success)
            {
                mainValue = hma[outNbElement - 1];
            }
        }
        else
        {
            mainValue = 0M;
        }
        
    }

    public override StateAction GetStateAction()
    {
        var candleCount = candleDatas.Count;
        if (candleCount <= duration)
        {
            return StateAction.Neutral;
        }

        var lastcandle = candleDatas[^1];
        
        if (lastcandle.ClosePrice > mainValue && GetTrendStyle() == TrendStyle.Upward)
        {
            return StateAction.Buy;
        }

        if (lastcandle.ClosePrice < mainValue && GetTrendStyle() == TrendStyle.Downward)
        {
            return StateAction.Sell;
        }

        return StateAction.Neutral;
    }
    
    protected override string indicatorName => $"hull";
}