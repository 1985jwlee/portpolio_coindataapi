

using Binance.Net.Interfaces;
using TALib;

public class ChandeMomentumOscillator : TechnicalDataBase
{
    private int duration;
    
    protected override string indicatorName => $"chandemomentum";
    
    public ChandeMomentumOscillator(int dur, decimal multiple, bool useclose,  List<IBinanceKline> candle) : base(candle)
    {
        duration = dur;
        var high = candleDatas.Select(x => x.HighPrice).ToArray();
        var low = candleDatas.Select(x => x.LowPrice).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).ToArray();
        var candleCount = candleDatas.Count;
        var output = new decimal[candleCount];
        var retCode = Core.Cmo(close, 0, candleCount - 1, output, out _, out var outNbEle, duration);
        var cmodirection = new int[candleCount];

        for (int i = 0; i < candleCount; ++i)
        {
            if (output[i] > 50) cmodirection[i] = -1;
            else if (output[i] < -50) cmodirection[i] = 1;
            else cmodirection[i] = 0;
        }
        
        var atr = new decimal[candleCount];
        retCode = Core.Atr(high, low, close, 0, candleCount - 1, atr, out _, out outNbEle, duration);
        if (retCode == Core.RetCode.Success)
        {
            
            var longStop = new decimal[candleCount];
            var shortStop = new decimal[candleCount];
            for (int i = duration; i < candleCount; i++)
            {
                var highestHigh = useclose ? GetHighest(close, i, duration) : GetHighest(high, i, duration);
                var lowestLow = useclose ? GetLowest(close, i, duration) : GetLowest(low, i, duration);
                longStop[i] = highestHigh - multiple * atr[i];
                shortStop[i] = lowestLow + multiple * atr[i];
                if (i > 0)
                {
                    longStop[i] = close[i - 1] > longStop[i - 1] ? Math.Max(longStop[i], longStop[i - 1]) : longStop[i];
                    shortStop[i] = close[i - 1] < shortStop[i - 1] ? Math.Min(shortStop[i], shortStop[i - 1]) : shortStop[i];
                }
            }

            var chandeexit = new int[candleCount];
            
            for (int i = 1; i < candleCount; i++)
            {
                if (close[i] > shortStop[i - 1])
                {
                    if (cmodirection[i] == 1)
                    {
                        chandeexit[i] = 1;
                    }
                    else
                    {
                        chandeexit[i] = 0;
                    }
                }
                else if (close[i] < longStop[i - 1])
                {
                    if (cmodirection[i] == -1)
                    {
                        chandeexit[i] = -1;
                    }
                    else
                    {
                        chandeexit[i] = 0;
                    }
                }
                else
                {
                    chandeexit[i] = chandeexit[i - 1];
                }
            }
            mainValue = chandeexit[^1];
        }
    }

    public override StateAction GetStateAction()
    {
        switch ((int)mainValue)
        {
            case 1:
                return StateAction.Buy;
            case -1:
                return StateAction.Sell;
        }
        return StateAction.Neutral;
    }
    
    private decimal GetHighest(decimal[] prices, int endIndex, int length)
    {
        var highest = prices[endIndex];
        for (int i = endIndex - length + 1; i <= endIndex; i++)
        {
            if (i >= 0 && prices[i] > highest)
            {
                highest = prices[i];
            }
        }
        return highest;
    }

    private decimal GetLowest(decimal[] prices, int endIndex, int length)
    {
        var lowest = prices[endIndex];
        for (int i = endIndex - length + 1; i <= endIndex; i++)
        {
            if (i >= 0 && prices[i] < lowest)
            {
                lowest = prices[i];
            }
        }
        return lowest;
    }
}