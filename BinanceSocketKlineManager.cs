using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using ReadBinanceData.AsyncContainer;

public class BinanceSocketKlineManager : BinanceKlineManagerBase
{
    protected BinanceRestClient restconnect;
    protected BinanceSocketClient socketconnect;
    protected Dictionary<string, Dictionary<KlineInterval, Queue<IBinanceStreamKlineData>>>  receivedCandles;
    protected object lockobj;
    
    
    

    public BinanceSocketKlineManager(BinanceSocketClient socket, BinanceRestClient rest) : base()
    {
        socketconnect = socket;
        receivedCandles = new Dictionary<string, Dictionary<KlineInterval, Queue<IBinanceStreamKlineData>>>();
        foreach (var coinName in Defines.futureNames)
        {
            var dict = new Dictionary<KlineInterval, Queue<IBinanceStreamKlineData>>();
            receivedCandles.Add(coinName, dict);
            foreach (var interval in Defines.binanceInterval)
            {
                dict.Add(interval, new Queue<IBinanceStreamKlineData>());
            }
        }
        
        restconnect = rest;
        lockobj = new object();
    }
   
    
    /// <summary>
    /// 캔들 초기 데이터를 REST 형태로 요청한 뒤 웹소켓 리시버를 추가한다.
    /// </summary>
    /// <returns></returns>
    public override async Task<bool> Initialize()
    {
        if (isInitialize) return true;
        foreach (var coinName in Defines.futureNames)
        {
            candleDatas.Add(coinName, new Dictionary<KlineInterval, List<IBinanceKline>>());
            foreach (var interval in Defines.binanceInterval)
            {
                candleDatas[coinName].Add(interval, new List<IBinanceKline>());
            }
        }

        foreach (var coinName in Defines.futureNames)
        {
            Console.WriteLine($"{coinName}| Socket Connect");
            await socketconnect.UsdFuturesApi.ExchangeData
                .SubscribeToKlineUpdatesAsync(coinName, Defines.binanceInterval, onMsg =>
                {
                    lock (lockobj)
                    {
                        //소켓 응답을 받을 때 캔들데이터 큐에 추가
                        if (receivedCandles.TryGetValue(coinName, out var dict) &&
                            dict.TryGetValue(onMsg.Data.Data.Interval, out var queue))
                        {
                            queue.Enqueue(onMsg.Data);
                        }
                        
                    }
        
                }, false, ctx.GetCopyToken());
        }
        

        foreach (var coinName in Defines.futureNames)
        {
            foreach (var interval in Defines.binanceInterval)
            {   
                Console.WriteLine($"Rest {coinName}|{interval}");
                
                var list = candleDatas[coinName][interval];
                while (list.Count == 0)
                {
                    await UpdateFutureCandles(coinName, interval);
                }
                var first = list[0].OpenTime;
                var result = await restconnect.UsdFuturesApi.ExchangeData.GetKlinesAsync(coinName, interval,
                    endTime: first,
                    limit: 500, ct: ctx.GetCopyToken());
                var beforeklines = result.Data.ToList();
                beforeklines.Reverse();
                for (int i = 0; i < beforeklines.Count; ++i)
                {
                    if (beforeklines[i].OpenTime >= first)
                    {
                        continue;
                    }
                    list.Insert(0, beforeklines[i]);
                }
            }
            
        }


        //캔들 데이터 무결성 테스트
        
        
        isInitialize = true;
        Console.WriteLine("Binance Socket Conn is Initialized");
        return true;
    }

    /// <summary>
    /// 캔들 데이터를 업데이트 하는 함수
    /// </summary>
    public override async Task UpdateFutureCandles()
    {
        lock (lockobj)
        {
            foreach (var coinname in Defines.futureNames)
            {
                foreach (var interval in Defines.binanceInterval)
                {
                    if (receivedCandles.TryGetValue(coinname, out var dict) &&
                        dict.TryGetValue(interval, out var queue))
                    {
                        
                        //Queue 에 추가된 데이터를 캔들 데이터로 업데이트
                        while (queue.Count > 0)
                        {
                            var result = queue.Dequeue();
                            if (candleDatas.TryGetValue(result.Symbol, out var candledict))
                            {
                                if (candledict.TryGetValue(result.Data.Interval, out var list))
                                {
                                    while (list.Count > Defines.CANDLE_DAY_COUNT * 24 * 60)
                                    {
                                        list.RemoveAt(0);
                                    }

                                    if (list.Count > 0)
                                    {
                                        if (list[^1].OpenTime == result.Data.OpenTime)
                                        {
                                            list.Remove(list[^1]);
                                        }
                                    }

                                    list.Add(result.Data);
                                }
                            }
                        }     
                    }
                }
            }
        }
    }

    public override async Task UpdateFutureCandles(string coinName)
    {
        lock (lockobj)
        {
            foreach (var interval in Defines.binanceInterval)
            {
                if (receivedCandles.TryGetValue(coinName, out var dict) &&
                    dict.TryGetValue(interval, out var queue))
                {
                    while (queue.Count > 0)
                    {
                        var result = queue.Dequeue();
                        if (candleDatas.TryGetValue(result.Symbol, out var candledict))
                        {
                            if (candledict.TryGetValue(result.Data.Interval, out var list))
                            {
                                if (list.Count > Defines.CANDLE_DAY_COUNT * 24 * 60)
                                {
                                    list.RemoveAt(0);
                                }

                                if (list.Count > 0)
                                {
                                    if (list[^1].OpenTime == result.Data.OpenTime)
                                    {
                                        list.Remove(list[^1]);
                                    }
                                }

                                list.Add(result.Data);
                            }
                        }
                    }     
                }
            }
        }
    }

    public override async Task UpdateFutureCandles(string coinName, KlineInterval interval)
    {
        lock (lockobj)
        {
            if (receivedCandles.TryGetValue(coinName, out var dict) &&
                dict.TryGetValue(interval, out var queue))
            {
                while (queue.Count > 0)
                {
                    var result = queue.Dequeue();
                    if (candleDatas.TryGetValue(result.Symbol, out var candledict))
                    {
                        if (candledict.TryGetValue(result.Data.Interval, out var list))
                        {
                            if (list.Count > Defines.CANDLE_DAY_COUNT * 24 * 60)
                            {
                                list.RemoveAt(0);
                            }

                            if (list.Count > 0)
                            {
                                if (list[^1].OpenTime == result.Data.OpenTime)
                                {
                                    list.Remove(list[^1]);
                                }
                            }

                            list.Add(result.Data);
                        }
                    }
                }     
            }
        }
        
    }

    public override void Dispose()
    {
        ctx.Cancel();
        ctx.Dispose();
    }
    
    public override bool GetKlines(string coinName, bool usecurrentcandle, out Dictionary<KlineInterval, List<IBinanceKline>> candles)
    {
        candles = new Dictionary<KlineInterval, List<IBinanceKline>>();
        if (candleDatas.TryGetValue(coinName, out var srccandle))
        {
            foreach (var src in srccandle)
            {
                var list = new List<IBinanceKline>();
                list.AddRange(src.Value);
                var last = list[^1];
                if (last.CloseTime.AddMinutes(src.Key.ConvertKlineToMinute()*-1) < DateTime.UtcNow)
                {
                    //현재 만들어지는 캔들 이니까 마지막 캔들을 뺀다.
                    if (usecurrentcandle == false)
                    {
                        list.RemoveAt(list.Count-1);
                    }
                    
                }
                candles.Add(src.Key, list);
            }
            return true;
        }
        return false;
    }

}