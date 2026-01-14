using System.Collections.Specialized;
using System.Net;
using System.Text;
using Binance.Net.Enums;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ReadBinanceData.AsyncContainer;
using WatsonWebserver;
using WatsonWebserver.Core;
using WatsonWebserver.Extensions.HostBuilderExtension;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

public class WebserverFileInfo
{
    public string host;
    public int port;
    public string summary_route;
    public string pivot_route;
    public string ma_route;
    public string oscillator_route;
    public string test_summary_route;
    public string test_pivot_route;
    public string test_ma_route;
    public string test_oscillator_route;
}


public class DataWebServer : IDisposable
{
    private readonly CancellationTokenSource cancellation;
    private readonly IBinanceExchangeKlineManager binanaceConn;
    private readonly DataWebserverStrings webserverStrings;
    private readonly WebserverFileInfo settingFileInfo;
    private Webserver webserver;

    private RxConcurrentDictionary<KlineInterval, Dictionary<string, string>> summarystrings;
    private RxConcurrentDictionary<KlineInterval, Dictionary<string, string>> oscillatorstrings;
    private RxConcurrentDictionary<KlineInterval, Dictionary<string, string>> movingaveragestrings;
    
    
    public DataWebServer([FromKeyedServices("socketconnect")]IBinanceExchangeKlineManager conn, DataWebserverStrings retstr)
    {
        binanaceConn = conn;
        webserverStrings = retstr;
        cancellation = new CancellationTokenSource();
        summarystrings = new RxConcurrentDictionary<KlineInterval, Dictionary<string, string>>();
        oscillatorstrings = new RxConcurrentDictionary<KlineInterval, Dictionary<string, string>>();
        movingaveragestrings = new RxConcurrentDictionary<KlineInterval, Dictionary<string, string>>();
        
        try
        {
            var setting = File.ReadAllText($"{Environment.CurrentDirectory}/DataResource/server_setting.json");
            settingFileInfo = JsonConvert.DeserializeObject<WebserverFileInfo>(setting);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Server Info Setting Failed {ex.Message}");
        }
    }


    
    public void StartServer()
    {
        var hostbuilder = new HostBuilder(settingFileInfo.host, settingFileInfo.port, false, DefaultRoute);
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.oscillator_route, Oscillators,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.oscillator_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.summary_route, Summary,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.summary_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.ma_route, MovingAverages,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.ma_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.pivot_route, Pivot,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.pivot_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.test_oscillator_route, Test_Oscillators,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.test_oscillator_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.test_summary_route, Test_Summary,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.test_summary_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.test_ma_route, Test_MovingAverages,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.test_ma_route);
            });
        hostbuilder.MapStaticRoute(HttpMethod.GET, settingFileInfo.test_pivot_route, Test_Pivot,
            async (ctx, exception) =>
            {
                await HandleException(ctx, exception, settingFileInfo.test_pivot_route);
            });
        
        
        webserver = hostbuilder.Build();
        
        
        
        try
        {
            webserver.Start(cancellation.Token);
            Console.WriteLine("StartServer");
        }
        catch(Exception e)
        {
            Console.WriteLine($"Cannot Start Server : {e.Message}");
        }
    }

    private async Task HandleException(HttpContextBase ctx, Exception ex, string route)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Exception Handler {route} {ctx.Request.Query.Querystring}");
        sb.AppendLine($"{ex.Message}");
        sb.AppendLine($"{ex.StackTrace}");
        var msg = sb.ToString();
        Console.WriteLine(msg);
    }


    /// <summary>
    /// 서버 정상동작 핑 테스트 용 api 루트
    /// </summary>
    /// <param name="ctx"></param>
    private async Task DefaultRoute(HttpContextBase ctx)
    {
        await ctx.Response.Send("hello world");
    }


    /// <summary>
    /// 1분마다 JSON 데이터를 만들어 둔다.
    /// </summary>
    public async Task PrepareResponseData()
    {
        foreach (var interval in Defines.binanceInterval)
        {
            var summarydict = new Dictionary<string, string>();
            var oscillatordict = new Dictionary<string, string>();
            var movingavgdict = new Dictionary<string, string>();
            foreach (var ticker in Defines.futureNames)
            {
                try
                {
                    await binanaceConn.UpdateFutureCandles(ticker, interval);
                    var summary = webserverStrings.Summary(ticker, interval);
                    var oscillator = webserverStrings.Oscillator(ticker, interval);
                    var movingavg = webserverStrings.MovingAverage(ticker, interval);

                    summarydict.Add(ticker, summary);
                    oscillatordict.Add(ticker, oscillator);
                    movingavgdict.Add(ticker, movingavg);
                }
                catch
                {
                    
                }
                    
            }

            summarydict.Add("TOTAL", JsonConvert.SerializeObject(summarydict, Formatting.None));
            oscillatordict.Add("TOTAL", JsonConvert.SerializeObject(oscillatordict, Formatting.None));
            movingavgdict.Add("TOTAL", JsonConvert.SerializeObject(movingavgdict, Formatting.None));
            summarystrings.TryRxAddOrUpdate(interval, summarydict);
            oscillatorstrings.TryRxAddOrUpdate(interval, oscillatordict);
            movingaveragestrings.TryRxAddOrUpdate(interval, movingavgdict);
        }
        
        await Task.Delay(TimeSpan.FromMinutes(1));
    }    
    
    
    /// <summary>
    /// Summary 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Summary(HttpContextBase ctx)
    {
        var response = ctx.Response;
        try
        {
            var symbol = ctx.Request.RetrieveQueryValue("symbol");
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (summarystrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue("TOTAL", out var retstr))
                    {
                        
                        response.ContentType = "application/json";
                    
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Summary {interval}");
                        }
                    }
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (summarystrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue(symbol, out var retstr))
                    {
                        response.ContentType = "application/json";
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Summary {interval}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Summary {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
        
    }
    
    /// <summary>
    /// Oscillators 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Oscillators(HttpContextBase ctx)
    {
        var response = ctx.Response;
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        try
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (oscillatorstrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue("TOTAL", out var retstr))
                    {
                        response.ContentType = "application/json";
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Oscillators {interval}");
                        }
                    }
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (oscillatorstrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue(symbol, out var retstr))
                    {
                        response.ContentType = "application/json";
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Oscillators {interval}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Oscillators {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }

    /// <summary>
    /// MovingAverages 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task MovingAverages(HttpContextBase ctx)
    {
        var response = ctx.Response;
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        try
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (movingaveragestrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue("TOTAL", out var retstr))
                    {
                    
                        response.ContentType = "application/json";
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Moving Average {interval}");
                        }
                    }
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (movingaveragestrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue(symbol, out var retstr))
                    {
                        response.ContentType = "application/json";
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Moving Average {interval}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"MovingAverages {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }

    /// <summary>
    /// Pivot 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Pivot(HttpContextBase ctx)
    {
        var response = ctx.Response;
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        try
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                var period = int.Parse(ctx.Request.RetrieveQueryValue("period"));
                var dict = new Dictionary<string, string>();
                foreach (var iter in Defines.futureNames)
                {
                    await binanaceConn.UpdateFutureCandles(iter, interval);
                    if (binanaceConn.GetKlines(iter, interval, true, out _))
                    {
                        var retStr = webserverStrings.Pivot(iter, interval, period);
                        dict.Add(iter, retStr);
                    }
                }
        
                string cleanedJson = JsonConvert.SerializeObject(dict, Formatting.None);
                response.ContentType = "application/json";
                var ret = await response.Send(cleanedJson);
                if (ret == false)
                {
                    Console.WriteLine($"Fail Pivot {interval}");
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                var period = int.Parse(ctx.Request.RetrieveQueryValue("period"));
                await binanaceConn.UpdateFutureCandles(symbol, interval);
                if (binanaceConn.GetKlines(symbol, interval, true, out _))
                {
                    var retStr = webserverStrings.Pivot(symbol, interval, period);
                    response.ContentType = "application/json";
                    var ret = await response.Send(retStr);
                    if (ret == false)
                    {
                        Console.WriteLine($"Fail Pivot {symbol} {interval}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Pivot {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }
    
     /// <summary>
    /// Summary 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Test_Summary(HttpContextBase ctx)
    {
        
        var response = ctx.Response;
        try
        {
            var symbol = ctx.Request.RetrieveQueryValue("symbol");
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (summarystrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue("TOTAL", out var retstr))
                    {
                        
                        response.ContentType = "application/json";
                        response.Headers.Add("Connection", "close");
                    
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Summary {interval}");
                        }
                    }
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (summarystrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue(symbol, out var retstr))
                    {
                        response.ContentType = "application/json";
                        response.Headers.Add("Connection", "close");
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Summary {interval}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Test_Summary {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }
    
    /// <summary>
    /// Oscillators 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Test_Oscillators(HttpContextBase ctx)
    {
        
        var response = ctx.Response;
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        try
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (oscillatorstrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue("TOTAL", out var retstr))
                    {
                        response.ContentType = "application/json";
                        response.Headers.Add("Connection", "close");
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Oscillators {interval}");
                        }
                    }
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (oscillatorstrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue(symbol, out var retstr))
                    {
                        response.ContentType = "application/json";
                        response.Headers.Add("Connection", "close");
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Oscillators {interval}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Test_Oscillators {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }

    /// <summary>
    /// MovingAverages 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Test_MovingAverages(HttpContextBase ctx)
    {
        
        var response = ctx.Response;
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        try
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (movingaveragestrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue("TOTAL", out var retstr))
                    {
                    
                        response.ContentType = "application/json";
                        response.Headers.Add("Connection", "close");
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Moving Average {interval}");
                        }
                    }
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                if (movingaveragestrings.TryGetValue(interval, out var dict))
                {
                    if (dict.TryGetValue(symbol, out var retstr))
                    {
                        response.ContentType = "application/json";
                        response.Headers.Add("Connection", "close");
                        var ret = await response.Send(retstr.Replace("\\", ""));
                        if (ret == false)
                        {
                            Console.WriteLine($"Fail Moving Average {interval}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Test_MovingAverages {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }

    /// <summary>
    /// Pivot 값 로드 인자값에 symbol이 있는 경우와 없는 경우에 따라서 개별 처리
    /// </summary>
    /// <param name="ctx"></param>
    private async Task Test_Pivot(HttpContextBase ctx)
    {
        var response = ctx.Response;
        var symbol = ctx.Request.RetrieveQueryValue("symbol");
        try
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                var period = int.Parse(ctx.Request.RetrieveQueryValue("period"));
                var dict = new Dictionary<string, string>();
                foreach (var iter in Defines.futureNames)
                {
                    await binanaceConn.UpdateFutureCandles(iter, interval);
                    if (binanaceConn.GetKlines(iter, interval, true, out _))
                    {
                        var retStr = webserverStrings.Pivot(iter, interval, period);
                        dict.Add(iter, retStr);

                    }
                }
        
                string cleanedJson = JsonConvert.SerializeObject(dict, Formatting.None);
                response.ContentType = "application/json";
                response.Headers.Add("Connection", "close");
                var ret = await response.Send(cleanedJson);
                if (ret == false)
                {
                    Console.WriteLine($"Fail Pivot {interval}");
                }
            }
            else
            {
                var interval = ToolScript.ConvertToInterval(ctx.Request.RetrieveQueryValue("interval"));
                var period = int.Parse(ctx.Request.RetrieveQueryValue("period"));
                await binanaceConn.UpdateFutureCandles(symbol, interval);
                if (binanaceConn.GetKlines(symbol, interval, true, out _))
                {
                    var retStr = webserverStrings.Pivot(symbol, interval, period);
                    response.ContentType = "application/json";
                    response.Headers.Add("Connection", "close");
                    var ret = await response.Send(retStr);
                    if (ret == false)
                    {
                        Console.WriteLine($"Fail Pivot {symbol} {interval}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Test_Pivot {ex.Message}/{ex.StackTrace}";
            Console.WriteLine(errorMsg);
        }
    }

    public void Dispose()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        webserver.Dispose();
    }

    
}