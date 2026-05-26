using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace Shared.Infrastructure;

public static class FileStoreUtility
{
    private static readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);
    private static readonly string _filePath;

    static FileStoreUtility()
    {
        var envPath = Environment.GetEnvironmentVariable("FileStore__Path");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            _filePath = Path.GetFullPath(envPath);
            var folder = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return;
        }

        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var directory = new DirectoryInfo(basePath);
        while (directory != null && !directory.Name.Contains("snappay-poc"))
        {
            directory = directory.Parent;
        }
        var root = directory?.FullName ?? Path.Combine(Path.GetTempPath(), "snappay-events");
        var eventsDir = Path.Combine(root, "events");
        if (!Directory.Exists(eventsDir)) Directory.CreateDirectory(eventsDir);
        _filePath = Path.Combine(eventsDir, "blocked_amounts.txt");
    }

    public static async Task WriteEventAsync(int customerId, decimal amount, string status)
    {
        // Use InvariantCulture to ensure decimals are always written with '.' regardless of system locale
        var line = $"{customerId}|{amount.ToString(CultureInfo.InvariantCulture)}|{status}|{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}";
        await _fileSemaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_filePath, line);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    public static async Task<List<string>> ReadAllLinesAsync()
    {
        if (!File.Exists(_filePath)) return new List<string>();
        
        await _fileSemaphore.WaitAsync();
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            var lines = new List<string>();
            string? line;
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
            }
            return lines;
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    public static async Task DeleteEventAsync(int customerId, string timestamp)
    {
        await _fileSemaphore.WaitAsync();
        try
        {
            if (!File.Exists(_filePath)) return;
            var lines = File.ReadAllLines(_filePath).ToList();
            var entryToRemove = $"{customerId}|";
            var newLines = lines.Where(l => !(l.StartsWith(entryToRemove) && l.Contains(timestamp))).ToList();
            File.WriteAllLines(_filePath, newLines);
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    public static async Task<decimal> GetBlockedAmountAsync(int customerId)
    {
        var lines = await ReadAllLinesAsync();
        // Only consider "Pending" status as a blocked amount; confirmed status implies a final order
        var lastLine = lines.LastOrDefault(l => 
        {
            var p = l.Split('|');
            return p.Length >= 3 && p[0] == customerId.ToString() && 
                   p[2].Trim().Equals("Pending", StringComparison.OrdinalIgnoreCase);
        });

        if (lastLine != null)
        {
            var parts = lastLine.Split('|');
            if (parts.Length >= 2 && decimal.TryParse(parts[1], out var amount)) return amount;
        }
        return 0;
    }
}