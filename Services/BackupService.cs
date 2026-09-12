using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Channels;
using backend.Options;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace backend.Services;

public record BackupInfo(string Id, string Type, string Status, string? FileName, long? SizeBytes,
    DateTime CreatedAtUtc, DateTime? CompletedAtUtc, DateTime? ExpiresAtUtc, string? Error);

public interface IBackupService
{
    BackupInfo QueueInstant();
    IReadOnlyCollection<BackupInfo> List();
    string? GetDownloadPath(string id);
    (string Token, DateTime ExpiresAtUtc)? IssueDownloadToken(string id);
    string? ValidateDownloadToken(string id, string token);
}

public sealed class BackupService(
    IOptions<BackupOptions> configuredOptions,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<BackupService> logger) : BackgroundService, IBackupService
{
    private readonly BackupOptions options = configuredOptions.Value;
    private readonly Channel<(string Id, string Type)> queue = Channel.CreateUnbounded<(string, string)>();
    private readonly ConcurrentDictionary<string, BackupInfo> jobs = new();
    private readonly ConcurrentDictionary<string, (string BackupId, DateTime ExpiresAtUtc)> downloadTokens = new();
    private readonly SemaphoreSlim backupLock = new(1, 1);

    public BackupInfo QueueInstant()
    {
        EnsureDirectory();
        CleanupExpired();
        if (jobs.Values.Any(x => x.Status is "Queued" or "Running"))
            throw new InvalidOperationException("A backup is already being prepared. Please wait for it to finish.");

        var id = Guid.NewGuid().ToString("N");
        var item = new BackupInfo(id, "Instant", "Queued", null, null, DateTime.UtcNow, null,
            DateTime.UtcNow.AddHours(Math.Clamp(options.InstantRetentionHours, 1, 168)), null);
        jobs[id] = item;
        queue.Writer.TryWrite((id, "Instant"));
        return item;
    }

    public IReadOnlyCollection<BackupInfo> List()
    {
        EnsureDirectory();
        CleanupExpired();
        var stored = Directory.EnumerateFiles(options.Directory, "crm-*.tar.gz")
            .Select(ReadStoredBackup).Where(x => x is not null).Cast<BackupInfo>();
        return jobs.Values.Concat(stored)
            .GroupBy(x => x.Id).Select(x => x.OrderByDescending(y => y.CompletedAtUtc).First())
            .OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }

    public string? GetDownloadPath(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Any(c => !char.IsLetterOrDigit(c) && c != '-')) return null;
        CleanupExpired();
        return Directory.EnumerateFiles(options.Directory, $"crm-*-{id}.tar.gz").SingleOrDefault();
    }

    public (string Token, DateTime ExpiresAtUtc)? IssueDownloadToken(string id)
    {
        if (GetDownloadPath(id) is null) return null;
        CleanupDownloadTokens();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expiry = DateTime.UtcNow.AddMinutes(2);
        downloadTokens[token] = (id, expiry);
        return (token, expiry);
    }

    public string? ValidateDownloadToken(string id, string token)
    {
        CleanupDownloadTokens();
        if (!downloadTokens.TryGetValue(token, out var entry) || entry.BackupId != id || entry.ExpiresAtUtc < DateTime.UtcNow)
            return null;
        return GetDownloadPath(id);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                EnsureDirectory();
                CleanupExpired();
                await QueueWeeklyIfDue();
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Backup storage is unavailable; the API will continue and retry in five minutes.");
                try { await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            }
        }

        var readerTask = ProcessQueue(stoppingToken);
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                CleanupExpired();
                await QueueWeeklyIfDue();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        queue.Writer.TryComplete();
        await readerTask;
    }

    private async Task ProcessQueue(CancellationToken token)
    {
        await foreach (var request in queue.Reader.ReadAllAsync(token))
        {
            await backupLock.WaitAsync(token);
            try { await CreateBackup(request.Id, request.Type, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "CRM {BackupType} backup {BackupId} failed.", request.Type, request.Id);
                if (jobs.TryGetValue(request.Id, out var item))
                    jobs[request.Id] = item with { Status = "Failed", CompletedAtUtc = DateTime.UtcNow, Error = ex.Message };
            }
            finally { backupLock.Release(); }
        }
    }

    private async Task QueueWeeklyIfDue()
    {
        var newest = Directory.EnumerateFiles(options.Directory, "crm-weekly-*.tar.gz")
            .Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
        if (DateTime.UtcNow - newest < TimeSpan.FromDays(Math.Clamp(options.WeeklyIntervalDays, 1, 31))) return;
        if (jobs.Values.Any(x => x.Type == "Weekly" && x.Status is "Queued" or "Running")) return;
        var id = Guid.NewGuid().ToString("N");
        jobs[id] = new BackupInfo(id, "Weekly", "Queued", null, null, DateTime.UtcNow, null, null, null);
        await queue.Writer.WriteAsync((id, "Weekly"));
    }

    private async Task CreateBackup(string id, string type, CancellationToken token)
    {
        var started = jobs[id] with { Status = "Running" };
        jobs[id] = started;
        var prefix = type.ToLowerInvariant();
        var fileName = $"crm-{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{id}.tar.gz";
        var finalPath = Path.Combine(options.Directory, fileName);
        var staging = Path.Combine(options.Directory, $".staging-{id}");
        Directory.CreateDirectory(staging);
        try
        {
            var sqlPath = Path.Combine(staging, "database.sql");
            await DumpDatabase(sqlPath, token);
            var manifestPath = Path.Combine(staging, "backup-info.txt");
            await File.WriteAllTextAsync(manifestPath,
                $"CRM backup\nType: {type}\nCreated UTC: {DateTime.UtcNow:O}\nDatabase: database.sql\n", token);

            var paths = options.ProjectPaths.Append(environment.ContentRootPath)
                .Where(Directory.Exists).Distinct(StringComparer.Ordinal).ToArray();
            await CreateArchive(finalPath + ".partial", staging, paths, token);
            File.Move(finalPath + ".partial", finalPath, true);

            if (type == "Weekly")
                foreach (var old in Directory.EnumerateFiles(options.Directory, "crm-weekly-*.tar.gz").Where(x => x != finalPath))
                    TryDelete(old);

            var completed = DateTime.UtcNow;
            jobs[id] = started with { Status = "Completed", FileName = fileName,
                SizeBytes = new FileInfo(finalPath).Length, CompletedAtUtc = completed,
                ExpiresAtUtc = type == "Instant" ? completed.AddHours(Math.Clamp(options.InstantRetentionHours, 1, 168)) : null };
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            TryDelete(finalPath + ".partial");
        }
    }

    private async Task DumpDatabase(string outputPath, CancellationToken token)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("The database connection is not configured.");
        var cs = new MySqlConnectionStringBuilder(connectionString);
        var start = new ProcessStartInfo(options.MySqlDumpExecutable)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        start.ArgumentList.Add($"--host={cs.Server}");
        start.ArgumentList.Add($"--port={cs.Port}");
        start.ArgumentList.Add($"--user={cs.UserID}");
        start.ArgumentList.Add("--single-transaction");
        start.ArgumentList.Add("--quick");
        start.ArgumentList.Add("--routines");
        start.ArgumentList.Add("--events");
        start.ArgumentList.Add("--triggers");
        start.ArgumentList.Add(cs.Database);
        start.Environment["MYSQL_PWD"] = cs.Password;
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start mysqldump.");
        await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        var copy = process.StandardOutput.BaseStream.CopyToAsync(output, token);
        var error = process.StandardError.ReadToEndAsync(token);
        await Task.WhenAll(copy, process.WaitForExitAsync(token));
        var errorText = await error;
        if (process.ExitCode != 0) throw new InvalidOperationException($"Database backup failed: {errorText.Trim()}");
    }

    private static async Task CreateArchive(string archivePath, string staging, string[] projectPaths, CancellationToken token)
    {
        await using var stream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var gzip = new GZipStream(stream, CompressionLevel.Fastest);
        using var tar = new System.Formats.Tar.TarWriter(gzip, leaveOpen: false);
        tar.WriteEntry(Path.Combine(staging, "database.sql"), "database.sql");
        tar.WriteEntry(Path.Combine(staging, "backup-info.txt"), "backup-info.txt");
        for (var i = 0; i < projectPaths.Length; i++)
            AddDirectory(tar, projectPaths[i], $"project/{i + 1}", token);
        await stream.FlushAsync(token);
    }

    private static void AddDirectory(System.Formats.Tar.TarWriter tar, string root, string archiveRoot, CancellationToken token)
    {
        var pending = new Stack<(string Disk, string Archive)>();
        pending.Push((root, archiveRoot));
        while (pending.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var current = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(current.Disk))
            {
                var info = new FileInfo(file);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                tar.WriteEntry(file, $"{current.Archive}/{Path.GetFileName(file)}");
            }
            foreach (var directory in Directory.EnumerateDirectories(current.Disk))
            {
                var info = new DirectoryInfo(directory);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                pending.Push((directory, $"{current.Archive}/{info.Name}"));
            }
        }
    }

    private BackupInfo? ReadStoredBackup(string path)
    {
        var file = new FileInfo(path);
        var parts = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path)).Split('-');
        if (parts.Length < 5) return null;
        var type = parts[1] == "weekly" ? "Weekly" : "Instant";
        var id = parts[^1];
        DateTime? expires = type == "Instant"
            ? file.LastWriteTimeUtc.AddHours(Math.Clamp(options.InstantRetentionHours, 1, 168))
            : null;
        return new BackupInfo(id, type, "Completed", file.Name, file.Length, file.CreationTimeUtc,
            file.LastWriteTimeUtc, expires, null);
    }

    private void CleanupExpired()
    {
        if (!Directory.Exists(options.Directory)) return;
        var cutoff = DateTime.UtcNow.AddHours(-Math.Clamp(options.InstantRetentionHours, 1, 168));
        foreach (var file in Directory.EnumerateFiles(options.Directory, "crm-instant-*.tar.gz"))
            if (File.GetLastWriteTimeUtc(file) < cutoff) TryDelete(file);
        foreach (var item in jobs.Where(x => x.Value.Type == "Instant" && x.Value.ExpiresAtUtc <= DateTime.UtcNow).ToArray())
            jobs.TryRemove(item.Key, out _);
        foreach (var item in jobs.Where(x => x.Value.Status == "Failed" && x.Value.CompletedAtUtc < DateTime.UtcNow.AddHours(-24)).ToArray())
            jobs.TryRemove(item.Key, out _);
        CleanupDownloadTokens();
    }

    private void CleanupDownloadTokens()
    {
        foreach (var item in downloadTokens.Where(x => x.Value.ExpiresAtUtc < DateTime.UtcNow).ToArray())
            downloadTokens.TryRemove(item.Key, out _);
    }

    private void EnsureDirectory() => Directory.CreateDirectory(options.Directory);
    private void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch (Exception ex) { logger.LogWarning(ex, "Could not delete backup file {Path}.", path); } }
}
