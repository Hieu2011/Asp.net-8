using System.Data;
using ApiCore8.Application.Abstractions;
using ApiCore8.Application.Services;
using ApiCore8.Domain.Entities;
using Xunit;

namespace ApiCore8.UnitTests.Services;

/// <summary>
/// Fake IDataCore ghi lại tham số/tên store được gọi, trả về giá trị dựng sẵn theo tên store —
/// không cần DB thật, chỉ verify đúng luồng gọi + parse kết quả của UserRepository.
/// </summary>
file class FakeDataCore : IDataCore
{
    public readonly Dictionary<string, object> AddedParameters = new();
    public readonly List<string> StoreNamesCalled = new();
    public string? LastStoreName;
    public CancellationToken LastCancellationToken;
    public string NonQueryStringResult = "True";
    public object? ObjectResult;
    public object? ListResult;

    public IDbConnection IConnection { get; set; } = null!;
    public IDbCommand ICommand { get; set; } = null!;
    public IDbTransaction ITransaction { get; set; } = null!;

    public void AddParameter(string paramName, object value) => AddedParameters[paramName] = value;
    public void ClearParameters() => AddedParameters.Clear();
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StartTransactionScopeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Dispose() { }

    public Task<List<T>> ExecStoreToListObjectAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        LastStoreName = storeName;
        LastCancellationToken = cancellationToken;
        StoreNamesCalled.Add(storeName);
        return Task.FromResult(ListResult is List<T> typed ? typed : new List<T>());
    }

    public Task<T> ExecStoreToObjectAsync<T>(string storeName, CancellationToken cancellationToken = default)
    {
        LastStoreName = storeName;
        LastCancellationToken = cancellationToken;
        StoreNamesCalled.Add(storeName);
        return Task.FromResult(ObjectResult is T typed ? typed : Activator.CreateInstance<T>());
    }

    public Task<int> ExecuteNonQueryAsync(string storeName, CancellationToken cancellationToken = default)
    {
        LastStoreName = storeName;
        LastCancellationToken = cancellationToken;
        StoreNamesCalled.Add(storeName);
        return Task.FromResult(1);
    }

    public Task<string> ExecuteNonQueryAsStringAsync(string storeName, CancellationToken cancellationToken = default)
    {
        LastStoreName = storeName;
        LastCancellationToken = cancellationToken;
        StoreNamesCalled.Add(storeName);
        return Task.FromResult(NonQueryStringResult);
    }

    public Task<DataTable> ExecuteStoreDataTableAsync(string storeName, CancellationToken cancellationToken = default)
    {
        LastStoreName = storeName;
        LastCancellationToken = cancellationToken;
        StoreNamesCalled.Add(storeName);
        return Task.FromResult(new DataTable());
    }
}

public class UserRepositoryTests
{
    // Bug thật đã bắt: UserRepository nhận CancellationToken từ Controller nhưng "nuốt" mất, không
    // truyền xuống IDataCore -> HTTP request bị hủy giữa chừng vẫn không cắt được câu query đang
    // chạy. 6 test dưới đây verify CancellationToken thực sự đi tới tận IDataCore cho mọi method.
    [Fact]
    public async Task CreateAsync_ForwardsCancellationTokenToDataCore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        using var cts = new CancellationTokenSource();

        await repo.CreateAsync(new Users(), cts.Token);

        Assert.Equal(cts.Token, db.LastCancellationToken);
    }

    [Fact]
    public async Task GetByIdAsync_ForwardsCancellationTokenToDataCore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        using var cts = new CancellationTokenSource();

        await repo.GetByIdAsync(Guid.NewGuid(), cts.Token);

        Assert.Equal(cts.Token, db.LastCancellationToken);
    }

    [Fact]
    public async Task GetAllAsync_ForwardsCancellationTokenToDataCore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        using var cts = new CancellationTokenSource();

        await repo.GetAllAsync(cts.Token);

        Assert.Equal(cts.Token, db.LastCancellationToken);
    }

    [Fact]
    public async Task SearchByCreatedDateAsync_ForwardsCancellationTokenToDataCore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        using var cts = new CancellationTokenSource();

        await repo.SearchByCreatedDateAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, cts.Token);

        Assert.Equal(cts.Token, db.LastCancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_ForwardsCancellationTokenToDataCore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        using var cts = new CancellationTokenSource();

        await repo.UpdateAsync(new Users { Id = Guid.NewGuid() }, cts.Token);

        Assert.Equal(cts.Token, db.LastCancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_ForwardsCancellationTokenToDataCore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        using var cts = new CancellationTokenSource();

        await repo.DeleteAsync(Guid.NewGuid(), cts.Token);

        Assert.Equal(cts.Token, db.LastCancellationToken);
    }

    [Fact]
    public async Task CreateAsync_PassesAllFieldsAndCallsCorrectStore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        var user = new Users { Username = "hieu", PasswordHash = "hash", FullName = "Hieu", Email = "a@b.com" };

        await repo.CreateAsync(user);

        Assert.Equal("sp_user_create", db.LastStoreName);
        Assert.Equal("hieu", db.AddedParameters["p_username"]);
        Assert.Equal("hash", db.AddedParameters["p_password_hash"]);
        Assert.Equal("Hieu", db.AddedParameters["p_full_name"]);
        Assert.Equal("a@b.com", db.AddedParameters["p_email"]);
    }

    [Fact]
    public async Task CreateAsync_DoesNotPassIsActiveOrId()
    {
        // sp_user_create không nhận p_id/p_is_active — DB tự sinh (DEFAULT gen_random_uuid()/NEWID()/SYS_GUID(),
        // is_active mặc định true) — nếu ai lỡ thêm AddParameter("p_id"/"p_is_active", ...) vào CreateAsync
        // sau này thì test này sẽ nhắc lại đúng hợp đồng hiện tại.
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        var user = new Users { Username = "hieu", PasswordHash = "hash", FullName = "Hieu", Email = "a@b.com" };

        await repo.CreateAsync(user);

        Assert.False(db.AddedParameters.ContainsKey("p_id"));
        Assert.False(db.AddedParameters.ContainsKey("p_is_active"));
    }

    [Fact]
    public async Task GetByIdAsync_CallsCorrectStoreWithId()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        var id = Guid.NewGuid();

        await repo.GetByIdAsync(id);

        Assert.Equal("sp_user_get_by_id", db.LastStoreName);
        Assert.Equal(id, db.AddedParameters["p_id"]);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // FakeDataCore mặc định trả Activator.CreateInstance<Users>() khi không set ObjectResult —
        // Id = Guid.Empty (default) — đúng hành vi thật của ExecStoreToObjectAsync khi DB không có dòng nào.
        var db = new FakeDataCore();
        var repo = new UserRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsUser()
    {
        var existing = new Users { Id = Guid.NewGuid(), Username = "hieu", PasswordHash = "hash", FullName = "Hieu", Email = "a@b.com", IsActive = true };
        var db = new FakeDataCore { ObjectResult = existing };
        var repo = new UserRepository(db);

        var result = await repo.GetByIdAsync(existing.Id);

        Assert.NotNull(result);
        Assert.Equal(existing.Id, result!.Id);
        Assert.Equal("hieu", result.Username);
    }

    [Fact]
    public async Task GetAllAsync_CallsCorrectStore()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);

        await repo.GetAllAsync();

        Assert.Equal("sp_user_get_all", db.LastStoreName);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsWhatDbCoreReturns()
    {
        var users = new List<Users>
        {
            new() { Id = Guid.NewGuid(), Username = "a" },
            new() { Id = Guid.NewGuid(), Username = "b" },
        };
        var db = new FakeDataCore { ListResult = users };
        var repo = new UserRepository(db);

        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Username);
        Assert.Equal("b", result[1].Username);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyListNotNull()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);

        var result = await repo.GetAllAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchByCreatedDateAsync_CallsCorrectStoreWithBothDates()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        var fromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        await repo.SearchByCreatedDateAsync(fromDate, toDate);

        Assert.Equal("sp_user_search_by_date", db.LastStoreName);
        Assert.Equal(fromDate, db.AddedParameters["p_from_date"]);
        Assert.Equal(toDate, db.AddedParameters["p_to_date"]);
    }

    [Fact]
    public async Task SearchByCreatedDateAsync_ReturnsWhatDbCoreReturns()
    {
        var users = new List<Users> { new() { Id = Guid.NewGuid(), Username = "trong-khoang-ngay" } };
        var db = new FakeDataCore { ListResult = users };
        var repo = new UserRepository(db);

        var result = await repo.SearchByCreatedDateAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        Assert.Single(result);
        Assert.Equal("trong-khoang-ngay", result[0].Username);
    }

    [Fact]
    public async Task UpdateAsync_PassesAllFieldsIncludingId()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        var user = new Users { Id = Guid.NewGuid(), FullName = "Hieu Updated", Email = "new@b.com", IsActive = false };

        await repo.UpdateAsync(user);

        Assert.Equal(user.Id, db.AddedParameters["p_id"]);
        Assert.Equal("Hieu Updated", db.AddedParameters["p_full_name"]);
        Assert.Equal("new@b.com", db.AddedParameters["p_email"]);
        Assert.Equal(false, db.AddedParameters["p_is_active"]);
    }

    [Theory]
    [InlineData("True", true)]   // Postgres (boolean native)
    [InlineData("False", false)]
    [InlineData("1", true)]      // Oracle/SQL Server (NUMBER/BIT 0-1, không có boolean lộ ra .NET)
    [InlineData("0", false)]
    [InlineData(" 1 ", true)]    // driver có thể trả kèm khoảng trắng thừa
    [InlineData("", false)]      // rơi vào nhánh bool.TryParse — không parse được -> false, không throw
    public async Task UpdateAsync_ParsesBothBooleanAndNumericResult(string rawResult, bool expected)
    {
        var db = new FakeDataCore { NonQueryStringResult = rawResult };
        var repo = new UserRepository(db);
        var user = new Users { Id = Guid.NewGuid(), FullName = "x", Email = "x@x.com", IsActive = true };

        var result = await repo.UpdateAsync(user);

        Assert.Equal(expected, result);
        Assert.Equal("sp_user_update", db.LastStoreName);
    }

    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public async Task DeleteAsync_ParsesBothBooleanAndNumericResult(string rawResult, bool expected)
    {
        var db = new FakeDataCore { NonQueryStringResult = rawResult };
        var repo = new UserRepository(db);

        var result = await repo.DeleteAsync(Guid.NewGuid());

        Assert.Equal(expected, result);
        Assert.Equal("sp_user_delete", db.LastStoreName);
    }

    [Fact]
    public async Task DeleteAsync_PassesOnlyId()
    {
        var db = new FakeDataCore();
        var repo = new UserRepository(db);
        var id = Guid.NewGuid();

        await repo.DeleteAsync(id);

        Assert.Equal(id, db.AddedParameters["p_id"]);
        Assert.Single(db.AddedParameters);
    }
}
