namespace AntiScam.Blog.Api.Data;

public interface ISecureBackupService
{
    Task<SecureBackupResult> CreateIfChangedAsync(CancellationToken cancellationToken = default);
}

public sealed record SecureBackupResult(bool Created, string BackupPath, string? Reason = null);
