namespace Shared.Common.Constants;

public static class ThreadingConstants
{
    /// <summary>Số luồng tối đa khi chạy song song (Parallel.ForEachAsync / SemaphoreSlim).</summary>
    public const int MaxDegreeOfParallelism = 5;
}
