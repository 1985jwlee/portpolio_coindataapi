
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class StochasticFastK_FOURTEEN_THREE : TechnicalDataCollections
{
    public StochasticFastK_FOURTEEN_THREE(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var stochObj = new StochasticRSIFast(14, 3, 3, 14, requestCandle[interval]);
                technicalDatas.Add(interval, stochObj);
            }
        }
    }

    protected override string indicatorName => "stoch_rsi_k";
}

public class StochasticRSIFast : TechnicalDataBase
{
    private int fastKduration;
    
    private int slowKduration;
    private int fastDduration;
    private int rsiDuration;

    private decimal[] fastK;
    private decimal[] fastD;

    private decimal kline;
    private decimal dline;
    
    protected override string indicatorName => $"stoch_rsi_k";
    

    public StochasticRSIFast(int fk, int sk, int d, int rsidur, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        fastKduration = fk;
        slowKduration = sk;
        fastDduration = d;
        rsiDuration = rsidur;
        
        var candleCount = candleDatas.Count;
        

        var rsi = new decimal[candleCount];

        var retCode = Core.Rsi(candles.Close, 0, candleCount - 1, rsi, out _, out var rsinbEle, rsiDuration);
        if (retCode != Core.RetCode.Success) return;
        var lastIdx = rsinbEle - 1;
        var usersi = rsi.Slice(0, lastIdx).ToArray();
        var rsicount = usersi.Length;
        fastK = new decimal[rsicount];
        fastD = new decimal[rsicount];
        retCode = Core.Stoch(usersi, usersi, usersi, 0, rsicount - 1, fastK, fastD, out _, out var nbele,
            Core.MAType.Sma, Core.MAType.Sma,
            fastKduration, slowKduration, fastDduration);
        if (retCode != Core.RetCode.Success) return;

        kline = fastK[nbele - 1];
        dline = fastD[nbele - 1];
        mainValue = kline;
    }
    
    /// <summary>
    /// 라이브러리에서 스토캐스틱 RSI 계산 방식이 트레이딩뷰 방식과 달라서 개별 구현함
    /// </summary>
    /// <param name="fk"></param>
    /// <param name="sk"></param>
    /// <param name="d"></param>
    /// <param name="rsidur"></param>
    /// <param name="candle"></param>
    public StochasticRSIFast(int fk, int sk, int d, int rsidur, List<IBinanceKline> candle) : base(candle)
    {
        fastKduration = fk;
        slowKduration = sk;
        fastDduration = d;
        rsiDuration = rsidur;
        
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        

        var rsi = new decimal[candleCount];

        var retCode = Core.Rsi(close, 0, candleCount - 1, rsi, out _, out var rsinbEle, rsiDuration);
        if (retCode != Core.RetCode.Success) return;
        var lastIdx = rsinbEle - 1;
        var usersi = rsi.Slice(0, lastIdx).ToArray();
        var rsicount = usersi.Length;
        fastK = new decimal[rsicount];
        fastD = new decimal[rsicount];
        retCode = Core.Stoch(usersi, usersi, usersi, 0, rsicount - 1, fastK, fastD, out _, out var nbele,
            Core.MAType.Sma, Core.MAType.Sma,
            fastKduration, rsiDuration, fastDduration);
        if (retCode != Core.RetCode.Success) return;

        kline = fastK[nbele - 1];
        dline = fastD[nbele - 1];
        mainValue = kline;
    }

    public override StateAction GetStateAction()
    {
        if (kline > 80 || dline > 80) return StateAction.Sell;
        if (kline < 20 || dline < 20) return StateAction.Sell;
        
        //K가 아래에서 D를 위로 크로스 롱
        // if (klineseries[0] < dlineseries[0] && klineseries[1] > dlineseries[1])
        // {
        //     return StateAction.Buy;
        // }
        //K가 위에서 D를 아래로 크로스 숏
        // if (klineseries[0] > dlineseries[0] && klineseries[1] < dlineseries[1])
        // {
        //     return StateAction.Sell;
        // }
        
        // if (kline > 80 && dline > 80 && dline > kline) return StateAction.Sell;
        // if (kline < 20 && dline < 20 && kline > dline) return StateAction.Buy;
        //
        return StateAction.Neutral;
    }

    public override string ToString()
    {
        return $"Stoch K : {kline} D : {dline}";
    }
}