using Microsoft.EntityFrameworkCore;
using RestaurantManagement.Common.Enums;
using RestaurantManagement.DataAccess.Context;
using RestaurantManagement.DataAccess.Entities;

namespace RestaurantManagement.DataAccess.Repositories;

public interface ITableRepository : IRepository<DiningTable>
{
    Task<DiningTable?> GetForUpdateAsync(int tableId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiningTable>> GetTablesWithAreaAsync(CancellationToken cancellationToken = default);
}

public class TableRepository : Repository<DiningTable>, ITableRepository
{
    public TableRepository(RestaurantDbContext context) : base(context)
    {
    }

    public async Task<DiningTable?> GetForUpdateAsync(int tableId, CancellationToken cancellationToken = default)
    {
        // Khóa bi quan ở mức bản ghi hàng trên PostgreSQL (bên trong transaction đang mở)
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM dining_tables WHERE id = {tableId} FOR UPDATE",
            cancellationToken);

        return await _context.DiningTables
            .Include(t => t.Area)
            .SingleOrDefaultAsync(t => t.Id == tableId, cancellationToken);
    }

    public async Task<IReadOnlyList<DiningTable>> GetTablesWithAreaAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DiningTables
            .AsNoTracking()
            .Include(t => t.Area)
            .OrderBy(t => t.Area.DisplayOrder)
            .ThenBy(t => t.TableNumber)
            .ToListAsync(cancellationToken);
    }
}
