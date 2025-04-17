using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SuperHeroAPI.md4;

namespace SuperHeroAPI.Data.Context;

public partial class dc3 : DbContext
{
    public dc3()
    {
    }

    public dc3(DbContextOptions<dc3> options)
        : base(options)
    {
    }

    public virtual DbSet<UmsFunctionUser> UmsFunctionUsers { get; set; }

    public virtual DbSet<UmsGlobalpermission> UmsGlobalpermissions { get; set; }

    public virtual DbSet<UmsPermission> UmsPermissions { get; set; }

    public virtual DbSet<UmsProcedureUser> UmsProcedureUsers { get; set; }

    public virtual DbSet<UmsRequestLog> UmsRequestLogs { get; set; }

    public virtual DbSet<UmsRole> UmsRoles { get; set; }

    public virtual DbSet<UmsTableUser> UmsTableUsers { get; set; }

    public virtual DbSet<UmsTriggerUser> UmsTriggerUsers { get; set; }

    public virtual DbSet<UmsUser> UmsUsers { get; set; }

    public virtual DbSet<UmsUserAuthToken> UmsUserAuthTokens { get; set; }

    public virtual DbSet<UmsUserRole> UmsUserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=195.93.252.168;Database=praktikapart2;Username=postgres;Password=plsworkpls");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UmsFunctionUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("function_user_pkey");

            entity.ToTable("ums_function_user", "ums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FunctionName)
                .HasColumnType("character varying")
                .HasColumnName("function_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UmsFunctionUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("function_user_fk");
        });

        modelBuilder.Entity<UmsGlobalpermission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("pk_globalpermissions");

            entity.ToTable("ums_globalpermissions", "ums");

            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.CreateGrant)
                .HasDefaultValue(false)
                .HasColumnName("create_grant");
            entity.Property(e => e.CreateTableGrant)
                .HasDefaultValue(false)
                .HasColumnName("create_table_grant");
            entity.Property(e => e.DeleteTableGrant)
                .HasDefaultValue(false)
                .HasColumnName("delete_table_grant");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UpdateTableGrant)
                .HasDefaultValue(false)
                .HasColumnName("update_table_grant");

            entity.HasOne(d => d.Role).WithMany(p => p.UmsGlobalpermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_id");
        });

        modelBuilder.Entity<UmsPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_permissions");

            entity.ToTable("ums_permissions", "ums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Operation).HasColumnName("operation");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.TableName).HasColumnName("table_name");

            entity.HasOne(d => d.Role).WithMany(p => p.UmsPermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_role_id");
        });

        modelBuilder.Entity<UmsProcedureUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("procedure_user_pkey");

            entity.ToTable("ums_procedure_user", "ums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProcedureName)
                .HasColumnType("character varying")
                .HasColumnName("procedure_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UmsProcedureUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("procedure_user_fk");
        });

        modelBuilder.Entity<UmsRequestLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ums_request_logs_pkey");

            entity.ToTable("ums_request_logs", "ums");

            entity.HasIndex(e => e.UserId, "idx_request_logs_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Duration).HasColumnName("duration");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.Method)
                .HasMaxLength(10)
                .HasColumnName("method");
            entity.Property(e => e.Path).HasColumnName("path");
            entity.Property(e => e.QueryString).HasColumnName("query_string");
            entity.Property(e => e.RequestBody).HasColumnName("request_body");
            entity.Property(e => e.RequestTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("request_time");
            entity.Property(e => e.ResponseBody).HasColumnName("response_body");
            entity.Property(e => e.ResponseTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("response_time");
            entity.Property(e => e.StatusCode).HasColumnName("status_code");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UmsRequestLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user");
        });

        modelBuilder.Entity<UmsRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ums_roles_pkey");

            entity.ToTable("ums_roles", "ums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleName).HasColumnName("role_name");
        });

        modelBuilder.Entity<UmsTableUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("table_user_pkey");

            entity.ToTable("ums_table_user", "ums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Tablename)
                .HasColumnType("character varying")
                .HasColumnName("tablename");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UmsTableUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("table_user_fk");
        });

        modelBuilder.Entity<UmsTriggerUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trigger_user_pkey");

            entity.ToTable("ums_trigger_user", "ums");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TriggerName)
                .HasColumnType("character varying")
                .HasColumnName("trigger_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UmsTriggerUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("trigger_user_fk");
        });

        modelBuilder.Entity<UmsUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ums_users_pkey");

            entity.ToTable("ums_users", "ums");

            entity.HasIndex(e => e.Username, "ums_unique_username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Username).HasColumnName("username");
        });

        modelBuilder.Entity<UmsUserAuthToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_auth_tokens_pkey");

            entity.ToTable("ums_user_auth_tokens", "ums");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.Expiration).HasColumnName("expiration");
            entity.Property(e => e.IsRevoked)
                .HasDefaultValue(false)
                .HasColumnName("is_revoked");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.UmsUserAuthToken)
                .HasForeignKey<UmsUserAuthToken>(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_id");
        });

        modelBuilder.Entity<UmsUserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("pk_user_role");

            entity.ToTable("ums_user_role", "ums");

            entity.HasIndex(e => e.RoleId, "IX_UserRole_RoleId");

            entity.HasIndex(e => e.UserId, "IX_UserRole_UserId");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Role).WithMany(p => p.UmsUserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("fk_user_role_roles_role_id");

            entity.HasOne(d => d.User).WithMany(p => p.UmsUserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_user_role_users_user_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
