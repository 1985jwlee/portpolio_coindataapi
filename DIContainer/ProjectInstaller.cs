

using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.DependencyInjection;
using ReadBinanceData.Process;

public class ProjectInstaller : IDisposable
{
    public ServiceProvider provider { get; }
    
    public ProjectInstaller(string[] args)
    {
        var collection = new ServiceCollection();

        
        var binanceRest = new BinanceRestClient();
        collection.AddScoped<BinanceRestClient>(p => binanceRest);
        
        var binanceSocket = new BinanceSocketClient();
        collection.AddScoped<BinanceSocketClient>(p => binanceSocket);

        
        collection.AddKeyedSingleton<IBinanceExchangeKlineManager, BinanceSocketKlineManager>("socketconnect");
        collection.AddSingleton<DataWebserverStrings>();
        collection.AddSingleton<DataWebServer>();
        collection.AddSingleton<IProcess, MainProcess>();
        provider = collection.BuildServiceProvider();
    }

    public void Dispose()
    {
        provider.Dispose();
    }
}