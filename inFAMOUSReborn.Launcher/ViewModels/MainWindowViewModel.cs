using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Threading;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace inFAMOUSReborn.Launcher.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _currentStepText = "";
    [ObservableProperty] private bool _isOptional = false;
    [ObservableProperty] private string _stepDescription = "";
    [ObservableProperty] private string _primaryButtonText = "";
    [ObservableProperty] private string _terminalOutput = "inFAMOUS Reborn Setup initialized...\n";
    [ObservableProperty] private bool _isReadmeLinkVisible = true;
    [ObservableProperty] private bool _isSortMenuVisible = false;
    [ObservableProperty] private int _selectedSortIndex = 0;
    
    private int _step;
    private readonly string _missionsDir;
    private Process? _serverProcess;

    private const long BytesInKb = 1024;
    private const long BytesInMb = BytesInKb * 1024;

    public MainWindowViewModel()
    {
        _missionsDir = ResolveMissionsDirectory();
        
        try
        {
            string settingsPath = Path.Combine(_missionsDir, "server_settings.json");
            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("SortMode", out var prop))
                {
                    int savedMode = prop.GetInt32();
                    if (savedMode >= 0 && savedMode < SortOptions.Length)
                    {
                        _selectedSortIndex = savedMode;
                    }
                }
            }
        }
        catch { }

        SetStep(1);
    }

    private string ResolveMissionsDirectory()
    {
        string baseDir = AppContext.BaseDirectory;

        if (baseDir.Contains("/bin/Debug") || baseDir.Contains("/bin/Release"))
        {
            int binIndex = baseDir.IndexOf("/bin/");
            return Path.Combine(baseDir.Substring(0, binIndex), "Missions");
        }

        if (baseDir.Contains(".app/Contents/"))
        {
            int appIndex = baseDir.IndexOf(".app");
            string appBundlePath = baseDir.Substring(0, appIndex + 4);
            string parentDir = Directory.GetParent(appBundlePath)?.FullName ?? baseDir;
            return Path.Combine(parentDir, "Missions");
        }

        return Path.Combine(baseDir, "Missions");
    }

    private string GetLocalIpAddress()
    {
        try
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530); 
            IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1 (Offline)"; }
    }

    private void Log(string message)
    {
        Dispatcher.UIThread.Post(() => TerminalOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n");
    }

    private void LogSet(string message)
    {
        Dispatcher.UIThread.Post(() => TerminalOutput = message);
    }

    [RelayCommand]
    private async Task CopyLogAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
        {
            await desktop.MainWindow.Clipboard.SetTextAsync(TerminalOutput);
        }
    }

    [RelayCommand]
    private async Task ExecutePrimaryActionAsync()
    {
        try
        {
            switch (_step)
            {
                case 1:
                    await KillPortsAsync();
                    CheckMissionsAndProceed();
                    break;
                case 2:
                    await DownloadMissionsAsync();
                    SetStep(3);
                    break;
                case 3:
                    StartServer();
                    break;
                case 4:
                    StopServer();
                    break;
            }
        }
        catch (Exception ex) { Log($"ERROR: {ex.Message}"); }
    }

    [RelayCommand]
    private void SkipStep()
    {
        if (_step == 1) CheckMissionsAndProceed();
        else SetStep(_step + 1);
    }

    private void CheckMissionsAndProceed()
    {
        bool baseExists = Directory.Exists(Path.Combine(_missionsDir, "base")) && Directory.EnumerateFileSystemEntries(Path.Combine(_missionsDir, "base")).Any();
        bool fobExists = Directory.Exists(Path.Combine(_missionsDir, "fob")) && Directory.EnumerateFileSystemEntries(Path.Combine(_missionsDir, "fob")).Any();
        
        if (baseExists && fobExists) SetStep(3);
        else SetStep(2);
    }

    [RelayCommand]
    private void OpenReadme()
    {
        Process.Start(new ProcessStartInfo { FileName = "https://github.com/adamstark1/inFAMOUS-Reborn-PS3", UseShellExecute = true });
    }

    public string[] SortOptions { get; } = { "Relevance (Default)", "Highest Rated", "Most Played", "Most Favorited" };
    partial void OnSelectedSortIndexChanged(int value)
    {
        try
        {
            var settings = new { SortMode = value };
            string json = System.Text.Json.JsonSerializer.Serialize(settings);
            
            string path = Path.Combine(_missionsDir, "server_settings.json");
            File.WriteAllText(path, json);
        
            Log($"[Settings] Sort mode updated to: {SortOptions[value]}");
        }
        catch (Exception ex) { Log($"[Sort] Failed to save sort settings: {ex.Message}"); }
    }

    private void SetStep(int step)
    {
        _step = step;
        IsSortMenuVisible = (_step == 4);
        switch (_step)
        {
            case 1:
                CurrentStepText = "Step 1/3: Port Clearance";
                IsOptional = true;
                StepDescription = "Free up Port 53 (DNS) and Port 80 (HTTPS).";
                PrimaryButtonText = "Kill Conflicting Ports";
                break;
            case 2:
                CurrentStepText = "Step 2/3: Download Missions";
                IsOptional = false;
                StepDescription = "Download and extract required mission files.";
                PrimaryButtonText = "Download & Extract";
                break;
            case 3:
                CurrentStepText = "Step 3/3: Console DNS";
                IsOptional = false;
                IsReadmeLinkVisible = true;
                StepDescription = $"Go to your PS3 Network Settings and set Primary DNS to:\n\n{GetLocalIpAddress()}";
                PrimaryButtonText = "Start Server";
                break;
            case 4:
                CurrentStepText = "Server is Running";
                IsOptional = false;
                IsReadmeLinkVisible = false;
                StepDescription = "Server is active. Monitoring PS3 requests in the terminal below.";
                PrimaryButtonText = "Stop Server";
                break;
        }
    }

    private void StartServer()
    {
        Log("Starting backend server...");
        string baseDir = AppContext.BaseDirectory;
        string exeName = OperatingSystem.IsWindows() ? "inFAMOUSReborn.exe" : "inFAMOUSReborn";
        
        string backendPath = Path.GetFullPath(Path.Combine(baseDir, "../Backend", exeName));

        if (!File.Exists(backendPath))
        {
            backendPath = Path.Combine(baseDir, exeName);
    
            if (!File.Exists(backendPath))
            {
                string devDebugPath = Path.GetFullPath(Path.Combine(baseDir, $"../../../../inFAMOUSReborn/bin/Debug/net8.0/{exeName}"));
                string devReleasePath = Path.GetFullPath(Path.Combine(baseDir, $"../../../../inFAMOUSReborn/bin/Release/net8.0/{exeName}"));

                if (File.Exists(devDebugPath)) backendPath = devDebugPath;
                else if (File.Exists(devReleasePath)) backendPath = devReleasePath;
                else
                {
                    string dllPath = Path.Combine(baseDir, "inFAMOUSReborn.dll");
                    if (File.Exists(dllPath)) backendPath = dllPath;
                }
            }
        }

        var psi = new ProcessStartInfo 
        { 
            UseShellExecute = false, 
            RedirectStandardOutput = true, 
            RedirectStandardError = true, 
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(backendPath)
        };
        
        if (backendPath.EndsWith(".dll"))
        {
            psi.FileName = "dotnet";
            psi.Arguments = $"\"{backendPath}\"";
        }
        else
        {
            psi.FileName = backendPath;
        }

        try
        {
            _serverProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _serverProcess.OutputDataReceived += (s, e) => 
            { 
                if (string.IsNullOrWhiteSpace(e.Data)) return;
                string line = e.Data.TrimStart();
                
                if (line.Contains("Microsoft.AspNetCore") || 
                    line.Contains("Microsoft.Hosting") || 
                    line.Contains("Now listening on:") || 
                    line.Contains("Application started.") || 
                    line.Contains("Hosting environment:") || 
                    line.Contains("Content root path:") || 
                    line.Contains("Overriding address(es)") || 
                    line.Contains("info: inFAMOUSReborn") || 
                    line.Contains("Building...") ||
                    line.Contains("warning CS") ||
                    line.Contains("warn: Microsoft") ||
                    line.Contains("lacks the subjectAlternativeName"))
                {
                    return;
                }
                
                if (!string.IsNullOrEmpty(line)) Log(line);
            };
            _serverProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log($"ERROR: {e.Data}"); };
            _serverProcess.Exited += (s, e) => { Log("Server process stopped."); Dispatcher.UIThread.Post(() => { if (_step == 4) SetStep(3); }); };
            
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();
            SetStep(4);
        }
        catch (Exception ex) { Log($"Failed to launch server: {ex.Message}"); }
    }

    private void StopServer()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            Log("Stopping backend server...");
            try { _serverProcess.Kill(true); } catch { }
            _serverProcess.Dispose();
            _serverProcess = null;
            Log("Please leave a star ⭐ on this repository!");
        }
        SetStep(3);
    }

    private async Task KillPortsAsync()
    {
        Log("Clearing Port 53 and Port 80...");
        if (OperatingSystem.IsMacOS())
        {
            await Process.Start(new ProcessStartInfo { FileName = "killall", Arguments = "-HUP mDNSResponder", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync();
            await Process.Start(new ProcessStartInfo { FileName = "killall", Arguments = "httpd", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync();
        }
        else if (OperatingSystem.IsWindows())
        {
            await Process.Start(new ProcessStartInfo { FileName = "net", Arguments = "stop sharedaccess", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!.WaitForExitAsync();
        }
        Log("Ports cleared.");
    }

    private async Task DownloadMissionsAsync()
    {
        if (!Directory.Exists(_missionsDir)) Directory.CreateDirectory(_missionsDir);
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(30);

        Log("Downloading inFAMOUS 2 base missions...");
        var baseBytes = await DownloadMissionsBufferedAsync("https://archive.org/download/infamous-2-ugc/maps_by_name.zip", client);
        string baseZip = Path.Combine(_missionsDir, "base.zip");

        Log("  > Extracting .zip and copying base missions...");
        await File.WriteAllBytesAsync(baseZip, baseBytes);
        
        await Task.Run(() => 
        {
            string tempBase = Path.Combine(_missionsDir, "temp_base");
            ExtractZip(baseZip, tempBase);
            string finalBase = Path.Combine(_missionsDir, "base");
            if (Directory.Exists(finalBase)) Directory.Delete(finalBase, true);
            Directory.Move(Path.Combine(tempBase, "maps_by_name"), finalBase);
            Directory.Delete(tempBase, true);
        });

        Log("Downloading base missions catalog...");
        string baseCatalogUrl = "https://github.com/adamstark1/inFAMOUS-Reborn-PS3/raw/refs/heads/main/Missions/ugc_missions_base.json.gz";
        var baseCatalogBytes = await DownloadMissionsBufferedAsync(baseCatalogUrl, client);
        await File.WriteAllBytesAsync(Path.Combine(_missionsDir, "ugc_missions_base.json.gz"), baseCatalogBytes);

        Log("Downloading Festival of Blood (FoB) missions...");
        var fobBytes = await DownloadMissionsBufferedAsync("https://archive.org/download/infamous-fob-ugc/maps_by_name.zip", client);
        string fobZip = Path.Combine(_missionsDir, "fob.zip");

        Log("  > Extracting .zip and copying FoB missions...");
        await File.WriteAllBytesAsync(fobZip, fobBytes);
        
        await Task.Run(() => 
        {
            string tempFob = Path.Combine(_missionsDir, "temp_fob");
            ExtractZip(fobZip, tempFob);
            string finalFob = Path.Combine(_missionsDir, "fob");
            if (Directory.Exists(finalFob)) Directory.Delete(finalFob, true);
            Directory.Move(Path.Combine(tempFob, "maps_by_name"), finalFob);
            Directory.Delete(tempFob, true);
        });

        Log("Downloading FoB missions catalog...");
        string fobCatalogUrl = "https://github.com/adamstark1/inFAMOUS-Reborn-PS3/raw/refs/heads/main/Missions/ugc_missions_fob.json.gz";
        var fobCatalogBytes = await DownloadMissionsBufferedAsync(fobCatalogUrl, client);
        await File.WriteAllBytesAsync(Path.Combine(_missionsDir, "ugc_missions_fob.json.gz"), fobCatalogBytes);

        File.Delete(baseZip);
        File.Delete(fobZip);
        Log("Cleanup finished.");
    }

    private async Task<byte[]> DownloadMissionsBufferedAsync(string url, HttpClient client)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var size = response.Content.Headers.ContentLength ?? 0;
        if (size == 0)
        {
            throw new InvalidOperationException("Downloading file failed: [size = 0]");
        }

        var bytes = new byte[(int)size];
        int total = 0;
        int readCount;
        long totalInMb = 0L;

        await using var stream = await response.Content.ReadAsStreamAsync();

        while ((readCount = await stream.ReadAsync(bytes.AsMemory(total))) != 0)
        {
            total += readCount;

            var newTotalInMb = total / BytesInMb;
            if (newTotalInMb == totalInMb) continue;

            ShowDownloadProgress(total, size);
            totalInMb = newTotalInMb;
        }

        ShowDownloadProgress(total, size);

        return bytes;
    }

    private void ShowDownloadProgress(long total, long size)
    {
        const char filled = '#';
        const char empty = '.';
        const int barLength = 40;

        var totalVal = Convert.ToDouble(total) / BytesInMb; 
        var sizeVal = Convert.ToDouble(size) / BytesInMb;
        
        var progress = totalVal / sizeVal;
        var progressLength = (int)(progress * barLength);

        var filledLine = new string(filled, progressLength);
        var emptyLine = new string(empty, barLength - progressLength);
        
        var sizeLine = $"{totalVal:N1} / {sizeVal:N1} MB";
        if (total == size)
        {
            sizeLine += " - Done.\n";
        }

        var line = $"[{DateTime.Now:HH:mm:ss}] [{filledLine}{emptyLine}] {sizeLine}";

        var terminalContent = TerminalOutput;
        if (terminalContent.EndsWith(" MB"))
        {
            var lineFeedIndex = terminalContent.LastIndexOf('\n');
            if (lineFeedIndex != -1)
            {
                terminalContent = terminalContent[..(lineFeedIndex + 1)];
                terminalContent += line;
                LogSet(terminalContent);
            }
            else
            {
                Log(line);
            }
        }
        else
        {
            terminalContent += line;
            LogSet(terminalContent);
        }
    }

    private void ExtractZip(string zipPath, string outputFolder)
    {
        if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
        ZipFile.ExtractToDirectory(zipPath, outputFolder);
    }
}