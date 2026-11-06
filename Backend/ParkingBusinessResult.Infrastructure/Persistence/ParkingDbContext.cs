using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence;

public partial class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DevAuthUsers> DevAuthUsers { get; set; }

    public virtual DbSet<DevParkingSessions> DevParkingSessions { get; set; }

    public virtual DbSet<DlKthtBaiDoXe> DlKthtBaiDoXe { get; set; }

    public virtual DbSet<SpApDungKhungGia> SpApDungKhungGia { get; set; }

    public virtual DbSet<SpChiTietMucThuKhungGia> SpChiTietMucThuKhungGia { get; set; }

    public virtual DbSet<SpDatTruocChoDoXe> SpDatTruocChoDoXe { get; set; }

    public virtual DbSet<SpDonViKinhDoanhQuanLy> SpDonViKinhDoanhQuanLy { get; set; }

    public virtual DbSet<SpKetQuaKinhDoanhBaiDoXe> SpKetQuaKinhDoanhBaiDoXe { get; set; }

    public virtual DbSet<SpKhungGiaThuPhi> SpKhungGiaThuPhi { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ParkingDbContext).Assembly);
    }
}
