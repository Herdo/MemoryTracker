namespace MemoryTracker
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Timers;
    using static System.Console;

    public static class Program
    {
        private static Process _process;
        private static string _outputFilePath;

        public static void Main()
        {
            var interval = SelectInterval();
            _process = SelectProcess();

            var utcNow = DateTime.UtcNow;
            var invalidPathChars = Path.GetInvalidFileNameChars();
            var cleanProcessName = new string(_process.ProcessName.Select(c => invalidPathChars.Contains(c) ? '_' : c).ToArray());
            var fileName = $"{utcNow:yyyyMMddHHmmss} - {cleanProcessName}.csv";

            _outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MemoryTracker", fileName);

            var timer = new Timer(interval * 1000)
            {
                AutoReset = true
            };
            timer.Elapsed += Timer_Elapsed;

            // ReSharper disable once AssignNullToNotNullAttribute
            var di = new DirectoryInfo(Path.GetDirectoryName(_outputFilePath));
            if (!di.Exists)
                di.Create();

            // Create file and write header
            File.WriteAllText(_outputFilePath, "UtcTimestamp;BytesUsed");

            WriteLine($"Writing output to {_outputFilePath}");
            WriteLine($"Tracking memory usage of \"{_process.ProcessName}\" in an interval of {interval} seconds. Press <Enter> to exit...");

            timer.Start();

            ReadLine();
        }

        private static int SelectInterval()
        {
            int interval;
            do
            {
                Write("Select interval (in seconds): ");
                int.TryParse(ReadLine(), out interval);
            } while (interval <= 0);

            return interval;
        }

        private static Process SelectProcess()
        {
            WriteLine("Getting processes...");

            var processes = Process.GetProcesses();
            foreach (var process in processes.OrderBy(m => m.ProcessName))
                WriteLine($"[{process.Id:D5}] {process.ProcessName}");

            int processId;
            do
            {
                Write("Enter a process ID: ");
                int.TryParse(ReadLine(), out processId);
            } while (processes.All(m => m.Id != processId));

            return processes.Single(m => m.Id == processId);
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _process.Refresh();
            // Write entry
            var entry = $"{Environment.NewLine}{DateTime.UtcNow:yyyy/MM/dd HH:mm:ss};{_process.PrivateMemorySize64}";
            Write(entry);
            File.AppendAllText(_outputFilePath, entry);
        }
    }
}
