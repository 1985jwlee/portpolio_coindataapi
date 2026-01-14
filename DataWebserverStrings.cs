using Binance.Net.Enums;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 전송될 JSON을 만드는 클래스 (거의 스테틱 클래스와 동일함)
/// </summary>
public class DataWebserverStrings
{
    private readonly IBinanceExchangeKlineManager binanceConn;
    
    public DataWebserverStrings([FromKeyedServices("socketconnect")]IBinanceExchangeKlineManager conn)
    {
        binanceConn = conn;
    }

    public string Pivot(string coinName, KlineInterval interval, int period)
    {
        JObject jObject = new JObject();
        
        var defaultSR = binanceConn.DefaultSRSeries(coinName, interval, period);
        var classic = new JObject();
        foreach (var kv in defaultSR)
        {
            classic.Add(kv.Key.ToString(), kv.Value);
        }
        jObject.Add("classic", classic);
        
        var fibbonachiSR = binanceConn.FibonattiSRSeries(coinName, interval, period);
        var fibo = new JObject();
        foreach (var kv in fibbonachiSR)
        {
            fibo.Add(kv.Key.ToString(), kv.Value);
        }
        jObject.Add("fibonacci", fibo);
        return jObject.ToString(Formatting.None);
    }

    public string Summary(string coinName, KlineInterval interval)
    {
        JObject jObject = new JObject();
        
        binanceConn.GetKlines(coinName, interval, true, out var candleInfo);
        var rsi = new RSI(14, candleInfo);
        var stoch = new StochasticK(14,3,3,candleInfo);
        var cci = new CCI(20, candleInfo);
        var adx = new ADI(14, candleInfo);
        var macd =  new MACD(12, 26, 9, IndicatorSourceType.CLOSE,candleInfo);
        var faststoch = new StochasticRSIFast(14, 3, 3, 14, candleInfo);
        var mom = new Momentum(10, candleInfo);
        var ult = new UltimateOscillator(7, 14, 28, candleInfo);
        var williams = new WilliamsR(14, candleInfo);
        var ao = new AwesomeOscillator(candleInfo);
        var bbp = new BullBearPower(candleInfo);
        var collections = new List<TechnicalDataBase>()
        {
            rsi, stoch, cci, adx, ao, mom,  macd, faststoch, williams, bbp, ult
        };
        collections.Add(new EMA(candleInfo, 7));
        
        collections.Add(new SMA(candleInfo, 7));
        //collections.Add(new EMA_TWENTY(candleInfo));
        collections.Add(new EMA(candleInfo, 30));
        
        collections.Add(new SMA(candleInfo, 30));
        
        collections.Add(new EMA(candleInfo, 50));
        
        collections.Add(new SMA(candleInfo, 50));
        collections.Add(new EMA(candleInfo, 90));
        collections.Add(new SMA(candleInfo, 90));
        
        collections.Add(new EMA(candleInfo, 200));
        collections.Add(new SMA(candleInfo, 200));

        //collections.Add(new SMA_TWENTY(candleInfo));
        collections.Add(new HullMA(candleInfo, 9));
        collections.Add(new VolumeWeightedMA(candleInfo, 20));
        collections.Add(new IchimokuBaseLine(candleInfo, 26));

        
        var action = new CountedAction(collections);
        jObject.Add("counted_actions", action.ToString());
        
        
        var rating = StateAction.Neutral;
        
        //Buy Sell 카운트 계산
        //Buy 가 Sell 보다 많으면 Buy로 처리 반대의 경우 Sell로 처리
        if (action.buy > action.sell + 1) rating = StateAction.Buy;
        if (action.buy + 1 < action.sell ) rating = StateAction.Sell;

        //각 상태값에서 중립값과 합산해서 3개가 더 많다면 strong 을 추가한다.
        if (rating == StateAction.Buy && action.sell + action.neutral + 3 < action.buy)
        {
            rating = StateAction.StrongBuy;
        }

        if (rating == StateAction.Sell && action.buy + action.neutral + 3 < action.sell)
        {
            rating = StateAction.StrongSell;
        }
        
        var list = collections.Select(x => x.MakeIndicatorData()).ToList();
        jObject.Add("overall_rating", rating.StateActionStr());
        jObject.Add("indicators", JsonConvert.SerializeObject(list, Formatting.None));
        
        JObject rootObject = JObject.Parse(jObject.ToString());
        
        // counted_actions 파싱 및 변환
        string countedActionsString = JObject.Parse(rootObject["counted_actions"].ToString()).ToString(Formatting.None);
        rootObject["counted_actions"] = JObject.Parse(countedActionsString);
        // indicators 파싱 및 변환
        JArray indicatorsArray = JArray.Parse(rootObject["indicators"].ToString());
        JArray parsedIndicatorsArray = new JArray();
        foreach (var indicatorString in indicatorsArray)
        {
            JObject indicator = JObject.Parse(indicatorString.ToString());
            parsedIndicatorsArray.Add(indicator);
        }
        rootObject["indicators"] = parsedIndicatorsArray;
        
        // 정리된 JSON 출력
        string cleanedJson = JsonConvert.SerializeObject(rootObject, Formatting.None);
        return cleanedJson;
    }

    public string MovingAverage(string coinName, KlineInterval interval)
    {
        JObject jObject = new JObject();
        
        binanceConn.GetKlines(coinName, interval, true, out var candleInfo);
        
        var collections = new List<TechnicalDataBase>();
        collections.Add(new EMA(candleInfo, 7));
        
        collections.Add(new SMA(candleInfo, 7));
        //collections.Add(new EMA_TWENTY(candleInfo));
        collections.Add(new EMA(candleInfo, 30));
        
        collections.Add(new SMA(candleInfo, 30));
        
        collections.Add(new EMA(candleInfo, 50));
        
        collections.Add(new SMA(candleInfo, 50));
        collections.Add(new EMA(candleInfo, 90));
        collections.Add(new SMA(candleInfo, 90));
        
        collections.Add(new EMA(candleInfo, 200));
        collections.Add(new SMA(candleInfo, 200));

        //collections.Add(new SMA_TWENTY(candleInfo));
        collections.Add(new HullMA(candleInfo, 9));
        collections.Add(new VolumeWeightedMA(candleInfo, 20));
        collections.Add(new IchimokuBaseLine(candleInfo, 26));
        
        var action = new CountedAction(collections);
        jObject.Add("counted_actions", action.ToString());
        var list = collections.Select(x => x.MakeIndicatorData()).ToList();
        jObject.Add("indicators", JsonConvert.SerializeObject(list, Formatting.None));

        var rating = StateAction.Neutral;
        //Buy Sell 카운트 계산
        //Buy 가 Sell 보다 많으면 Buy로 처리 반대의 경우 Sell로 처리
        if (action.buy > action.sell + 1) rating = StateAction.Buy;
        if (action.buy + 1 < action.sell ) rating = StateAction.Sell;

        //각 상태값에서 반대 시그널이 2개 미만일 경우 strong 추가
        if (rating == StateAction.Buy && action.sell < 2)
        {
            rating = StateAction.StrongBuy;
        }

        if (rating == StateAction.Sell && action.buy < 2)
        {
            rating = StateAction.StrongSell;
        }
        
        jObject.Add("overall_rating", rating.StateActionStr());
        
        
        JObject rootObject = JObject.Parse(jObject.ToString());
        
        // counted_actions 파싱 및 변환
        string countedActionsString = JObject.Parse(rootObject["counted_actions"].ToString()).ToString(Formatting.None);
        rootObject["counted_actions"] = JObject.Parse(countedActionsString);
        // indicators 파싱 및 변환
        JArray indicatorsArray = JArray.Parse(rootObject["indicators"].ToString());
        JArray parsedIndicatorsArray = new JArray();
        foreach (var indicatorString in indicatorsArray)
        {
            JObject indicator = JObject.Parse(indicatorString.ToString());
            parsedIndicatorsArray.Add(indicator);
        }
        rootObject["indicators"] = parsedIndicatorsArray;
        
        // 정리된 JSON 출력
        string cleanedJson = JsonConvert.SerializeObject(rootObject, Formatting.None);
        return cleanedJson;
    }
    
    public string Oscillator(string coinName, KlineInterval interval)
    {
        JObject jObject = new JObject();

        binanceConn.GetKlines(coinName,  interval, true, out var candleInfo);
        var rsi = new RSI(14, candleInfo);
        var stoch = new StochasticK(14,3,3,candleInfo);
        var cci = new CCI(20, candleInfo);
        var adx = new ADI(14, candleInfo);
        var macd =  new MACD(12, 26, 9, IndicatorSourceType.CLOSE,candleInfo);
        var faststoch = new StochasticRSIFast(14, 3, 3, 14, candleInfo);
        var mom = new Momentum(10, candleInfo);
        var ult = new UltimateOscillator(7, 14, 28, candleInfo);
        var williams = new WilliamsR(14, candleInfo);
        var ao = new AwesomeOscillator(candleInfo);
        var bbp = new BullBearPower(candleInfo);
        var collections = new List<TechnicalDataBase>()
        {
            rsi, stoch, cci, adx, ao, mom,  macd, faststoch, williams, bbp, ult
        };

        var action = new CountedAction(collections);
        jObject.Add("counted_actions", action.ToString());
        var list = collections.Select(x => x.MakeIndicatorData()).ToList();
        jObject.Add("indicators", JsonConvert.SerializeObject(list));
        
        var rating = StateAction.Neutral;
        //Buy Sell 카운트 계산
        //Buy 가 Sell 보다 많으면 Buy로 처리 반대의 경우 Sell로 처리
        if (action.buy > action.sell + 1) rating = StateAction.Buy;
        if (action.buy < action.sell + 1) rating = StateAction.Sell;

        //각 상태값에서 반대 시그널이 2개 미만일 경우 strong 추가
        if (rating == StateAction.Buy && action.sell < 2)
        {
            rating = StateAction.StrongBuy;
        }

        if (rating == StateAction.Sell && action.buy < 2)
        {
            rating = StateAction.StrongSell;
        }
        
        jObject.Add("overall_rating", rating.StateActionStr());
        
        
        JObject rootObject = JObject.Parse(jObject.ToString());
        
        // counted_actions 파싱 및 변환
        string countedActionsString = JObject.Parse(rootObject["counted_actions"].ToString()).ToString(Formatting.None);
        rootObject["counted_actions"] = JObject.Parse(countedActionsString);
        // indicators 파싱 및 변환
        JArray indicatorsArray = JArray.Parse(rootObject["indicators"].ToString());
        JArray parsedIndicatorsArray = new JArray();
        foreach (var indicatorString in indicatorsArray)
        {
            JObject indicator = JObject.Parse(indicatorString.ToString());
            parsedIndicatorsArray.Add(indicator);
        }
        rootObject["indicators"] = parsedIndicatorsArray;
        
        // 정리된 JSON 출력
        string cleanedJson = JsonConvert.SerializeObject(rootObject, Formatting.None);
        return cleanedJson;
        
    }

    public class CountedAction
    {
        public int buy;
        public int sell;
        public int neutral;

        public CountedAction(List<TechnicalDataBase> coll)
        {
            foreach (var col in coll)
            {
                var ind = col.mainValue;
                var sig = col.GetStateAction();
                switch (sig)
                {
                    case StateAction.Buy: ++buy;
                        break;
                    case StateAction.Neutral: ++neutral;
                        break;
                    case StateAction.Sell: ++sell;
                        break;
                }
            }
        }

        public override string ToString()
        {
            var obj = new JObject();

            
            obj.Add("Buy", buy);
            obj.Add("Neutral", neutral);
            obj.Add("Sell", sell);
            return obj.ToString();
        }
    }
}