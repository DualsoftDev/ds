namespace DsWebApp.Client.Data
{
	public interface IbackupDataProvider
    {
		Task<IEnumerable<BackupData>> GetBackupAsync(CancellationToken ct = default);
		Task<IEnumerable<BackupData>> GetReducedBackupsAsync(CancellationToken ct = default);
	}
}
