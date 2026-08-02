using System.Security.Cryptography;
using System.Text.Json;
using AntiScam.Blog.Api.Security;
using Microsoft.Data.Sqlite;

namespace AntiScam.Blog.Api.Data;

public sealed class SecureBackupService(
    BlogDatabaseOptions database,
    BackupOptions options,
    ILogger<SecureBackupService> logger) : ISecureBackupService
{
    private const string BackupFileName = "backup.enc.json";
    private const string MetadataFileName = "backup_meta.json";

    public async Task<SecureBackupResult> CreateIfChangedAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            return new SecureBackupResult(false, string.Empty, "disabled");
        }

        var directory = Path.GetFullPath(options.DirectoryPath);
        Directory.CreateDirectory(directory);
        var backupPath = Path.Combine(directory, BackupFileName);
        var metadataPath = Path.Combine(directory, MetadataFileName);
        var snapshotPath = Path.Combine(directory, $".sqlite-snapshot-{Guid.NewGuid():N}.tmp");
        byte[] databaseBytes;
        try
        {
            await CreateDatabaseSnapshotAsync(snapshotPath, cancellationToken);
            databaseBytes = await File.ReadAllBytesAsync(snapshotPath, cancellationToken);
        }
        finally
        {
            if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
        }
        var digest = Convert.ToHexString(SHA256.HashData(databaseBytes));

        if (File.Exists(metadataPath))
        {
            await using var stream = File.OpenRead(metadataPath);
            var previous = await JsonSerializer.DeserializeAsync<BackupMetadata>(stream, cancellationToken: cancellationToken);
            if (previous?.SourceSha256 == digest && File.Exists(backupPath))
            {
                return new SecureBackupResult(false, backupPath, "unchanged");
            }
        }

        var secret = await GetOrCreateEncryptionSecretAsync(cancellationToken);
        var key = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret));
        var encryptor = new AesGcmAuthenticatedEncryptor(key);
        var payload = encryptor.Encrypt(Convert.ToBase64String(databaseBytes), "antiscam-sqlite-backup-v1");
        var temporaryBackup = backupPath + ".tmp";
        var temporaryMetadata = metadataPath + ".tmp";
        await File.WriteAllTextAsync(temporaryBackup, JsonSerializer.Serialize(payload), cancellationToken);
        await File.WriteAllTextAsync(temporaryMetadata, JsonSerializer.Serialize(new BackupMetadata("AES-GCM-256", DateTimeOffset.UtcNow, digest)), cancellationToken);
        File.Move(temporaryBackup, backupPath, true);
        File.Move(temporaryMetadata, metadataPath, true);
        return new SecureBackupResult(true, backupPath);
    }

    private async Task CreateDatabaseSnapshotAsync(string snapshotPath, CancellationToken cancellationToken)
    {
        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = database.DatabasePath, Pooling = false }.ConnectionString);
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = snapshotPath, Pooling = false }.ConnectionString);
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
    }

    private async Task<string> GetOrCreateEncryptionSecretAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.EncryptionKey)) return options.EncryptionKey;

        var keyPath = Path.GetFullPath(options.KeyFilePath);
        var keyDirectory = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrEmpty(keyDirectory)) Directory.CreateDirectory(keyDirectory);
        if (File.Exists(keyPath)) return (await File.ReadAllTextAsync(keyPath, cancellationToken)).Trim();

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        try
        {
            await File.WriteAllTextAsync(keyPath, secret, cancellationToken);
            logger.LogInformation("Generated a local backup encryption key at {KeyPath}.", keyPath);
            return secret;
        }
        catch (IOException) when (File.Exists(keyPath))
        {
            return (await File.ReadAllTextAsync(keyPath, cancellationToken)).Trim();
        }
    }

    private sealed record BackupMetadata(string Algorithm, DateTimeOffset CreatedAt, string SourceSha256);
}
