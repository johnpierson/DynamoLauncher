using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DynamoLauncher;

static class Program
{
    private static readonly Regex VersionPattern =
        new(@"_(\d+\.\d+\.\d+\.\d+)_(\d{8}T\d{4})", RegexOptions.Compiled);

    private static readonly string[] ExeNames =
    [
        "DynamoSandbox.exe",
        "DynamoWPFCLI.exe",
        "DynamoCLI.exe",
    ];

    static readonly string BaseDir =
        Path.GetDirectoryName(Environment.ProcessPath ?? AppContext.BaseDirectory)
        ?? AppContext.BaseDirectory;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "Dynamo Launcher";

        NativeWindow.Setup();

        try
        {
            Run();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n  Fatal error: {ex}");
            Console.ResetColor();
            Console.WriteLine("\n  Press any key to exit.");
            Console.ReadKey(intercept: true);
        }
    }

    static void Run()
    {
        while (true)
        {
            var installs = Discover();

            Console.Clear();
            Header();

            Write("  Scanning: ", ConsoleColor.DarkGray);
            Write(BaseDir + "\n\n", ConsoleColor.DarkGray);

            if (installs.Count == 0)
            {
                Write("  No Dynamo installs found in subfolders.\n\n", ConsoleColor.Yellow);
                Write("  [R] Refresh   [Q] Quit\n\n  > ", ConsoleColor.DarkGray);
            }
            else
            {
                PrintTable(installs);
                Console.WriteLine();
                Write("  Number to launch, [R] refresh, [Q] quit: ", ConsoleColor.DarkGray);
            }

            var input = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";

            if (input == "Q" || input == "QUIT") break;
            if (input == "R") continue;

            if (int.TryParse(input, out int n) && n >= 1 && n <= installs.Count)
            {
                Launch(installs[n - 1]);
                break;
            }
        }
    }

    // ── Discovery ────────────────────────────────────────────────────────────

    static List<Install> Discover()
    {
        var result = new List<Install>();

        foreach (var folder in Directory.GetDirectories(BaseDir).OrderByDescending(d => d))
        {
            var exe = FindExe(folder);
            if (exe is null) continue;

            var name = Path.GetFileName(folder);
            var m    = VersionPattern.Match(name);

            string version, date;
            if (m.Success)
            {
                version = m.Groups[1].Value;
                var ts  = m.Groups[2].Value;
                date    = $"{ts[0..4]}-{ts[4..6]}-{ts[6..8]}  {ts[9..11]}:{ts[11..]}";
            }
            else
            {
                try   { version = FileVersionInfo.GetVersionInfo(exe).FileVersion ?? "?"; }
                catch { version = "?"; }
                try   { date = File.GetLastWriteTime(exe).ToString("yyyy-MM-dd  HH:mm"); }
                catch { date = "?"; }
            }

            result.Add(new Install(name, exe, version, date));
        }

        return result;
    }

    static string? FindExe(string folder)
    {
        foreach (var name in ExeNames)
        {
            var p = Path.Combine(folder, name);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    // ── Display ──────────────────────────────────────────────────────────────

    static void Header()
    {
        Write("\n  ╔══════════════════════════════╗\n", ConsoleColor.DarkCyan);
        Write("  ║     ", ConsoleColor.DarkCyan);
        Write("DYNAMO LAUNCHER", ConsoleColor.Cyan);
        Write("          ║\n", ConsoleColor.DarkCyan);
        Write("  ╚══════════════════════════════╝\n\n", ConsoleColor.DarkCyan);
    }

    static void PrintTable(List<Install> installs)
    {
        const int wNum  = 4;
        const int wVer  = 14;
        const int wDate = 20;

        Write($"  {"#",-wNum}{"VERSION",-wVer}{"BUILD DATE",-wDate}FOLDER\n", ConsoleColor.DarkGray);
        Write("  " + new string('─', 80) + "\n", ConsoleColor.DarkGray);

        for (int i = 0; i < installs.Count; i++)
        {
            var inst = installs[i];
            Write($"  {i + 1,-wNum}", ConsoleColor.Gray);
            Write($"{inst.Version,-wVer}", ConsoleColor.Cyan);
            Write($"{inst.Date,-wDate}", ConsoleColor.Blue);
            Write(inst.FolderName + "\n", ConsoleColor.DarkGray);
        }
    }

    // ── Launch ───────────────────────────────────────────────────────────────

    static void Launch(Install inst)
    {
        Console.Clear();
        Write("\n  Launching: ", ConsoleColor.DarkGray);
        Write(inst.ExePath + "\n", ConsoleColor.Cyan);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName         = inst.ExePath,
                WorkingDirectory = Path.GetDirectoryName(inst.ExePath),
                UseShellExecute  = true,
            });

            Write("\n  Dynamo started. This window will close.\n", ConsoleColor.DarkGreen);
            Thread.Sleep(1500);
        }
        catch (Exception ex)
        {
            Write($"\n  Error: {ex.Message}\n", ConsoleColor.Red);
            Write("\n  Press any key to exit.\n", ConsoleColor.DarkGray);
            Console.ReadKey(intercept: true);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }
}

// ── Native window setup ───────────────────────────────────────────────────────

static class NativeWindow
{
    const uint WM_SETICON          = 0x0080;
    const int  ICON_SMALL          = 0;
    const int  ICON_BIG            = 1;
    const uint SWP_NOSIZE          = 0x0001;
    const uint SWP_NOZORDER        = 0x0004;
    const uint MONITOR_NEAREST     = 0x00000002;

    [DllImport("kernel32")] static extern IntPtr GetConsoleWindow();
    [DllImport("user32")]   static extern bool   GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32")]   static extern bool   SetWindowPos(IntPtr hWnd, IntPtr hWndAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32")]   static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
    [DllImport("user32")]   static extern bool   GetMonitorInfo(IntPtr hMon, ref MONITORINFO mi);
    [DllImport("user32")]   static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("shell32")]  static extern int    SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);
    [DllImport("shell32")]  static extern uint   ExtractIconEx([MarshalAs(UnmanagedType.LPWStr)] string file, int index, IntPtr[] large, IntPtr[] small, uint count);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct MONITORINFO
    {
        public int  cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    public static void Setup()
    {
        try { SetCurrentProcessExplicitAppUserModelID("Autodesk.DynamoLauncher"); } catch { }

        var hwnd = GetConsoleWindow();
        if (hwnd == IntPtr.Zero) return;

        SetIcon(hwnd);
        CenterOnScreen(hwnd);
    }

    static void SetIcon(IntPtr hwnd)
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath is null) return;

            var large = new IntPtr[1];
            var small = new IntPtr[1];
            ExtractIconEx(exePath, 0, large, small, 1);

            if (large[0] != IntPtr.Zero)
                SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG,   large[0]);
            if (small[0] != IntPtr.Zero)
                SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, small[0]);
        }
        catch { }
    }

    static void CenterOnScreen(IntPtr hwnd)
    {
        try
        {
            GetWindowRect(hwnd, out var win);
            int w = win.Right  - win.Left;
            int h = win.Bottom - win.Top;

            var hMon = MonitorFromWindow(hwnd, MONITOR_NEAREST);
            var mi   = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            GetMonitorInfo(hMon, ref mi);

            int x = mi.rcWork.Left + (mi.rcWork.Right  - mi.rcWork.Left - w) / 2;
            int y = mi.rcWork.Top  + (mi.rcWork.Bottom - mi.rcWork.Top  - h) / 2;

            SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }
        catch { }
    }
}

record Install(string FolderName, string ExePath, string Version, string Date);
