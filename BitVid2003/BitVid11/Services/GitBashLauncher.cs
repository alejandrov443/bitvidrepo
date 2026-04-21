using System.Diagnostics;
using System.Management;

namespace BitVid11.Services
{
    public static class GitBashLauncher
    {
        public static Process process { get; set; }
        public static Process ltx2apiprocess { get; set; }

        public static void LaunchLtxApp()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\Git\git-bash.exe",
                    Arguments = "-c \"cd /c/LTX-2-OPTIMIZED && export PYTHONPATH=/c/LTX-2-OPTIMIZED/packages/ltx-pipelines/src:/c/LTX-2-OPTIMIZED/packages/ltx-core/src && source /c/Users/Arrowdyne/Miniconda3/etc/profile.d/conda.sh && conda activate base && python web_ui_v4.py\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                process = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Git Bash: {ex.Message}");
            }

        }

        public static void LaunchLTXAPI()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\Git\git-bash.exe",
                    Arguments = "-c \"cd /c/LTX-2-OPTIMIZED && source /c/Users/Arrowdyne/Miniconda3/etc/profile.d/conda.sh && conda activate base && uvicorn ltx2simple:app --reload --host 0.0.0.0 --port 8000",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                ltx2apiprocess = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Git Bash: {ex.Message}");
            }

        }

        public static void CloseProcess()
        {
            if (process != null && !process.HasExited)
            {
                KillProcessTree(process.Id); // This kills Git Bash + Python + any children
                process = null; // Clear the reference
            }
            endpythontask();
        }


        public static void Closeltx2apiProcess()
        {
            if (process != null && !process.HasExited)
            {
                KillProcessTree(process.Id); // This kills Git Bash + Python + any children
                process = null; // Clear the reference
            }
            endltx2task();
        }

        public static void endltx2task()
        {
            string processName = "uvicorn";

            // Get all processes with that name
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();  // Terminates the process
                    process.WaitForExit(); // Optional: wait for process to exit
                    Console.WriteLine($"Killed process {process.ProcessName} (ID: {process.Id})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error killing process: {ex.Message}");
                }
            }
        }



        public static void endpythontask()
        {
            string processName = "python";

            // Get all processes with that name
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();  // Terminates the process
                    process.WaitForExit(); // Optional: wait for process to exit
                    Console.WriteLine($"Killed process {process.ProcessName} (ID: {process.Id})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error killing process: {ex.Message}");
                }
            }
        }


        public static void KillProcessTree(int pid)
        {
            var searcher = new ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={pid}");

            foreach (ManagementObject mo in searcher.Get())
            {
                KillProcessTree(Convert.ToInt32(mo["ProcessID"]));
            }

            try
            {
                Process.GetProcessById(pid).Kill();
            }
            catch { /* ignore if already exited */ }
        }

    }
}
