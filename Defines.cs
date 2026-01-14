using Binance.Net.Enums;

public static class Defines
{

    
     
    public const int CANDLE_DAY_COUNT = 10;

    
    // public const decimal MINIMAL_SLOPECHECK_RATIO = 0.002M;
    // public const int CANDLE_GROUP_COUNT = 10;
    // public const int INITIAL_CANDLE_GROUPS = 7; 
    
    public static List<KlineInterval> binanceInterval = new List<KlineInterval>()
    {
        KlineInterval.OneMinute, KlineInterval.ThreeMinutes, KlineInterval.FiveMinutes, KlineInterval.FifteenMinutes, KlineInterval.ThirtyMinutes,
        KlineInterval.OneHour, KlineInterval.TwoHour, KlineInterval.FourHour, 
        KlineInterval.OneDay, KlineInterval.OneWeek, KlineInterval.OneMonth,
    };

    

    public static List<string> futureNames = new List<string>()
    {
        "ALGOUSDT", "GRTUSDT", "ADAUSDT", "ETHUSDT", "BNBUSDT", "BTCUSDT", "XLMUSDT", "AUSDT", "SANDUSDT",
        "BCHUSDT", "ETCUSDT", "AVAXUSDT", "NEARUSDT", "ATOMUSDT", "SOLUSDT", "LTCUSDT", "TRXUSDT", "POLUSDT",
        "DOTUSDT", "FILUSDT", "XRPUSDT", "DOGEUSDT", "AAVEUSDT", "LINKUSDT"
    };


}

public static class ToolScript
{
    
    
    public static T[] ShiftDefaultValues<T>(this IList<T> array, int shiftCount)
    {
        var length = array.Count;
        var result = new T[length];

        // 뒤쪽의 0을 제거하고 앞쪽에 추가
        for (int i = 0; i < length; i++)
        {
            if (i < shiftCount)
            {
                result[i] = default(T)!;
            }
            else
            {
                result[i] = array[i - shiftCount];
            }
        }

        return result;
    }
    
        
    public static List<T> Slice<T>(this IList<T> inputList, int startIndex, int endIndex)
    { 
        int elementCount = endIndex-startIndex + 1;
        return inputList.Skip(startIndex).Take(elementCount).ToList();
    }
    
    public static KlineInterval ConvertToInterval(string key)
    {
        switch (key)
        {
            case "1m": return KlineInterval.OneMinute;
            case "5m": return KlineInterval.FiveMinutes;
            case "15m": return KlineInterval.FifteenMinutes;
            case "30m": return KlineInterval.ThirtyMinutes;
            case "1h": return KlineInterval.OneHour;
            case "2h": return KlineInterval.TwoHour;
            case "4h": return KlineInterval.FourHour;
            case "1d": return KlineInterval.OneDay;
            case "1w": return KlineInterval.OneWeek;
            case "1M": return KlineInterval.OneMonth;
        }

        return KlineInterval.OneMinute;
    }
    
    public static CancellationToken GetCopyToken(this CancellationTokenSource ctx)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(ctx.Token).Token;
    }
    
    
    public static int ConvertKlineToMinute(this KlineInterval kline)
    {
        switch (kline)
        {
            case KlineInterval.OneMinute:
                return 1;
            case KlineInterval.ThreeMinutes:
                return 3;
            case KlineInterval.FiveMinutes:
                return 5;
            case KlineInterval.FifteenMinutes:
                return 15;
            case KlineInterval.ThirtyMinutes:
                return 30;
            case KlineInterval.OneHour:
                return 60;
            case KlineInterval.TwoHour:
                return 120;
            case KlineInterval.FourHour:
                return 240;
            case KlineInterval.OneDay:
                return 60 * 24;
        }
        
        return 0;
    }
    
}