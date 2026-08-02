namespace AntiScam.Blog.Api.Data;

public sealed record BackupOptions
{
    public bool Enabled { get; init; } = true;
    public string DirectoryPath { get; init; } = "secure_backups";
    public string? EncryptionKey { get; init; }
    public string KeyFilePath { get; init; } = "data/antiscam-backup.key";
}
