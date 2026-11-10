using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence;

public partial class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DevAuthUsers> DevAuthUsers { get; set; }


    public virtual DbSet<DlKthtBaiDoXe> DlKthtBaiDoXe { get; set; }

    public virtual DbSet<SpApDungKhungGia> SpApDungKhungGia { get; set; }

    public virtual DbSet<SpChiTietMucThuKhungGia> SpChiTietMucThuKhungGia { get; set; }

    public virtual DbSet<SpDatTruocChoDoXe> SpDatTruocChoDoXe { get; set; }

    public virtual DbSet<SpDonViKinhDoanhQuanLy> SpDonViKinhDoanhQuanLy { get; set; }

    public virtual DbSet<SpKetQuaKinhDoanhBaiDoXe> SpKetQuaKinhDoanhBaiDoXe { get; set; }

    public virtual DbSet<SpKhungGiaThuPhi> SpKhungGiaThuPhi { get; set; }

    internal ReadTransaction BeginReadTransaction()
    {
        Database.OpenConnection();
        SqliteTransaction? transaction = null;
        try
        {
            transaction = ((SqliteConnection)Database.GetDbConnection()).BeginTransaction(deferred: true);
            IDbContextTransaction enlisted = Database.UseTransaction(transaction)!;
            return new(this, transaction, enlisted);
        }
        catch
        {
            try { transaction?.Dispose(); }
            finally { Database.CloseConnection(); }
            throw;
        }
    }

    internal sealed class ReadTransaction(ParkingDbContext context, SqliteTransaction transaction, IDbContextTransaction enlisted) : IDisposable
    {
        public void Commit() => enlisted.Commit();

        public void Dispose()
        {
            // EF does not own a transaction passed to UseTransaction; release both handles.
            try { enlisted.Dispose(); }
            finally
            {
                try { transaction.Dispose(); }
                finally { context.Database.CloseConnection(); }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingDbContext).Assembly);
    }
}
