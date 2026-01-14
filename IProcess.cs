using Microsoft.Extensions.DependencyInjection;

namespace ReadBinanceData.Process;

public interface IProcess
{
    bool IsRun { get; }
    void Process();

    void OnClose();
}


public class MainProcess : IProcess
{
    private readonly IBinanceExchangeKlineManager restKlineManager;
    
    private readonly DataWebServer server;

    public MainProcess([FromKeyedServices("socketconnect")]IBinanceExchangeKlineManager conn, DataWebServer _server)
    {
        restKlineManager = conn;
        server = _server;
    }

    public bool IsRun { get; set; }

    public void Process()
    {
        IsRun = true;
        
        Task.Run(UpdateFutureCandles);
    }

    public void OnClose()
    {
        
    }

    private async Task UpdateFutureCandles()
    {
        
         
        await restKlineManager.Initialize();
        server.StartServer();
        while (IsRun)
        {
            foreach (var name in Defines.futureNames)
            {
                await restKlineManager.UpdateFutureCandles(name);
            }
            
            await server.PrepareResponseData();
        }
    }
}