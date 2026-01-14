// See https://aka.ms/new-console-template for more information


using ReadBinanceData.Process;

Console.WriteLine($"프로그램 시작시간 {DateTime.Now}");

var installer = new ProjectInstaller(args);

var process = installer.provider.GetService(typeof(IProcess)) as IProcess;


AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
{
    Console.WriteLine("Process Finished");
    installer.Dispose();
};


process?.Process();
while (true)
{
    if (process?.IsRun == false) break;
}
process?.OnClose(); 