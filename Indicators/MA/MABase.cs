using Binance.Net.Enums;
using Binance.Net.Interfaces;

public abstract class MABase : TechnicalDataBase
{
    protected int duration;


    public MABase(List<IBinanceKline> candle, int dur) : base(candle)
    {
        duration = dur;
    }
    
    public override StateAction GetStateAction()
    {
        var candleCount = candleDatas.Count;
        if (candleCount <= duration)
        {
            return StateAction.Neutral;
        }

        var lastcandle = candleDatas[^1];
        
        if (lastcandle.ClosePrice > mainValue 
            //&& GetTrendStyle() == TrendStyle.Upward
            )
        {
            return StateAction.Buy;
        }

        if (lastcandle.ClosePrice < mainValue 
            //&& GetTrendStyle() == TrendStyle.Downward
            )
        {
            return StateAction.Sell;
        }

        return StateAction.Neutral;
    }
}

public class MACollection<T> : TechnicalDataCollections where T : MABase
{
    public MACollection(Dictionary<KlineInterval, List<IBinanceKline>> candle, int duration) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                T obj = (T)Activator.CreateInstance(typeof(T), candle[interval], duration);
                technicalDatas.Add(interval, obj);
            }
        }
    }

    protected override string indicatorName { get; }
}