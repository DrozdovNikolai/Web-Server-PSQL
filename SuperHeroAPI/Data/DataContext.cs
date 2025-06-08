global using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EFCore.NamingConventions;
using SuperHeroAPI.Models;

namespace PostgreSQL.Data
{
    public partial class DataContext : DbContext
    {

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            //this.Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
              optionsBuilder.UseLazyLoadingProxies();
              
              // Use snake_case naming convention for PostgreSQL
              optionsBuilder.UseSnakeCaseNamingConvention();
              
              base.OnConfiguring(optionsBuilder);
        }


        public virtual DbSet<Permission> Permissions { get; set; } = null!;
       

        public virtual DbSet<Role> Roles { get; set; } = null!;

        public virtual DbSet<SuperHero> SuperHeroes { get; set; } = null!;



        public virtual DbSet<User> Users { get; set; } = null!;

        public virtual DbSet<UserRole> UserRoles { get; set; } = null!;

        public virtual DbSet<TableUser> TableUsers { get; set; } = null!;
        public virtual DbSet<TriggerUser> TriggerUsers { get; set; } = null!;
        public virtual DbSet<ProcedureUser> ProcedureUsers { get; set; } = null!;
        public virtual DbSet<FunctionUser> FunctionUsers { get; set; } = null!;

        public virtual DbSet<UserAuthToken> UserAuthTokens { get; set; } = null!;

        public virtual DbSet<RequestLog> RequestLogs { get; set; } = null!;

        public virtual DbSet<Container> Containers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Container>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("ums_containers_pkey");

                entity.ToTable("ums_containers", "ums");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnName("created_at");
                entity.Property(e => e.DbHost)
                    .HasMaxLength(255)
                    .HasColumnName("db_host");
                entity.Property(e => e.DbName)
                    .HasMaxLength(255)
                    .HasColumnName("db_name");
                entity.Property(e => e.DbPassword)
                    .HasMaxLength(255)
                    .HasColumnName("db_password");
                entity.Property(e => e.DbPasswordUser)
                    .HasMaxLength(255)
                    .HasColumnName("db_password_user");
                entity.Property(e => e.DbPort)
                    .HasMaxLength(10)
                    .HasColumnName("db_port");
                entity.Property(e => e.DbUser)
                    .HasMaxLength(255)
                    .HasColumnName("db_user");
                entity.Property(e => e.DbUsername)
                    .HasMaxLength(255)
                    .HasColumnName("db_username");
                entity.Property(e => e.ExternalUrl)
                    .HasMaxLength(255)
                    .HasDefaultValue("")
                    .HasColumnName("external_url");
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });
            modelBuilder.Entity<FunctionUser>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("function_user_pkey");

                entity.ToTable("ums_function_user", "ums");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.FunctionName)
                    .HasColumnType("character varying")
                    .HasColumnName("function_name");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User).WithMany(p => p.FunctionUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("function_user_fk");
            });

            modelBuilder.Entity<GlobalPermission>(entity =>
            {
                entity.HasKey(e => e.PermissionId).HasName("ums_globalpermissions_pkey");

                entity.ToTable("ums_globalpermissions", "ums");

                entity.HasIndex(e => new { e.RoleId, e.SchemaName }, "uq_role_schema").IsUnique();

                entity.Property(e => e.PermissionId).HasColumnName("permission_id");
                entity.Property(e => e.CreateGrant)
                    .HasDefaultValue(false)
                    .HasColumnName("grant_create_db");
                entity.Property(e => e.CreateTableGrant)
                    .HasDefaultValue(false)
                    .HasColumnName("grant_create_obj");
                entity.Property(e => e.DeleteTableGrant)
                    .HasDefaultValue(false)
                    .HasColumnName("grant_delete_tbl");
                entity.Property(e => e.UpdateTableGrant)
                    .HasDefaultValue(false)
                    .HasColumnName("grant_update_tbl");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.SchemaName).HasColumnName("schema_name");

                entity.HasOne(d => d.Role).WithMany(p => p.GlobalPermissions)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("ums_globalpermissions_role_id_fkey");
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("pk_permissions");

                entity.ToTable("ums_permissions", "ums");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Operation).HasColumnName("operation");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.TableName).HasColumnName("table_name");

                entity.HasOne(d => d.Role).WithMany(p => p.Permissions)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_role_id");
            });

            modelBuilder.Entity<ProcedureUser>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("procedure_user_pkey");

                entity.ToTable("ums_procedure_user", "ums");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProcedureName)
                    .HasColumnType("character varying")
                    .HasColumnName("procedure_name");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User).WithMany(p => p.ProcedureUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("procedure_user_fk");
            });

            modelBuilder.Entity<RequestLog>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("ums_request_logs_pkey");

                entity.ToTable("ums_request_logs", "ums");

                entity.HasIndex(e => e.UserId, "idx_request_logs_user_id");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Duration).HasColumnName("duration");
                entity.Property(e => e.IPAddress)
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

                entity.HasOne(d => d.User).WithMany(p => p.RequestUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("fk_user");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId).HasName("ums_roles_pkey");

                entity.ToTable("ums_roles", "ums");

                entity.Property(e => e.RoleId).HasColumnName("id");
                entity.Property(e => e.RoleName).HasColumnName("role_name");
            });

            modelBuilder.Entity<TableUser>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("table_user_pkey");

                entity.ToTable("ums_table_user", "ums");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Tablename)
                    .HasColumnType("character varying")
                    .HasColumnName("tablename");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User).WithMany(p => p.TableUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("table_user_fk");
            });

            modelBuilder.Entity<TriggerUser>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("trigger_user_pkey");

                entity.ToTable("ums_trigger_user", "ums");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TriggerName)
                    .HasColumnType("character varying")
                    .HasColumnName("trigger_name");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.User).WithMany(p => p.TriggerUsers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("trigger_user_fk");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("ums_users_pkey");

                entity.ToTable("ums_users", "ums");

                entity.HasIndex(e => e.Username, "ums_unique_username").IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.Username).HasColumnName("username");
            });

            modelBuilder.Entity<UserAuthToken>(entity =>
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

                entity.HasOne(d => d.User).WithOne(p => p.UserAuthToken)
                    .HasForeignKey<UserAuthToken>(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_user_id");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.UserRoleId).HasName("pk_user_role");

                entity.ToTable("ums_user_role", "ums");

                entity.HasIndex(e => e.RoleId, "IX_UserRole_RoleId");

                entity.HasIndex(e => e.UserId, "IX_UserRole_UserId");

                entity.Property(e => e.UserRoleId).HasColumnName("id");
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");

                entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("fk_user_role_roles_role_id");

                entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("fk_user_role_users_user_id");
            });
      

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    }
}