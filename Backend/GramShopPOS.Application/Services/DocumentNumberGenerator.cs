using System.Data;
using System.Data.Common;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.Interfaces;
using GramShopPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GramShopPOS.Application.Services;

public sealed class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private readonly IAppDbContext _db;

    public DocumentNumberGenerator(IAppDbContext db)
    {
        _db = db;
    }

    public Task<string> NextBillNumberAsync(int storeId, string prefix, int financialYearStartMonth, CancellationToken cancellationToken = default) =>
        NextAsync(_db.BillSequences, "BillSequences", storeId, prefix, financialYearStartMonth, cancellationToken);

    public Task<string> NextReturnNumberAsync(int storeId, string prefix, int financialYearStartMonth, CancellationToken cancellationToken = default) =>
        NextAsync(_db.ReturnSequences, "ReturnSequences", storeId, prefix, financialYearStartMonth, cancellationToken);

    private async Task<string> NextAsync<TSequence>(
        DbSet<TSequence> set,
        string tableName,
        int storeId,
        string prefix,
        int financialYearStartMonth,
        CancellationToken cancellationToken)
        where TSequence : class
    {
        var fy = FinancialYear.GetCode(DateTime.UtcNow, financialYearStartMonth);
        await EnsureRowAsync(set, tableName, storeId, prefix, fy, cancellationToken);
        var next = await IncrementAsync(tableName, storeId, fy, cancellationToken);
        return $"{prefix}-FY{fy}-{next:000000}";
    }

    private async Task EnsureRowAsync<TSequence>(
        DbSet<TSequence> set,
        string tableName,
        int storeId,
        string prefix,
        string fy,
        CancellationToken cancellationToken)
        where TSequence : class
    {
        if (tableName == "BillSequences")
        {
            if (await _db.BillSequences.AnyAsync(x => x.StoreId == storeId && x.FinancialYearCode == fy, cancellationToken))
            {
                return;
            }

            _db.BillSequences.Add(new BillSequence
            {
                StoreId = storeId,
                FinancialYearCode = fy,
                Prefix = prefix,
                LastNumber = 0,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
        }
        else
        {
            if (await _db.ReturnSequences.AnyAsync(x => x.StoreId == storeId && x.FinancialYearCode == fy, cancellationToken))
            {
                return;
            }

            _db.ReturnSequences.Add(new ReturnSequence
            {
                StoreId = storeId,
                FinancialYearCode = fy,
                Prefix = prefix,
                LastNumber = 0,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            });
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent insert of the same sequence row is expected.
        }
    }

    private async Task<int> IncrementAsync(string tableName, int storeId, string fy, CancellationToken cancellationToken)
    {
        var provider = _db.Database.ProviderName ?? string.Empty;
        if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return await IncrementSqlServerAsync(tableName, storeId, fy, cancellationToken);
        }

        if (tableName == "BillSequences")
        {
            var seq = await _db.BillSequences.FirstAsync(x => x.StoreId == storeId && x.FinancialYearCode == fy, cancellationToken);
            seq.LastNumber += 1;
            seq.UpdatedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return seq.LastNumber;
        }

        var ret = await _db.ReturnSequences.FirstAsync(x => x.StoreId == storeId && x.FinancialYearCode == fy, cancellationToken);
        ret.LastNumber += 1;
        ret.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ret.LastNumber;
    }

    private async Task<int> IncrementSqlServerAsync(string tableName, int storeId, string fy, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = $"""
            UPDATE [{tableName}]
            SET [LastNumber] = [LastNumber] + 1, [UpdatedDate] = SYSUTCDATETIME()
            OUTPUT INSERTED.[LastNumber]
            WHERE [StoreId] = @storeId AND [FinancialYearCode] = @fy
            """;

        var storeParam = command.CreateParameter();
        storeParam.ParameterName = "@storeId";
        storeParam.Value = storeId;
        command.Parameters.Add(storeParam);

        var fyParam = command.CreateParameter();
        fyParam.ParameterName = "@fy";
        fyParam.Value = fy;
        command.Parameters.Add(fyParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            throw new InvalidOperationException("Failed to allocate a document number.");
        }

        if (tableName == "BillSequences")
        {
            var local = _db.BillSequences.Local.FirstOrDefault(x => x.StoreId == storeId && x.FinancialYearCode == fy);
            if (local is not null)
            {
                await _db.ReloadTrackedAsync(local, cancellationToken);
            }
        }
        else
        {
            var local = _db.ReturnSequences.Local.FirstOrDefault(x => x.StoreId == storeId && x.FinancialYearCode == fy);
            if (local is not null)
            {
                await _db.ReloadTrackedAsync(local, cancellationToken);
            }
        }

        return Convert.ToInt32(result);
    }
}
