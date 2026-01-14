
using Binance.Net.Enums;
using Binance.Net.Interfaces;

public interface IExchangeKlineManager<TCANDLE, TINTERVAL> : IDisposable, IInitializer
{
    bool GetKlines(string coinName, TINTERVAL interval, bool usecurrentcandle, out List<TCANDLE> candles);
    bool GetKlines(string coinName, bool usecurrentcandle, out Dictionary<TINTERVAL, List<TCANDLE>> candles);
    Task UpdateFutureCandles();
    Task UpdateFutureCandles(string coinName);

    Task UpdateFutureCandles(string coinName, TINTERVAL interval);

    bool ValidateCandleData(string coinName);

    bool ValidateCandleData(List<TCANDLE> candles);
}

public interface IBinanceExchangeKlineManager : IExchangeKlineManager<IBinanceKline, KlineInterval>{}



public abstract class BinanceKlineManagerBase : IBinanceExchangeKlineManager
{
    protected CancellationTokenSource ctx;
    protected bool isInitialize;
    protected Dictionary<string, Dictionary<KlineInterval, List<IBinanceKline>>> candleDatas;

    public BinanceKlineManagerBase()
    {
        ctx = new CancellationTokenSource();
        isInitialize = false;
        candleDatas = new Dictionary<string, Dictionary<KlineInterval, List<IBinanceKline>>>();
    }
    public abstract void Dispose();

    public  bool GetKlines(string coinName,  KlineInterval interval,bool usecurrentcandle, out List<IBinanceKline> candles)
    {
        candles = null;
        if (!GetKlines(coinName, usecurrentcandle,  out var dict)) { return false;}
        return dict.TryGetValue(interval, out candles);
    }

    public virtual bool GetKlines(string coinName, bool usecurrentcandle, out Dictionary<KlineInterval, List<IBinanceKline>> candles)
    {
        return candleDatas.TryGetValue(coinName, out candles);
    }

    public abstract Task<bool> Initialize();

    public abstract Task UpdateFutureCandles();

    public abstract Task UpdateFutureCandles(string coinName);

    public abstract Task UpdateFutureCandles(string coinName, KlineInterval interval);

    public bool ValidateCandleData(string coinName)
    {
        if (candleDatas.TryGetValue(coinName, out var dict) == false)
        {
            return false;
        }

        foreach (var interval in Defines.binanceInterval)
        {
            if (dict.TryGetValue(interval, out var list) == false) return false;
            if (ValidateCandleData(list) == false) return false;
        }

        return true;
    }

    public bool ValidateCandleData(List<IBinanceKline> candles)
    {
        const decimal reasonablegap = 0.05M;
        for (int i = 0; i < candles.Count - 1; ++i)
        {
            var close = candles[i].ClosePrice;
            var nextopen = candles[i + 1].OpenPrice;

            var candlegap = (nextopen - close) / close;
            if (candlegap > reasonablegap || candlegap <- reasonablegap)
            {
                return false;
            }
        }

        return true;
    }
}