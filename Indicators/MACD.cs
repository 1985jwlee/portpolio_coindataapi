
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using TALib;

public class MACD_TWELVE_TWENTYSIX_SIGNINE : TechnicalDataCollections
{
    public MACD_TWELVE_TWENTYSIX_SIGNINE(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle)
    {
        foreach (var interval in Defines.binanceInterval)
        {
            if (requestCandle.ContainsKey(interval))
            {
                var macdObj = new MACD(12, 26, 9, IndicatorSourceType.CLOSE, requestCandle[interval]);
                technicalDatas.Add(interval, macdObj);
            }
        }
    }

    protected override string indicatorName => "macd";
}

public class MACD : TechnicalDataBase
{
    protected override string indicatorName => $"macd";
    private int shortTerm;
    private int longTerm;
    private int sigTerm;

    public decimal CurrentFrameMACD => mainValue;
    public decimal PrevFrameMACD { get; set; }
    public decimal CurrentFrameSig { get; set; }
    public decimal PrevFrameSig { get; set; }
    
    public decimal CurrentFrameHisto { get; set; }
    public decimal PrevFrameHisto { get; set; }
    
    public decimal[] HistogramSeries { get; set; }
    public decimal[] MACDSeries { get; set; }

    public MACD(int st, int lt, int sigdur, IndicatorSourceType srctype, BinanceKlineDataToDecimal candles) : base(candles.source)
    {
        shortTerm = st;
        longTerm = lt;
        sigTerm = sigdur;
        var candleCount = candles.CandleCount;
        MACDSeries = new decimal[candleCount];
        var sigseries = new decimal[candleCount];
        HistogramSeries = new decimal[candleCount];
        decimal[] input = null;
        switch (srctype)
        {
            case IndicatorSourceType.OPEN:
                input = candles.Open;
                break;
            case IndicatorSourceType.CLOSE:
                input = candles.Close;
                break;
            
            case IndicatorSourceType.OLHC4:
                input = candles.source.Select(x => (x.OpenPrice + x.LowPrice + x.HighPrice + x.ClosePrice) / 4).ToArray();
                break;
            
        }
        var retCode = Core.Macd(input, 0, candleCount - 1, MACDSeries, sigseries, HistogramSeries, out _, out var outNbEle,
            shortTerm, longTerm, sigTerm);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = MACDSeries[outNbEle - 1];
            PrevFrameMACD = MACDSeries[outNbEle - 2];
            CurrentFrameSig = sigseries[outNbEle - 1];
            PrevFrameSig = sigseries[outNbEle - 2];
            CurrentFrameHisto = HistogramSeries[outNbEle - 1];
            PrevFrameHisto = HistogramSeries[outNbEle - 2];
        }
    }


    public MACD(int st, int lt, int sigdur, IndicatorSourceType srctype, List<IBinanceKline> candles) : base(candles)
    {
        shortTerm = st;
        longTerm = lt;
        sigTerm = sigdur;
        var candleCount = candles.Count;
        MACDSeries = new decimal[candleCount];
        var sigseries = new decimal[candleCount];
        HistogramSeries = new decimal[candleCount];
        
        decimal[] input = null;
        switch (srctype)
        {
            case IndicatorSourceType.OPEN:
                input = candles.Select(x => x.OpenPrice).ToArray();
                break;
            case IndicatorSourceType.CLOSE:
                input = candles.Select(x => x.ClosePrice).ToArray();
                break;
            
            case IndicatorSourceType.OLHC4:
                input = candles.Select(x => (x.OpenPrice + x.LowPrice + x.HighPrice + x.ClosePrice) / 4).ToArray();
                break;
        }
        
        var retCode = Core.Macd(input, 0, candleCount - 1, MACDSeries, sigseries, HistogramSeries, out _, out var outNbEle,
            shortTerm, longTerm, sigTerm);
        if (retCode == Core.RetCode.Success)
        {
            mainValue = MACDSeries[outNbEle - 1];
            PrevFrameMACD = MACDSeries[outNbEle - 2];
            CurrentFrameSig = sigseries[outNbEle - 1];
            PrevFrameSig = sigseries[outNbEle - 2];
            CurrentFrameHisto = HistogramSeries[outNbEle - 1];
            PrevFrameHisto = HistogramSeries[outNbEle - 2];
        }
    }

    public override StateAction GetStateAction()
    {
        
        //MACD가 시그널 상향 돌파 시 롱 주문 진입
        //빨녹분녹으로 찍힐 때 롱 시그널 준다.
        if (CurrentFrameHisto > 0) return StateAction.Buy;
        if (CurrentFrameHisto < 0) return StateAction.Sell;
        
        // if (CurrentFrameHisto > 0 && CurrentFrameHisto > PrevFrameHisto)
        // {
        //     return StateAction.Buy;
        // }
        //
        // //녹빨, 연빨 등으로 찍힐 때 숏 시그널 준다.
        // if (CurrentFrameHisto < 0 && CurrentFrameHisto < PrevFrameHisto)
        // {
        //     return StateAction.Sell;
        // }
        
        return StateAction.Neutral;
    }

    public override string ToString()
    {
        return $"{PrevFrameHisto:N2},{CurrentFrameHisto:N2}";
    }

}
