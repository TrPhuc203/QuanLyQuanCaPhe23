using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace QuanLyQuanCaPhe23.Models
{
    public partial class QUANLYCAPHEContext : DbContext
    {
        public QUANLYCAPHEContext()
        {
        }

        public QUANLYCAPHEContext(DbContextOptions<QUANLYCAPHEContext> options)
            : base(options)
        {
        }


        public virtual DbSet<CaPhe> CaPhes { get; set; }
        public virtual DbSet<ChiTietDonHang> ChiTietDonHangs { get; set; }
        public virtual DbSet<DonHang> DonHangs { get; set; }
        public virtual DbSet<KhachHang> KhachHangs { get; set; }
        public virtual DbSet<KhuyenMai> KhuyenMais { get; set; }
        public virtual DbSet<QuanLy> QuanLies { get; set; }
        public virtual DbSet<Size> Sizes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=QUANLYCAPHE;Integrated Security=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<CaPhe>(entity =>
            {
                entity.ToTable("CaPhe");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Anh).HasMaxLength(200);

                entity.Property(e => e.MieuTa).HasMaxLength(100);

                entity.Property(e => e.SizeId).HasColumnName("SizeID");

                entity.Property(e => e.Ten).HasMaxLength(50);

                entity.Property(e => e.Tien).HasColumnType("decimal(18, 0)");

                entity.HasOne(d => d.Size)
                    .WithMany(p => p.CaPhes)
                    .HasForeignKey(d => d.SizeId)
                    .HasConstraintName("fk_sizeeee");
                entity.HasOne(d => d.QuanLy)
                .WithMany(p => p.CaPhes)
                .HasForeignKey(d => d.MaQl)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CaPhe_QuanLy");
            });

            modelBuilder.Entity<ChiTietDonHang>(entity =>
            {
                entity.HasKey(e => new { e.DonHangId, e.CaPheId })
                    .HasName("PK__ChiTietD__58A23880E684914E");

                entity.ToTable("ChiTietDonHang");

                entity.Property(e => e.DonHangId).HasColumnName("DonHangID");

                entity.Property(e => e.CaPheId).HasColumnName("CaPheID");

                entity.Property(e => e.KhuyenMaiId).HasColumnName("KhuyenMaiID");

                entity.Property(e => e.Tien).HasColumnType("decimal(18, 0)");

                entity.HasOne(d => d.CaPhe)
                    .WithMany(p => p.ChiTietDonHangs)
                    .HasForeignKey(d => d.CaPheId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_capheid");

                entity.HasOne(d => d.DonHang)
                    .WithMany(p => p.ChiTietDonHangs)
                    .HasForeignKey(d => d.DonHangId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_HD");

                entity.HasOne(d => d.KhuyenMai)
                    .WithMany(p => p.ChiTietDonHangs)
                    .HasForeignKey(d => d.KhuyenMaiId)
                    .HasConstraintName("fk_KM");
            });

            modelBuilder.Entity<DonHang>(entity =>
            {
                entity.ToTable("DonHang");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.KhachHangId).HasColumnName("KhachHangID");

                entity.Property(e => e.NgayTao)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.PayPalKey)
                    .HasMaxLength(500)
                    .IsFixedLength(true);

                entity.HasOne(d => d.KhachHang)
                    .WithMany(p => p.DonHangs)
                    .HasForeignKey(d => d.KhachHangId)
                    .HasConstraintName("fk_KHHH");
            });

            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.HasKey(e => e.MaKh)
                    .HasName("PK__KhachHan__2725CF1EDBB81A0C");

                entity.ToTable("KhachHang");

                entity.Property(e => e.MaKh).HasColumnName("MaKH");

                entity.Property(e => e.DiaChi).HasMaxLength(50);


                entity.Property(e => e.Gmail)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.HoKh)
                    .HasMaxLength(50)
                    .HasColumnName("HoKH");

                entity.Property(e => e.Pass).HasMaxLength(50);

                entity.Property(e => e.SoDienThoai).HasMaxLength(50);

                entity.Property(e => e.TenKh)
                    .HasMaxLength(50)
                    .HasColumnName("TenKH");

                entity.Property(e => e.UserName).HasMaxLength(50);
            });

            modelBuilder.Entity<KhuyenMai>(entity =>
            {
                entity.ToTable("KhuyenMai");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Ten).HasMaxLength(50);
            });

            modelBuilder.Entity<QuanLy>(entity =>
            {
                entity.HasKey(e => e.MaQl)
                    .HasName("PK__QuanLy__2725F85246C6FCA9");

                entity.ToTable("QuanLy");

                entity.Property(e => e.MaQl).HasColumnName("MaQL");

                entity.Property(e => e.DiaChi).HasMaxLength(50);

                entity.Property(e => e.HoQl)
                    .HasMaxLength(50)
                    .HasColumnName("HoQL");

                entity.Property(e => e.Pass).HasMaxLength(50);

                entity.Property(e => e.SoDienThoai).HasMaxLength(50);

                entity.Property(e => e.TenQl)
                    .HasMaxLength(50)
                    .HasColumnName("TenQL");

                entity.Property(e => e.UserName).HasMaxLength(50);
            });

            modelBuilder.Entity<Size>(entity =>
            {
                entity.ToTable("Size");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DungTich).HasMaxLength(20);

                entity.Property(e => e.Ten).HasMaxLength(10);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
