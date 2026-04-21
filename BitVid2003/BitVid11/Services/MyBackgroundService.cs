using BitVid11.Data;
using BitVid11.Models;
using BitVid11.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Stripe;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

public class MyBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWebHostEnvironment _env;
    private Process? _pythonProcess;
   
    public MyBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env
        )
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _env = env;
        
    }

    private readonly string _mp4SourceFolder = @"C:\LTX-2-OPTIMIZED";
    private readonly string _mp4TargetFolder = @"C:\BitVidPremium9\BitVid2003\BitVid11\wwwroot\ltxvideo";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        {
            // moves mp4 files
            var mp4MonitorTask = MonitorMp4Files(stoppingToken);
            // stops at stage 2 
            var stopAtStage2 = StopAtStage2(stoppingToken);
            // restarts gitbash python service web_ui.v4.py


            
            await Task.WhenAll( mp4MonitorTask, stopAtStage2);

        }
    }

    private async Task StopAtStage2(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // show work log and check for cancel request
                await MonitorWorkLog(stoppingToken);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task sendCancelRequest()
    {
        var client = _httpClientFactory.CreateClient();

        var pythonApiUrl = "http://localhost:8000/cancel";

        var apiKey = "supersecretkey";
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);

        var response = await client.PostAsync(pythonApiUrl, null);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"ERROR {response.StatusCode}: {error}");
            return;
        }
        Console.WriteLine("Cancel Request Executed");

        GitBashLauncher.CloseProcess();
        GitBashLauncher.LaunchLtxApp();



    }
    private async Task MonitorWorkLog(CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient();
 
        var pythonApiUrl = "http://localhost:8000/monitor";
        var apiKey = "supersecretkey";
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);
        // send monitor work log request
        var response = await client.PostAsync(pythonApiUrl, null);
        if (!response.IsSuccessStatusCode)
        {

            GitBashLauncher.CloseProcess();
            GitBashLauncher.LaunchLtxApp();

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var videoJobs = await dbContext.VideoJobs.ToListAsync(stoppingToken);

                foreach (var videoJob in videoJobs)
                {
                    if (videoJob.Status == "Queued")
                    {
                        // Call your method here
                        QueueJob(videoJob);
                    }
                }
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

                GitBashLauncher.LaunchLTXAPI();
            }, stoppingToken);
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"ERROR {response.StatusCode}: {error}");
            return;
        }

        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var workerlog = doc.RootElement.GetProperty("result").GetString();

        Console.WriteLine("THE WORK LOG:\n\r\n\r " + workerlog);

        // turn workerlog to string
        workerlog = workerlog.ToString();

        // read through string
        if (workerlog.Contains("Stage 2"))
        {
            Console.WriteLine("Found Stage 2!");
            await sendCancelRequest();
        }
        else
        {
            Console.WriteLine("Stage 2 not found.");
        }

        // monitor work log every 5 seconds, keep checking for stage 2 message
        await Task.Delay(5000, stoppingToken);

    }

    private async void QueueJob(VideoJobs videojob)
    {
        var client = _httpClientFactory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(videojob.Prompt), "prompt");

        var physicalPath = Path.Combine(_env.WebRootPath, videojob.uploadedImagePath.TrimStart('/'));

        if (System.IO.File.Exists(physicalPath))
        {
            var stream = System.IO.File.OpenRead(physicalPath);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(fileContent, "reference", Path.GetFileName(physicalPath));

        }

        var pythonApiUrl = "http://localhost:8000/generate";
        var apiKey = "supersecretkey";
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);

        var response = await client.PostAsync(pythonApiUrl, content);
        response.EnsureSuccessStatusCode();


        throw new NotImplementedException();
    }

    private async Task MonitorMp4Files(CancellationToken stoppingToken)
    {
        // this process will hit every 5 seconds
        // this process will execute a task in 5 minutes after video generation takes place
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // @"C:\BitVidPremium5\BitVid2003\BitVid11\wwwroot\ltxvideo";
                if (!Directory.Exists(_mp4TargetFolder))
                    Directory.CreateDirectory(_mp4TargetFolder);

                // @"C:\LTX-2-OPTIMIZED";
                if (Directory.Exists(_mp4SourceFolder))
                {
                    // get all mp4 files in ltx 2 folder
                    var files = Directory.GetFiles(_mp4SourceFolder, "*.mp4");

                    foreach (var filePath in files)
                    {
                        // get the filename example : output_20260316_074014_bc50_
                        // gets console job id for example: --- STARTED JOB: bc50 ---
                        // only after file exists so 5 minutes after video generation begins
                        var fileName = Path.GetFileName(filePath);
                        // get the final path output
                        var destPath = Path.Combine(_mp4TargetFolder, fileName);

                        // Copy 
                        System.IO.File.Copy(filePath, destPath, true);
                        // then delete to avoid partial files
                        System.IO.File.Delete(filePath);

                        Console.WriteLine($"Moved MP4 file: {fileName}");

                        // Extract Job ID from filename
                        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"_([a-zA-Z0-9]{4})_(?=\.mp4$)");

                        if (match.Success)
                        {
                            // gets job id last four for filename example: f342 or 3drt
                            string jobId = match.Groups[1].Value;
                            
                            using var scope = _scopeFactory.CreateScope();
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();


                            var job = await dbContext.VideoJobs
                                .FirstOrDefaultAsync(v => v.JobIdentification == jobId);

                            if (job != null)
                            {
                                job.Status = "Done";
                                job.VideoPath = Path.GetFileNameWithoutExtension(fileName);

                                await dbContext.SaveChangesAsync(stoppingToken);

                                Console.WriteLine($"Job {jobId} updated to Done with video {fileName}");
                            }
                            else
                            {
                                Console.WriteLine($"No matching job for {jobId}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving MP4 files: {ex.Message}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

}