using System.Diagnostics;

namespace BitVid11.Services
{
    public class StartupTask : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Run API immediately

            GitBashLauncher.LaunchLtxApp();
            // Delay ONLY this call by 20 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
              
                GitBashLauncher.LaunchLTXAPI();
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Application stopping.");
            return Task.CompletedTask;
        }
    }
}
