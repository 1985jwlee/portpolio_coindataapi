using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Newtonsoft.Json.Linq;

public struct DivergenceInfo
{
    public int previousIndex;
    public int currentIndex;
    public TrendStyle trendStyle;
}

public interface ITechnicalData
{
    decimal mainValue { get; }
    StateAction GetStateAction();
    TrendStyle GetTrendStyle();
}

public class BinanceKlineDataToDecimal
{
    public List<IBinanceKline> source { get; }
    public decimal[] Open { get; }
    public decimal[] High { get; }
    public decimal[] Low { get; }
    public decimal[] Close { get; }
    public decimal[] Volumes { get; }

    public int CandleCount { get; }

    public BinanceKlineDataToDecimal(List<IBinanceKline> kline)
    {
        source = kline;
        CandleCount = source.Count;
        Open = kline.Select(x => x.OpenPrice).ToArray();
        High = kline.Select(x => x.HighPrice).ToArray();
        Low = kline.Select(x => x.LowPrice).ToArray();
        Close = kline.Select(x => x.ClosePrice).ToArray();
        Volumes = kline.Select(x => x.Volume).ToArray();
    }
}

public abstract class TechnicalDataBase : ITechnicalData
{
    protected List<IBinanceKline> candleDatas;
    public decimal mainValue { get; protected set; }
    
    protected abstract string indicatorName { get; }

    public TechnicalDataBase() => candleDatas = new List<IBinanceKline>();
    
    public TechnicalDataBase(List<IBinanceKline> candle)
    {
        candleDatas = candle;
    }

    public virtual StateAction GetStateAction()
    {
        return StateAction.Neutral;
    }

    public virtual TrendStyle GetTrendStyle()
    {
        var gap = candleDatas[^1].ClosePrice - candleDatas[^2].ClosePrice;
        if (Math.Abs(gap) / candleDatas[^1].ClosePrice < 0.001M) return TrendStyle.Neutral; 
        
        if (gap > 0) return TrendStyle.Upward;
        
        if (gap < 0) return TrendStyle.Downward;

        return TrendStyle.Neutral;
    }
    
    public virtual string MakeIndicatorData()
    {
        JObject jObject = new JObject();
        jObject.Add("name", indicatorName);
        var value = mainValue;
        jObject.Add("value", value);
        var action = GetStateAction();
        jObject.Add("action", action.ToString());
        var ret = jObject.ToString();
        return ret;
    }
}



public interface ITechnicalDataCollections
{
    string MakeIndicatorData(KlineInterval interval);
    StateAction GetStateAction(KlineInterval interval, decimal value);
    decimal GetIndicator(KlineInterval interval);
    
    
}

public abstract class TechnicalDataCollections : ITechnicalDataCollections
{
    protected abstract string indicatorName { get; }
    
    protected Dictionary<KlineInterval, List<IBinanceKline>> requestCandle;
    protected IDictionary<KlineInterval, ITechnicalData> technicalDatas { get; } = new Dictionary<KlineInterval, ITechnicalData>();

    
    
    public TechnicalDataCollections(Dictionary<KlineInterval, List<IBinanceKline>> candle)
    {
        requestCandle = candle;
    }

    

    public decimal GetIndicator(KlineInterval interval)
    {
        if (technicalDatas.TryGetValue(interval, out var data))
        {
            return data.mainValue;
        }

        return 0;
    }

    public StateAction GetStateAction(KlineInterval interval, decimal value)
    {
        if (technicalDatas.TryGetValue(interval, out var v))
        {
            return v.GetStateAction();
        }

        return StateAction.Neutral;
    }

    public virtual string MakeIndicatorData(KlineInterval interval)
    {
        JObject jObject = new JObject();
        jObject.Add("name", indicatorName);
        var value = GetIndicator(interval);
        jObject.Add("value", value);
        var action = GetStateAction(interval, value);
        jObject.Add("action", action.ToString());
        var ret = jObject.ToString();
        return ret;
    }
}