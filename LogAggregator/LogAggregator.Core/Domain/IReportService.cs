namespace LogAggregator.Core.Domain
{
    public interface IReportService
    {
        Task GenerateReportAsync(string outputPath, CancellationToken ct = default);
    }
}
