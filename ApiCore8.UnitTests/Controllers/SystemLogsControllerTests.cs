using ApiCore8.Api.Controllers;
using ApiCore8.Application.Contracts;
using ApiCore8.Application.Interfaces;
using ApiCore8.Domain.Entities;
using Xunit;

namespace ApiCore8.UnitTests.Controllers;

/// <summary>
/// Fake ISystemLogRepository — verify đúng luồng của Controller (map request/response, xử lý lỗi,
/// forward CancellationToken), không cần Mongo thật.
/// </summary>
file class FakeSystemLogRepository : ISystemLogRepository
{
    public SystemLog? LastInsertedLog;
    public string? LastDeletedId;
    public CancellationToken LastCancellationToken;
    public ResultMessage DeleteByIdResult = new(false, ResultMessage.ErrorTypes.No_Error, "Deleted successfully", "ok");
    public ResultMessage InsertResult = new(false, ResultMessage.ErrorTypes.No_Error, "Inserted successfully", "ok");

    public Task<PagedResult<SystemLog>> SearchAsync(SystemLogFilterRequest request, CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        return Task.FromResult(new PagedResult<SystemLog> { Items = new List<SystemLog>(), Total = 0, Page = 1, PageSize = 20 });
    }

    public Task<(SystemLog?, ResultMessage)> GetLogByIDAsync(string id, CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        return Task.FromResult<(SystemLog?, ResultMessage)>((null, new ResultMessage()));
    }

    public Task<ResultMessage> DeleteOldLogsAsync(int daysOld, CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        return Task.FromResult(new ResultMessage());
    }

    public Task<ResultMessage> DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        LastDeletedId = id;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(DeleteByIdResult);
    }

    public Task<ResultMessage> InsertAsync(SystemLog log, CancellationToken cancellationToken = default)
    {
        LastInsertedLog = log;
        LastCancellationToken = cancellationToken;
        return Task.FromResult(InsertResult);
    }
}

public class SystemLogsControllerTests
{
    [Fact]
    public async Task Insert_ValidRequest_CallsRepositoryWithUtcTimestamp()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);
        var request = new InsertSystemLogRequest { Level = "Error", Message = "test message", Category = "MyCategory" };

        var before = DateTime.UtcNow;
        var result = await controller.Insert(request, CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.False(result.IsError);
        Assert.NotNull(repo.LastInsertedLog);
        Assert.Equal("Error", repo.LastInsertedLog!.Level);
        Assert.Equal("test message", repo.LastInsertedLog.Message);
        Assert.InRange(repo.LastInsertedLog.Timestamp, before, after); // luôn UtcNow, không phải giờ local
        Assert.Equal("MyCategory", repo.LastInsertedLog.Properties!["SourceContext"].AsString);
    }

    [Fact]
    public async Task Insert_NoCategory_PropertiesIsNull()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);
        var request = new InsertSystemLogRequest { Level = "Information", Message = "no category" };

        await controller.Insert(request, CancellationToken.None);

        Assert.Null(repo.LastInsertedLog!.Properties);
    }

    [Fact]
    public async Task Insert_RepositoryReturnsError_PropagatesError()
    {
        var repo = new FakeSystemLogRepository
        {
            InsertResult = new ResultMessage(true, ResultMessage.ErrorTypes.Insert, "Error inserting log", "boom")
        };
        var controller = new SystemLogsController(repo);

        var result = await controller.Insert(new InsertSystemLogRequest { Message = "x" }, CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Insert_ForwardsCancellationTokenToRepository()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);
        using var cts = new CancellationTokenSource();

        await controller.Insert(new InsertSystemLogRequest { Message = "x" }, cts.Token);

        Assert.Equal(cts.Token, repo.LastCancellationToken);
    }

    [Fact]
    public async Task DeleteById_ValidId_CallsRepositoryWithSameId()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);

        var result = await controller.DeleteById("507f1f77bcf86cd799439011", CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("507f1f77bcf86cd799439011", repo.LastDeletedId);
    }

    [Fact]
    public async Task DeleteById_RepositoryReportsNotFound_PropagatesError()
    {
        var repo = new FakeSystemLogRepository
        {
            DeleteByIdResult = new ResultMessage(true, ResultMessage.ErrorTypes.Delete, "Not found", "no such log")
        };
        var controller = new SystemLogsController(repo);

        var result = await controller.DeleteById("507f1f77bcf86cd799439011", CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task DeleteById_ForwardsCancellationTokenToRepository()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);
        using var cts = new CancellationTokenSource();

        await controller.DeleteById("507f1f77bcf86cd799439011", cts.Token);

        Assert.Equal(cts.Token, repo.LastCancellationToken);
    }

    [Fact]
    public async Task GetRecent_ForwardsCancellationTokenToRepository()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);
        using var cts = new CancellationTokenSource();

        await controller.GetRecent(cancellationToken: cts.Token);

        Assert.Equal(cts.Token, repo.LastCancellationToken);
    }

    [Fact]
    public async Task GetStats_ForwardsCancellationTokenToRepository()
    {
        var repo = new FakeSystemLogRepository();
        var controller = new SystemLogsController(repo);
        using var cts = new CancellationTokenSource();

        await controller.GetStats(cts.Token);

        Assert.Equal(cts.Token, repo.LastCancellationToken);
    }
}
