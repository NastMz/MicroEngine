// ARCH-1 P1 measurement helper (Windows): runs one game process and records, as JSON,
//   wallMs       process start (just before CreateProcess) to exit
//   reportMs     process start to the launcher's "platform: ..." line (--report), i.e. runtime start, SDL video,
//                window and renderer created; the profile's "load" and "first frame" phases follow it
//   peakWorkingSetBytes / peakPrivateBytes   GetProcessMemoryInfo on the exited process handle
//                (PeakWorkingSetSize, PeakPagefileUsage = peak commit, which includes the GPU driver's user-mode heaps)
//   exitCode, stdout and stderr (kept in the files given)
//
// Usage: Measure <result.json> <log.txt> <exe> [args...]   (environment is inherited)
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

if (args.Length < 3) { Console.Error.WriteLine("Usage: Measure <result.json> <log.txt> <exe> [args...]"); return 2; }
string resultPath = args[0], logPath = args[1];
var info = new ProcessStartInfo(args[2]) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
foreach (var a in args.Skip(3)) info.ArgumentList.Add(a);

var log = new StringBuilder();
double reportMs = -1;
long start = Stopwatch.GetTimestamp();
using var process = Process.Start(info) ?? throw new InvalidOperationException("could not start " + args[2]);
process.OutputDataReceived += (_, e) =>
{
    if (e.Data is null) return;
    if (reportMs < 0 && e.Data.StartsWith("platform:", StringComparison.Ordinal))
        reportMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    lock (log) log.Append("out: ").AppendLine(e.Data);
};
process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (log) log.Append("err: ").AppendLine(e.Data); };
process.BeginOutputReadLine();
process.BeginErrorReadLine();
process.WaitForExit();
double wallMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

long peakWs = -1, peakPrivate = -1;
var counters = new ProcessMemoryCounters { cb = (uint)Marshal.SizeOf<ProcessMemoryCounters>() };
if (GetProcessMemoryInfo(process.Handle, out counters, counters.cb))
{
    peakWs = (long)counters.PeakWorkingSetSize;
    peakPrivate = (long)counters.PeakPagefileUsage;
}
File.WriteAllText(logPath, log.ToString());
string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
File.WriteAllText(resultPath,
    $"{{\"exitCode\":{process.ExitCode},\"wallMs\":{N(wallMs)},\"reportMs\":{N(reportMs)},\"peakWorkingSetBytes\":{peakWs},\"peakPrivateBytes\":{peakPrivate}}}\n");
return 0;

[DllImport("psapi.dll", SetLastError = true)]
static extern bool GetProcessMemoryInfo(IntPtr process, out ProcessMemoryCounters counters, uint size);

[StructLayout(LayoutKind.Sequential)]
struct ProcessMemoryCounters
{
    public uint cb;
    public uint PageFaultCount;
    public nuint PeakWorkingSetSize, WorkingSetSize, QuotaPeakPagedPoolUsage, QuotaPagedPoolUsage,
        QuotaPeakNonPagedPoolUsage, QuotaNonPagedPoolUsage, PagefileUsage, PeakPagefileUsage;
}
