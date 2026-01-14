
using Binance.Net.Enums;
using Binance.Net.Interfaces;

public class VWMA_TWENTY : MACollection<VolumeWeightedMA>
{
    public VWMA_TWENTY(Dictionary<KlineInterval, List<IBinanceKline>> candle) : base(candle, 20)
    {
    }

    protected override string indicatorName => "vwma";
}

public class VolumeWeightedMA : MABase
{
    public VolumeWeightedMA(List<IBinanceKline> candle, int dur) : base(candle, dur)
    {
        var volume = candleDatas.Select(x => x.Volume).TakeLast(duration).ToArray();
        var close = candleDatas.Select(x => x.ClosePrice).TakeLast(duration).ToArray();
        var candleCount = candleDatas.Count;
        if (candleCount > dur)
        {
            var volumeSum = volume.Sum();
            decimal volumeWeightedValue = 0;
            for (int i = 0; i < duration; ++i)
            {
                volumeWeightedValue += volume[i] * close[i];
            }
            mainValue = volumeWeightedValue / volumeSum;
        }
        else
        {
            mainValue = 0M;
        }
    }
    
    protected override string indicatorName => $"vwma";
}