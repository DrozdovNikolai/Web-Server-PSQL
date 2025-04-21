global using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuperHeroAPI.md2;
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


        public virtual DbSet<Attendance> Attendances { get; set; } = null!;



        public virtual DbSet<Auditorium> Auditoria { get; set; } = null!;

        public virtual DbSet<Contract> Contracts { get; set; } = null!;

        public virtual DbSet<Course> Courses { get; set; } = null!;

        public virtual DbSet<CourseWork> CourseWorks { get; set; } = null!;

        public virtual DbSet<Day> Days { get; set; } = null!;

        public virtual DbSet<Departament> Departaments { get; set; } = null!;

        public virtual DbSet<Direction> Directions { get; set; } = null!;

        public virtual DbSet<Discipline> Disciplines { get; set; } = null!;

        public virtual DbSet<ForSchedLec> ForSchedLecs { get; set; } = null!;

        public virtual DbSet<ForSchedPrac> ForSchedPracs { get; set; } = null!;

        public virtual DbSet<Grade> Grades { get; set; } = null!;

        public virtual DbSet<Group> Groups { get; set; } = null!;

        public virtual DbSet<Journal> Journals { get; set; } = null!;

        public virtual DbSet<KursVkr> KursVkrs { get; set; } = null!;

        public virtual DbSet<LGroup> LGroups { get; set; } = null!;

        public virtual DbSet<LGroupsDay> LGroupsDays { get; set; } = null!;

        public virtual DbSet<LWishDay> LWishDays { get; set; } = null!;

        public virtual DbSet<Listener> Listeners { get; set; } = null!;

        public virtual DbSet<ListenerWish> ListenerWishes { get; set; } = null!;

        public virtual DbSet<PayGraph> PayGraphs { get; set; } = null!;

        public virtual DbSet<Payer> Payers { get; set; } = null!;

        public virtual DbSet<Permission> Permissions { get; set; } = null!;
       
        public virtual DbSet<Position> Positions { get; set; } = null!;

        public virtual DbSet<Profile> Profiles { get; set; } = null!;

        public virtual DbSet<Program_u> Programs { get; set; } = null!;

        public virtual DbSet<Role> Roles { get; set; } = null!;

        public virtual DbSet<Schedule> Schedules { get; set; } = null!;

        public virtual DbSet<ScientificAdvisorsCourseWorkReport> ScientificAdvisorsCourseWorkReports { get; set; } = null!;

        public virtual DbSet<Student> Students { get; set; } = null!;

        public virtual DbSet<StudentEducationFormReport> StudentEducationFormReports { get; set; } = null!;

        public virtual DbSet<Subject> Subjects { get; set; } = null!;

        public virtual DbSet<SuperHero> SuperHeroes { get; set; } = null!;

        public virtual DbSet<TeachGruz> TeachGruzs { get; set; } = null!;

        public virtual DbSet<Teacher> Teachers { get; set; } = null!;

        public virtual DbSet<Teachschedule> Teachschedules { get; set; } = null!;

        public virtual DbSet<Tegrsu> Tegrsus { get; set; } = null!;

        public virtual DbSet<TempDepartament> TempDepartaments { get; set; } = null!;

        public virtual DbSet<TempDistribKit> TempDistribKits { get; set; } = null!;

        public virtual DbSet<TempFacName> TempFacNames { get; set; } = null!;

        public virtual DbSet<TempItogVo> TempItogVos { get; set; } = null!;

        public virtual DbSet<TempOfoVo> TempOfoVos { get; set; } = null!;

        public virtual DbSet<TempPractice> TempPractices { get; set; } = null!;

        public virtual DbSet<TempProffesion> TempProffesions { get; set; } = null!;

        public virtual DbSet<TempSostav> TempSostavs { get; set; } = null!;

        public virtual DbSet<TempSostavTest> TempSostavTests { get; set; } = null!;

        public virtual DbSet<TempTaskType> TempTaskTypes { get; set; } = null!;

        public virtual DbSet<TempTeachGruz> TempTeachGruzs { get; set; } = null!;

        public virtual DbSet<Tsch> Tsches { get; set; } = null!;

        public virtual DbSet<User> Users { get; set; } = null!;

        public virtual DbSet<UserRole> UserRoles { get; set; } = null!;

        public virtual DbSet<Workload> Workloads { get; set; } = null!;

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
                entity.HasKey(e => e.Id).HasName("pk_globalpermissions");

                entity.ToTable("ums_globalpermissions", "ums");

                entity.Property(e => e.Id).HasColumnName("permission_id");
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

                entity.HasOne(d => d.Role).WithMany(p => p.GlobalPermissions)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_role_id");
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
                    .HasForeignKey<UserAuthToken>(d => d.Id)
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
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(e => e.AttendanceId).HasName("attendances_pkey");

                entity.ToTable("attendances");

                entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
                entity.Property(e => e.AtDate).HasColumnName("at_date");
                entity.Property(e => e.AtStudentId).HasColumnName("at_student_id");
                entity.Property(e => e.AtTegrsuId).HasColumnName("at_tegrsu_id");
                entity.Property(e => e.Status).HasColumnName("status");
            });

            modelBuilder.Entity<AuditTableStudent>(entity =>
            {
                entity.HasKey(e => e.AuditId).HasName("audit_table_students_pkey");

                entity.ToTable("audit_table_students");

                entity.Property(e => e.AuditId).HasColumnName("audit_id");
                entity.Property(e => e.Course).HasColumnName("course");
                entity.Property(e => e.CourseWorkId).HasColumnName("course_work_id");
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .HasColumnName("email");
                entity.Property(e => e.EnrolledDate).HasColumnName("enrolled_date");
                entity.Property(e => e.EnrollmentOrder)
                    .HasMaxLength(50)
                    .HasColumnName("enrollment_order");
                entity.Property(e => e.FirstName)
                    .HasMaxLength(255)
                    .HasColumnName("first_name");
                entity.Property(e => e.Gender)
                    .HasMaxLength(1)
                    .HasColumnName("gender");
                entity.Property(e => e.GroupId).HasColumnName("group_id");
                entity.Property(e => e.Inn)
                    .HasMaxLength(20)
                    .HasColumnName("INN");
                entity.Property(e => e.LastName)
                    .HasMaxLength(255)
                    .HasColumnName("last_name");
                entity.Property(e => e.Operation)
                    .HasMaxLength(1)
                    .HasColumnName("operation");
                entity.Property(e => e.PassportSeriesAndNumber)
                    .HasMaxLength(20)
                    .HasColumnName("passport_series_and_number");
                entity.Property(e => e.Patronymic)
                    .HasMaxLength(255)
                    .HasColumnName("patronymic");
                entity.Property(e => e.PlaceOfBirth)
                    .HasMaxLength(255)
                    .HasColumnName("place_of_birth");
                entity.Property(e => e.Snils)
                    .HasMaxLength(20)
                    .HasColumnName("SNILS");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.StudentLogin)
                    .HasMaxLength(255)
                    .HasColumnName("student_login");
                entity.Property(e => e.Timestamp)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .HasColumnName("timestamp");
            });

            modelBuilder.Entity<Auditorium>(entity =>
            {
                entity.HasKey(e => e.AudId).HasName("auditorium_pkey");

                entity.ToTable("auditorium");

                entity.Property(e => e.AudId).HasColumnName("aud_id");
                entity.Property(e => e.Count).HasColumnName("count");
                entity.Property(e => e.Number)
                    .HasMaxLength(50)
                    .HasColumnName("number");
                entity.Property(e => e.Type)
                    .HasMaxLength(200)
                    .HasColumnName("type");
            });

            modelBuilder.Entity<Contract>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("contracts_pkey");

                entity.ToTable("contracts");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CertDate).HasColumnName("cert_date");
                entity.Property(e => e.ContrNumber)
                    .HasMaxLength(30)
                    .HasColumnName("contr_number");
                entity.Property(e => e.DateEnroll).HasColumnName("date_enroll");
                entity.Property(e => e.DateKick).HasColumnName("date_kick");
                entity.Property(e => e.GroupToMove).HasColumnName("group_to_move");
                entity.Property(e => e.ListenedHours).HasColumnName("listened_hours");
                entity.Property(e => e.ListenerId).HasColumnName("listener_id");
                entity.Property(e => e.PayerId).HasColumnName("payer_id");
                entity.Property(e => e.ProgramId).HasColumnName("program_id");

                entity.HasOne(d => d.GroupToMoveNavigation).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.GroupToMove)
                    .HasConstraintName("gr_id");

                entity.HasOne(d => d.Listener).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.ListenerId)
                    .HasConstraintName("lr_id");

                entity.HasOne(d => d.Payer).WithMany(p => p.Contracts)
                    .HasForeignKey(d => d.PayerId)
                    .HasConstraintName("pay_id");
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.CourseId).HasName("courses_pkey");

                entity.ToTable("courses");

                entity.Property(e => e.CourseId).HasColumnName("course_id");
                entity.Property(e => e.Course1)
                    .HasMaxLength(1)
                    .HasColumnName("course");
                entity.Property(e => e.GroupId).HasColumnName("group_id");
            });

            modelBuilder.Entity<CourseWork>(entity =>
            {
                entity.HasKey(e => e.CourseWorkId).HasName("course_work_pkey");

                entity.ToTable("course_work");

                entity.Property(e => e.CourseWorkId).HasColumnName("course_work_id");
                entity.Property(e => e.CourseWorkKafedra).HasColumnName("course_work_kafedra");
                entity.Property(e => e.CourseWorkOcenka).HasColumnName("course_work_ocenka");
                entity.Property(e => e.CourseWorkStudentId).HasColumnName("course_work_student_id");
                entity.Property(e => e.CourseWorkTeacherId).HasColumnName("course_work_teacher_id");
                entity.Property(e => e.CourseWorkTheme)
                    .HasMaxLength(300)
                    .HasColumnName("course_work_theme");
                entity.Property(e => e.CourseWorkVipysk).HasColumnName("course_work_vipysk");
                entity.Property(e => e.CourseWorkYear).HasColumnName("course_work_year");

                entity.HasOne(d => d.CourseWorkKafedraNavigation).WithMany(p => p.CourseWorks)
                    .HasForeignKey(d => d.CourseWorkKafedra)
                    .HasConstraintName("fk_kafedra");

                entity.HasOne(d => d.CourseWorkStudent).WithMany(p => p.CourseWorks)
                    .HasForeignKey(d => d.CourseWorkStudentId)
                    .HasConstraintName("fk_std");

                entity.HasOne(d => d.CourseWorkTeacher).WithMany(p => p.CourseWorks)
                    .HasForeignKey(d => d.CourseWorkTeacherId)
                    .HasConstraintName("fk_teacher");
            });

            modelBuilder.Entity<Day>(entity =>
            {
                entity.HasKey(e => e.DayId).HasName("day_pkey");

                entity.ToTable("days");

                entity.Property(e => e.DayId)
                    .ValueGeneratedNever()
                    .HasColumnName("day_id");
                entity.Property(e => e.Dayofweek)
                    .HasMaxLength(50)
                    .HasColumnName("dayofweek");
            });

            modelBuilder.Entity<Departament>(entity =>
            {
                entity.HasKey(e => e.DepId).HasName("departaments_pkey");

                entity.ToTable("departaments");

                entity.Property(e => e.DepId).HasColumnName("dep_id");
                entity.Property(e => e.DepAbb)
                    .HasMaxLength(50)
                    .HasColumnName("dep_abb");
                entity.Property(e => e.DepName)
                    .HasMaxLength(200)
                    .HasColumnName("dep_name");
            });

            modelBuilder.Entity<Direction>(entity =>
            {
                entity.HasKey(e => e.DirId).HasName("directions_pkey");

                entity.ToTable("directions");

                entity.Property(e => e.DirId).HasColumnName("dir_id");
                entity.Property(e => e.DirCode)
                    .HasMaxLength(20)
                    .HasColumnName("dir_code");
                entity.Property(e => e.DirName)
                    .HasMaxLength(100)
                    .HasColumnName("dir_name");
                entity.Property(e => e.Magister).HasColumnName("magister");
            });

            modelBuilder.Entity<Discipline>(entity =>
            {
                entity.HasKey(e => e.DisciplinesId).HasName("disciplines_pkey");

                entity.ToTable("disciplines");

                entity.Property(e => e.DisciplinesId)
                    .UseIdentityAlwaysColumn()
                    .HasColumnName("disciplines_id");
                entity.Property(e => e.DisciplineName)
                    .HasMaxLength(200)
                    .HasColumnName("discipline_name");
                entity.Property(e => e.GroupNumber).HasColumnName("group_number");
            });

            modelBuilder.Entity<ForSchedLec>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("for_sched_lec_pkey");

                entity.ToTable("for_sched_lec");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.Disc)
                    .HasMaxLength(200)
                    .HasColumnName("disc");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Fio)
                    .HasMaxLength(200)
                    .HasColumnName("fio");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.LecH).HasColumnName("lec_h");
                entity.Property(e => e.Napr)
                    .HasMaxLength(10)
                    .HasColumnName("napr");
                entity.Property(e => e.NumberOfGroups).HasColumnName("number_of_groups");
                entity.Property(e => e.NumberOfStreams).HasColumnName("number_of_streams");
                entity.Property(e => e.NumberOfSubgroups).HasColumnName("number_of_subgroups");
            });

            modelBuilder.Entity<ForSchedPrac>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("for_sched_prac_pkey");

                entity.ToTable("for_sched_prac");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.Disc)
                    .HasMaxLength(200)
                    .HasColumnName("disc");
                entity.Property(e => e.DisplayAudit)
                    .HasMaxLength(200)
                    .HasColumnName("display_audit");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Fio)
                    .HasMaxLength(200)
                    .HasColumnName("fio");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.Napr)
                    .HasMaxLength(10)
                    .HasColumnName("napr");
                entity.Property(e => e.NumberOfSubgroups).HasColumnName("number_of_subgroups");
            });

            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.GradeId).HasName("grades_pkey");

                entity.ToTable("grades");

                entity.Property(e => e.GradeId).HasColumnName("grade_id");
                entity.Property(e => e.GrDate).HasColumnName("gr_date");
                entity.Property(e => e.GrStudentId).HasColumnName("gr_student_id");
                entity.Property(e => e.GrTegrsuId).HasColumnName("gr_tegrsu_id");
                entity.Property(e => e.Grade1).HasColumnName("grade");
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.GroupId).HasName("groups_pkey");

                entity.ToTable("groups");

                entity.Property(e => e.GroupId).HasColumnName("group_id");
                entity.Property(e => e.Course)
                    .HasDefaultValueSql("1")
                    .HasColumnName("course");
                entity.Property(e => e.GroupDirId).HasColumnName("group_dir_id");
                entity.Property(e => e.GroupNumber)
                    .HasMaxLength(50)
                    .HasColumnName("group_number");
                entity.Property(e => e.GroupProfId).HasColumnName("group_prof_id");
                entity.Property(e => e.Magister)
                    .HasDefaultValueSql("false")
                    .HasColumnName("magister");

                entity.HasOne(d => d.GroupDir).WithMany(p => p.Groups)
                    .HasForeignKey(d => d.GroupDirId)
                    .HasConstraintName("fk_dir");

                entity.HasOne(d => d.GroupProf).WithMany(p => p.Groups)
                    .HasForeignKey(d => d.GroupProfId)
                    .HasConstraintName("fk_main");
            });

            modelBuilder.Entity<Journal>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("journal");

                entity.Property(e => e.Date).HasColumnName("date");
                entity.Property(e => e.Grade).HasColumnName("grade");
                entity.Property(e => e.GroupId).HasColumnName("group_id");
                entity.Property(e => e.JId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("j_id");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");
                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            });

            modelBuilder.Entity<KursVkr>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("kurs_VKR_pkey");

                entity.ToTable("kurs_VKR");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Form)
                    .HasMaxLength(200)
                    .HasColumnName("form");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.Napr)
                    .HasMaxLength(10)
                    .HasColumnName("napr");
                entity.Property(e => e.StFio)
                    .HasMaxLength(50)
                    .HasColumnName("st_fio");
                entity.Property(e => e.TeachFio)
                    .HasMaxLength(100)
                    .HasColumnName("teach_fio");
                entity.Property(e => e.Type)
                    .HasMaxLength(20)
                    .HasColumnName("type");
                entity.Property(e => e.Years)
                    .HasMaxLength(10)
                    .HasColumnName("years");
            });

            modelBuilder.Entity<LGroup>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("l_groups_pkey");

                entity.ToTable("l_groups");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.Endtime).HasColumnName("endtime");
                entity.Property(e => e.GroupNumber)
                    .HasMaxLength(50)
                    .HasColumnName("group_number");
                entity.Property(e => e.GroupProgramId).HasColumnName("group_program_id");
                entity.Property(e => e.Hours).HasColumnName("hours");
                entity.Property(e => e.PeopleCount).HasColumnName("people_count");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.Starttime).HasColumnName("starttime");
            });

            modelBuilder.Entity<LGroupsDay>(entity =>
            {
                entity.HasKey(e => e.LGroupsDaysId).HasName("l_groups_days");

                entity.ToTable("l_groups_day");

                entity.Property(e => e.LGroupsDaysId)
                    .UseIdentityAlwaysColumn()
                    .HasColumnName("l_groups_days_id");
                entity.Property(e => e.DayId).HasColumnName("day_id");
                entity.Property(e => e.Endtime).HasColumnName("endtime");
                entity.Property(e => e.LGroups).HasColumnName("l_groups");
                entity.Property(e => e.Starttime).HasColumnName("starttime");

                entity.HasOne(d => d.LGroupsNavigation).WithMany(p => p.LGroupsDays)
                    .HasForeignKey(d => d.LGroups)
                    .HasConstraintName("l_groups");
            });

            modelBuilder.Entity<LWishDay>(entity =>
            {
                entity.HasKey(e => e.LWishDayId).HasName("l_wish_day");

                entity.ToTable("l_wish_days");

                entity.Property(e => e.LWishDayId)
                    .UseIdentityAlwaysColumn()
                    .HasColumnName("l_wish_day_id");
                entity.Property(e => e.DayId).HasColumnName("day_id");
                entity.Property(e => e.Endtime).HasColumnName("endtime");
                entity.Property(e => e.ListenerId).HasColumnName("listener_id");
                entity.Property(e => e.Starttime).HasColumnName("starttime");

                entity.HasOne(d => d.Listener).WithMany(p => p.LWishDays)
                    .HasForeignKey(d => d.ListenerId)
                    .HasConstraintName("list");
            });

            modelBuilder.Entity<Listener>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("listeners_pkey");

                entity.ToTable("listeners");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DepartmentCode)
                    .HasMaxLength(10)
                    .HasColumnName("department_code");
                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .HasColumnName("email");
                entity.Property(e => e.GroupId).HasColumnName("group_id");
                entity.Property(e => e.IssueDate).HasColumnName("issue_date");
                entity.Property(e => e.IssuedBy)
                    .HasMaxLength(255)
                    .HasColumnName("issued_by");
                entity.Property(e => e.Lastname)
                    .HasMaxLength(255)
                    .HasColumnName("lastname");
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
                entity.Property(e => e.Passport)
                    .HasMaxLength(20)
                    .HasColumnName("passport");
                entity.Property(e => e.PeopleCount).HasColumnName("people_count");
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(15)
                    .HasColumnName("phone_number");
                entity.Property(e => e.RegistrationAddress).HasColumnName("registration_address");
                entity.Property(e => e.Snils)
                    .HasMaxLength(30)
                    .HasColumnName("snils");
                entity.Property(e => e.Surname)
                    .HasMaxLength(255)
                    .HasColumnName("surname");
            });

            modelBuilder.Entity<ListenerWish>(entity =>
            {
                entity.HasKey(e => e.WishId).HasName("listener_wishes_pkey");

                entity.ToTable("listener_wishes");

                entity.Property(e => e.WishId).HasColumnName("wish_id");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.Hours).HasColumnName("hours");
                entity.Property(e => e.ListenerId).HasColumnName("listener_id");
                entity.Property(e => e.PeopleCount).HasColumnName("people_count");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
                entity.Property(e => e.SuitableDays).HasColumnName("suitable_days");
                entity.Property(e => e.WishDescription).HasColumnName("wish_description");
            });

            modelBuilder.Entity<PayGraph>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("pay_graph_pkey");

                entity.ToTable("pay_graph");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AllSum)
                    .HasPrecision(10, 2)
                    .HasColumnName("all_sum");
                entity.Property(e => e.Bank)
                    .HasMaxLength(50)
                    .HasColumnName("bank");
                entity.Property(e => e.ContractId).HasColumnName("contract_id");
                entity.Property(e => e.Date40).HasColumnName("date_40");
                entity.Property(e => e.DepositedAmount)
                    .HasPrecision(10, 2)
                    .HasDefaultValueSql("0.00")
                    .HasColumnName("deposited_amount");
                entity.Property(e => e.ExpirationDate).HasColumnName("expiration_date");
                entity.Property(e => e.LeftToPay)
                    .HasPrecision(10, 2)
                    .HasColumnName("left_to_pay");

                entity.HasOne(d => d.Contract).WithMany(p => p.PayGraphs)
                    .HasForeignKey(d => d.ContractId)
                    .HasConstraintName("contract");
            });

            modelBuilder.Entity<Payer>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("payers_pkey");

                entity.ToTable("payers");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.DepartmentCode)
                    .HasMaxLength(10)
                    .HasColumnName("department_code");
                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .HasColumnName("email");
                entity.Property(e => e.IssueDate).HasColumnName("issue_date");
                entity.Property(e => e.IssuedBy)
                    .HasMaxLength(255)
                    .HasColumnName("issued_by");
                entity.Property(e => e.Lastname)
                    .HasMaxLength(255)
                    .HasColumnName("lastname");
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
                entity.Property(e => e.Passport)
                    .HasMaxLength(20)
                    .HasColumnName("passport");
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(15)
                    .HasColumnName("phone_number");
                entity.Property(e => e.RegistrationAddress).HasColumnName("registration_address");
                entity.Property(e => e.Snils)
                    .HasMaxLength(30)
                    .HasColumnName("snils");
                entity.Property(e => e.Surname)
                    .HasMaxLength(255)
                    .HasColumnName("surname");
            });

            modelBuilder.Entity<Position>(entity =>
            {
                entity.HasKey(e => e.PositionId).HasName("positions_pkey");

                entity.ToTable("positions");

                entity.Property(e => e.PositionId).HasColumnName("position_id");
                entity.Property(e => e.PosName)
                    .HasMaxLength(255)
                    .HasColumnName("pos_name");
            });

            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasKey(e => e.ProfId).HasName("profiles_pkey");

                entity.ToTable("profiles");

                entity.Property(e => e.ProfId).HasColumnName("prof_id");
                entity.Property(e => e.ProfDirId).HasColumnName("prof_dir_id");
                entity.Property(e => e.ProfName)
                    .HasMaxLength(100)
                    .HasColumnName("prof_name");

                entity.HasOne(d => d.ProfDir).WithMany(p => p.Profiles)
                    .HasForeignKey(d => d.ProfDirId)
                    .HasConstraintName("pr_dir_id");
            });

            modelBuilder.Entity<Program_u>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("programs_pkey");

                entity.ToTable("programs");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EndDate).HasColumnName("end_date");
                entity.Property(e => e.Hours).HasColumnName("hours");
                entity.Property(e => e.ProgramName)
                    .HasMaxLength(255)
                    .HasColumnName("program_name");
                entity.Property(e => e.RequiredAmount)
                    .HasPrecision(10, 2)
                    .HasColumnName("required_amount");
                entity.Property(e => e.StartDate).HasColumnName("start_date");
            });

            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasKey(e => e.ScheduleId).HasName("schedule_pkey");

                entity.ToTable("schedule");

                entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
                entity.Property(e => e.AudId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("aud_id");
                entity.Property(e => e.DayId).HasColumnName("day_id");
                entity.Property(e => e.GroupId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("group_id");
                entity.Property(e => e.SubjectId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("subject_id");
                entity.Property(e => e.TeacherId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("teacher_id");
                entity.Property(e => e.Timerange)
                    .HasMaxLength(30)
                    .HasColumnName("timerange");

                entity.HasOne(d => d.Aud).WithMany(p => p.Schedules)
                    .HasForeignKey(d => d.AudId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("schedule_aud_id_fkey");

                entity.HasOne(d => d.Day).WithMany(p => p.Schedules)
                    .HasForeignKey(d => d.DayId)
                    .HasConstraintName("schedule_day_id_fkey");

                entity.HasOne(d => d.Group).WithMany(p => p.Schedules)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("schedule_group_id_fkey");

                entity.HasOne(d => d.Subject).WithMany(p => p.Schedules)
                    .HasForeignKey(d => d.SubjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("schedule_subject_id_fkey");

                entity.HasOne(d => d.Teacher).WithMany(p => p.Schedules)
                    .HasForeignKey(d => d.TeacherId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("schedule_teacher_id_fkey");
            });

            modelBuilder.Entity<ScientificAdvisorsCourseWorkReport>(entity =>
            {
                entity.HasKey(e => e.ReportId).HasName("scientific_advisors_course_work_report_pkey");

                entity.ToTable("scientific_advisors_course_work_report");

                entity.Property(e => e.ReportId).HasColumnName("report_id");
                entity.Property(e => e.ReportContent).HasColumnName("report_content");
                entity.Property(e => e.ReportDate).HasColumnName("report_date");

                entity.HasMany(d => d.CourseWorks).WithMany(p => p.Reports)
                    .UsingEntity<Dictionary<string, object>>(
                        "ScientificAdvisorsCourseWorkReportCourse",
                        r => r.HasOne<CourseWork>().WithMany()
                            .HasForeignKey("CourseWorkId")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("scientific_advisors_course_work_report_cour_course_work_id_fkey"),
                        l => l.HasOne<ScientificAdvisorsCourseWorkReport>().WithMany()
                            .HasForeignKey("ReportId")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("scientific_advisors_course_work_report_courses_report_id_fkey"),
                        j =>
                        {
                            j.HasKey("ReportId", "CourseWorkId").HasName("scientific_advisors_course_work_report_courses_pkey");
                            j.ToTable("scientific_advisors_course_work_report_courses");
                            j.IndexerProperty<int>("ReportId").HasColumnName("report_id");
                            j.IndexerProperty<int>("CourseWorkId").HasColumnName("course_work_id");
                        });
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId).HasName("students_pkey");

                entity.ToTable("students");

                entity.Property(e => e.StudentId).HasColumnName("student_id");
                entity.Property(e => e.Course).HasColumnName("course");
                entity.Property(e => e.CourseWorkId).HasColumnName("course_work_id");
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .HasColumnName("email");
                entity.Property(e => e.EnrolledDate).HasColumnName("enrolled_date");
                entity.Property(e => e.EnrollmentOrder)
                    .HasMaxLength(100)
                    .HasColumnName("enrollment_order");
                entity.Property(e => e.FirstName)
                    .HasMaxLength(255)
                    .HasColumnName("first_name");
                entity.Property(e => e.Gender)
                    .HasMaxLength(10)
                    .HasColumnName("gender");
                entity.Property(e => e.GroupId).HasColumnName("group_id");
                entity.Property(e => e.Inn)
                    .HasMaxLength(20)
                    .HasColumnName("INN");
                entity.Property(e => e.IsBudget).HasColumnName("is_budget");
                entity.Property(e => e.LastName)
                    .HasMaxLength(255)
                    .HasColumnName("last_name");
                entity.Property(e => e.PassportSeriesAndNumber)
                    .HasMaxLength(20)
                    .HasColumnName("passport_series_and_number");
                entity.Property(e => e.Patronymic)
                    .HasMaxLength(255)
                    .HasColumnName("patronymic");
                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(15)
                    .HasColumnName("phone_number");
                entity.Property(e => e.PhoneNumberRod)
                    .HasMaxLength(15)
                    .HasColumnName("phone_number_rod");
                entity.Property(e => e.PlaceOfBirth)
                    .HasMaxLength(255)
                    .HasColumnName("place_of_birth");
                entity.Property(e => e.Snils)
                    .HasMaxLength(20)
                    .HasColumnName("SNILS");
                entity.Property(e => e.StudentLogin)
                    .HasMaxLength(100)
                    .HasColumnName("student_login");
                entity.Property(e => e.Subgroup)
                    .HasMaxLength(3)
                    .HasColumnName("subgroup");
                entity.Property(e => e.ZachetkaNumber)
                    .HasMaxLength(15)
                    .HasColumnName("zachetka_number");

                entity.HasOne(d => d.Group).WithMany(p => p.Students)
                    .HasForeignKey(d => d.GroupId)
                    .HasConstraintName("fk_group");
            });

            modelBuilder.Entity<StudentEducationFormReport>(entity =>
            {
                entity.HasKey(e => e.ReportId).HasName("student_education_form_report_pkey");

                entity.ToTable("student_education_form_report");

                entity.Property(e => e.ReportId).HasColumnName("report_id");
                entity.Property(e => e.EducationForm)
                    .HasMaxLength(100)
                    .HasColumnName("education_form");
                entity.Property(e => e.ReportContent).HasColumnName("report_content");
                entity.Property(e => e.ReportDate).HasColumnName("report_date");

                entity.HasMany(d => d.Students).WithMany(p => p.Reports)
                    .UsingEntity<Dictionary<string, object>>(
                        "StudentEducationFormReportStudent",
                        r => r.HasOne<Student>().WithMany()
                            .HasForeignKey("StudentId")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("student_education_form_report_students_student_id_fkey"),
                        l => l.HasOne<StudentEducationFormReport>().WithMany()
                            .HasForeignKey("ReportId")
                            .OnDelete(DeleteBehavior.ClientSetNull)
                            .HasConstraintName("student_education_form_report_students_report_id_fkey"),
                        j =>
                        {
                            j.HasKey("ReportId", "StudentId").HasName("student_education_form_report_students_pkey");
                            j.ToTable("student_education_form_report_students");
                            j.IndexerProperty<int>("ReportId").HasColumnName("report_id");
                            j.IndexerProperty<int>("StudentId").HasColumnName("student_id");
                        });
            });

            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasKey(e => e.SubjectId).HasName("subjects_pkey");

                entity.ToTable("subjects");

                entity.Property(e => e.SubjectId).HasColumnName("subject_id");
                entity.Property(e => e.SubType)
                    .HasMaxLength(255)
                    .HasColumnName("sub_type");
                entity.Property(e => e.SubjectName)
                    .HasMaxLength(200)
                    .HasColumnName("subject_name");
            });

            modelBuilder.Entity<TeachGruz>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("teach_gruz_pkey");

                entity.ToTable("teach_gruz");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CodeNapr)
                    .HasMaxLength(50)
                    .HasColumnName("code_napr");
                entity.Property(e => e.ConsultH).HasColumnName("consult_h");
                entity.Property(e => e.ControlH).HasColumnName("control_h");
                entity.Property(e => e.DisName)
                    .HasMaxLength(200)
                    .HasColumnName("dis_name");
                entity.Property(e => e.ExamH).HasColumnName("exam_h");
                entity.Property(e => e.Fam)
                    .HasMaxLength(50)
                    .HasColumnName("fam");
                entity.Property(e => e.GekH).HasColumnName("GEK_h");
                entity.Property(e => e.KontBudg).HasColumnName("kont_budg");
                entity.Property(e => e.KontDog).HasColumnName("kont_dog");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.KursachH).HasColumnName("kursach_h");
                entity.Property(e => e.LabH).HasColumnName("lab_h");
                entity.Property(e => e.LecH).HasColumnName("lec_h");
                entity.Property(e => e.MagistrH).HasColumnName("magistr_h");
                entity.Property(e => e.ManageH).HasColumnName("manage_h");
                entity.Property(e => e.NumberOfGroups).HasColumnName("number_of_groups");
                entity.Property(e => e.NumberOfStreams).HasColumnName("number_of_streams");
                entity.Property(e => e.NumberOfSubgroups).HasColumnName("number_of_subgroups");
                entity.Property(e => e.OtherH).HasColumnName("other_h");
                entity.Property(e => e.PracticeH).HasColumnName("practice_h");
                entity.Property(e => e.Semestr).HasColumnName("semestr");
                entity.Property(e => e.SeminarH).HasColumnName("seminar_h");
                entity.Property(e => e.VkrH).HasColumnName("VKR_h");
                entity.Property(e => e.ZachetH).HasColumnName("zachet_h");
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.HasKey(e => e.TeacherId).HasName("teachers_pkey");

                entity.ToTable("teachers");

                entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
                entity.Property(e => e.FirstName)
                    .HasMaxLength(50)
                    .HasColumnName("first_name");
                entity.Property(e => e.LastName)
                    .HasMaxLength(50)
                    .HasColumnName("last_name");
                entity.Property(e => e.Patronymic)
                    .HasMaxLength(50)
                    .HasColumnName("patronymic");
            });

            modelBuilder.Entity<Teachschedule>(entity =>
            {
                entity.HasKey(e => e.LessonId).HasName("teachschedule_pkey");

                entity.ToTable("teachschedule");

                entity.Property(e => e.LessonId).HasColumnName("lesson_id");
                entity.Property(e => e.DayId).HasColumnName("day_id");
                entity.Property(e => e.Time)
                    .HasMaxLength(20)
                    .HasColumnName("time");
                entity.Property(e => e.WlId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("wl_id");

                entity.HasOne(d => d.Day).WithMany(p => p.Teachschedules)
                    .HasForeignKey(d => d.DayId)
                    .HasConstraintName("teachschedule_day_id_fkey");

                entity.HasOne(d => d.Wl).WithMany(p => p.Teachschedules)
                    .HasForeignKey(d => d.WlId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("teachschedule_wl_id_fkey");
            });

            modelBuilder.Entity<Tegrsu>(entity =>
            {
                entity.HasKey(e => e.TegrsuId).HasName("tegrsus_pkey");

                entity.ToTable("tegrsus");

                entity.Property(e => e.TegrsuId).HasColumnName("tegrsu_id");
                entity.Property(e => e.TegrsuGroupId).HasColumnName("tegrsu_group_id");
                entity.Property(e => e.TegrsuSubjectId).HasColumnName("tegrsu_subject_id");
                entity.Property(e => e.TegrsuTeacherId).HasColumnName("tegrsu_teacher_id");
            });

            modelBuilder.Entity<TempDepartament>(entity =>
            {
                entity.HasKey(e => e.DepId).HasName("temp_departaments_pkey");

                entity.ToTable("temp_departaments");

                entity.Property(e => e.DepId).HasColumnName("dep_id");
                entity.Property(e => e.DepAbb)
                    .HasMaxLength(50)
                    .HasColumnName("dep_abb");
                entity.Property(e => e.DepName)
                    .HasMaxLength(200)
                    .HasColumnName("dep_name");
            });

            modelBuilder.Entity<TempDistribKit>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_distrib_KIT_pkey");

                entity.ToTable("temp_distrib_KIT");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Fio)
                    .HasMaxLength(200)
                    .HasColumnName("fio");
                entity.Property(e => e.Gek).HasColumnName("GEK");
                entity.Property(e => e.Kaf)
                    .HasMaxLength(200)
                    .HasColumnName("kaf");
                entity.Property(e => e.Lab).HasColumnName("lab");
                entity.Property(e => e.Lec).HasColumnName("lec");
                entity.Property(e => e.Practice).HasColumnName("practice");
                entity.Property(e => e.Sem).HasColumnName("sem");
                entity.Property(e => e.Sem1).HasColumnName("sem1");
                entity.Property(e => e.Sem2).HasColumnName("sem2");
                entity.Property(e => e.Vkr).HasColumnName("VKR");
            });

            modelBuilder.Entity<TempFacName>(entity =>
            {
                entity.HasKey(e => e.FacId).HasName("temp_fac_names_pkey");

                entity.ToTable("temp_fac_names");

                entity.Property(e => e.FacId).HasColumnName("fac_id");
                entity.Property(e => e.FullName)
                    .HasMaxLength(200)
                    .HasColumnName("full_name");
                entity.Property(e => e.ShortName)
                    .HasMaxLength(10)
                    .HasColumnName("short_name");
            });

            modelBuilder.Entity<TempItogVo>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_itogVO_pkey");

                entity.ToTable("temp_itogVO");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CheckPlagVkr).HasColumnName("checkPlagVKR");
                entity.Property(e => e.ConsEx).HasColumnName("cons_ex");
                entity.Property(e => e.ConsZfo).HasColumnName("cons_zfo");
                entity.Property(e => e.ControlWork).HasColumnName("control_work");
                entity.Property(e => e.DefVkr).HasColumnName("defVKR");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.ExSpeak).HasColumnName("ex_speak");
                entity.Property(e => e.ExWr).HasColumnName("ex_wr");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Gosexam).HasColumnName("GOSexam");
                entity.Property(e => e.LabBudj).HasColumnName("lab_budj");
                entity.Property(e => e.LabDogovor).HasColumnName("lab_dogovor");
                entity.Property(e => e.LekBudj).HasColumnName("lek_budj");
                entity.Property(e => e.LekDogovor).HasColumnName("lek_dogovor");
                entity.Property(e => e.Manag).HasColumnName("manag");
                entity.Property(e => e.ManageVkr).HasColumnName("manageVKR");
                entity.Property(e => e.NormContVkr).HasColumnName("normContVKR");
                entity.Property(e => e.PBudg).HasColumnName("p_budg");
                entity.Property(e => e.PDogovor).HasColumnName("p_dogovor");
                entity.Property(e => e.Practice).HasColumnName("practice");
                entity.Property(e => e.RecVkr).HasColumnName("recVKR");
                entity.Property(e => e.Years)
                    .HasMaxLength(50)
                    .HasColumnName("years");
                entity.Property(e => e.ZachetH).HasColumnName("zachet_h");
            });

            modelBuilder.Entity<TempOfoVo>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_OFO_VO_pkey");

                entity.ToTable("temp_OFO_VO");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CodeNapr)
                    .HasMaxLength(50)
                    .HasColumnName("code_napr");
                entity.Property(e => e.ConsEx).HasColumnName("cons_ex");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.DisName)
                    .HasMaxLength(200)
                    .HasColumnName("dis_name");
                entity.Property(e => e.ExSpeak).HasColumnName("ex_speak");
                entity.Property(e => e.ExWr).HasColumnName("ex_wr");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.KontBudg).HasColumnName("kont_budg");
                entity.Property(e => e.KontDog).HasColumnName("kont_dog");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.LabBudj).HasColumnName("lab_budj");
                entity.Property(e => e.LabDogovor).HasColumnName("lab_dogovor");
                entity.Property(e => e.LekBudj).HasColumnName("lek_budj");
                entity.Property(e => e.LekDogovor).HasColumnName("lek_dogovor");
                entity.Property(e => e.NumberOfGroups).HasColumnName("number_of_groups");
                entity.Property(e => e.NumberOfStreams).HasColumnName("number_of_streams");
                entity.Property(e => e.NumberOfSubgroups).HasColumnName("number_of_subgroups");
                entity.Property(e => e.PBudg).HasColumnName("p_budg");
                entity.Property(e => e.PDogovor).HasColumnName("p_dogovor");
                entity.Property(e => e.Practice).HasColumnName("practice");
                entity.Property(e => e.ZachetH).HasColumnName("zachet_h");
            });

            modelBuilder.Entity<TempPractice>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_practice_pkey");

                entity.ToTable("temp_practice");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.BudgH).HasColumnName("budg_h");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.DogH).HasColumnName("dog_h");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.Napr)
                    .HasMaxLength(200)
                    .HasColumnName("napr");
                entity.Property(e => e.NumberOfGroups).HasColumnName("number_of_groups");
                entity.Property(e => e.NumberOfSubgroups).HasColumnName("number_of_subgroups");
                entity.Property(e => e.NumberOfWeeks).HasColumnName("number_of_weeks");
                entity.Property(e => e.NumberOfWorkDays).HasColumnName("number_of_work_days");
                entity.Property(e => e.PracticeName)
                    .HasMaxLength(200)
                    .HasColumnName("practice_name");
                entity.Property(e => e.Profile)
                    .HasMaxLength(200)
                    .HasColumnName("profile");
                entity.Property(e => e.Semestr).HasColumnName("semestr");
                entity.Property(e => e.Srok)
                    .HasMaxLength(50)
                    .HasColumnName("srok");
                entity.Property(e => e.StudentsBudg).HasColumnName("students_budg");
                entity.Property(e => e.StudentsDog).HasColumnName("students_dog");
                entity.Property(e => e.Years)
                    .HasMaxLength(50)
                    .HasColumnName("years");
            });

            modelBuilder.Entity<TempProffesion>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_proffesions_pkey");

                entity.ToTable("temp_proffesions");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Dep)
                    .HasMaxLength(100)
                    .HasColumnName("dep");
                entity.Property(e => e.Fac)
                    .HasMaxLength(100)
                    .HasColumnName("fac");
                entity.Property(e => e.Kval)
                    .HasMaxLength(10)
                    .HasColumnName("kval");
                entity.Property(e => e.Napr)
                    .HasMaxLength(10)
                    .HasColumnName("napr");
                entity.Property(e => e.Proffesions)
                    .HasMaxLength(200)
                    .HasColumnName("proffesions");
                entity.Property(e => e.Profile)
                    .HasMaxLength(100)
                    .HasColumnName("profile");
                entity.Property(e => e.Srok).HasColumnName("srok");
            });

            modelBuilder.Entity<TempSostav>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_sostav_pkey");

                entity.ToTable("temp_sostav");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Deg)
                    .HasMaxLength(50)
                    .HasColumnName("deg");
                entity.Property(e => e.Dep)
                    .HasMaxLength(100)
                    .HasColumnName("dep");
                entity.Property(e => e.Dolj)
                    .HasMaxLength(50)
                    .HasColumnName("dolj");
                entity.Property(e => e.Fac)
                    .HasMaxLength(100)
                    .HasColumnName("fac");
                entity.Property(e => e.Name1)
                    .HasMaxLength(50)
                    .HasColumnName("name1");
                entity.Property(e => e.Name2)
                    .HasMaxLength(50)
                    .HasColumnName("name2");
                entity.Property(e => e.Name3)
                    .HasMaxLength(50)
                    .HasColumnName("name3");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");
            });

            modelBuilder.Entity<TempSostavTest>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_sostav_test_pkey");

                entity.ToTable("temp_sostav_test");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Deg)
                    .HasMaxLength(50)
                    .HasColumnName("deg");
                entity.Property(e => e.Dep)
                    .HasMaxLength(100)
                    .HasColumnName("dep");
                entity.Property(e => e.Dolj)
                    .HasMaxLength(50)
                    .HasColumnName("dolj");
                entity.Property(e => e.Fac)
                    .HasMaxLength(100)
                    .HasColumnName("fac");
                entity.Property(e => e.Name1)
                    .HasMaxLength(50)
                    .HasColumnName("name1");
                entity.Property(e => e.Name2)
                    .HasMaxLength(50)
                    .HasColumnName("name2");
                entity.Property(e => e.Name3)
                    .HasMaxLength(50)
                    .HasColumnName("name3");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasColumnName("status");
            });

            modelBuilder.Entity<TempTaskType>(entity =>
            {
                entity
                    .HasNoKey()
                    .ToTable("temp_task_type");

                entity.Property(e => e.Dep)
                    .HasMaxLength(100)
                    .HasColumnName("dep");
                entity.Property(e => e.Fac)
                    .HasMaxLength(100)
                    .HasColumnName("fac");
                entity.Property(e => e.Kval)
                    .HasMaxLength(10)
                    .HasColumnName("kval");
                entity.Property(e => e.Napr)
                    .HasMaxLength(10)
                    .HasColumnName("napr");
                entity.Property(e => e.Profile)
                    .HasMaxLength(100)
                    .HasColumnName("profile");
                entity.Property(e => e.Srok).HasColumnName("srok");
                entity.Property(e => e.TaskType)
                    .HasMaxLength(200)
                    .HasColumnName("task_type");
            });

            modelBuilder.Entity<TempTeachGruz>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("temp_teach_gruz_pkey");

                entity.ToTable("temp_teach_gruz");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CodeNapr)
                    .HasMaxLength(50)
                    .HasColumnName("code_napr");
                entity.Property(e => e.ConsultH).HasColumnName("consult_h");
                entity.Property(e => e.ControlH).HasColumnName("control_h");
                entity.Property(e => e.Dep)
                    .HasMaxLength(200)
                    .HasColumnName("dep");
                entity.Property(e => e.DisName)
                    .HasMaxLength(200)
                    .HasColumnName("dis_name");
                entity.Property(e => e.ExamH).HasColumnName("exam_h");
                entity.Property(e => e.Fac)
                    .HasMaxLength(200)
                    .HasColumnName("fac");
                entity.Property(e => e.Fam)
                    .HasMaxLength(50)
                    .HasColumnName("fam");
                entity.Property(e => e.GekH).HasColumnName("GEK_h");
                entity.Property(e => e.KontBudg).HasColumnName("kont_budg");
                entity.Property(e => e.KontDog).HasColumnName("kont_dog");
                entity.Property(e => e.Kurs).HasColumnName("kurs");
                entity.Property(e => e.KursachH).HasColumnName("kursach_h");
                entity.Property(e => e.LabH).HasColumnName("lab_h");
                entity.Property(e => e.LecH).HasColumnName("lec_h");
                entity.Property(e => e.MagistrH).HasColumnName("magistr_h");
                entity.Property(e => e.ManageH).HasColumnName("manage_h");
                entity.Property(e => e.NumberOfGroups).HasColumnName("number_of_groups");
                entity.Property(e => e.NumberOfStreams).HasColumnName("number_of_streams");
                entity.Property(e => e.NumberOfSubgroups).HasColumnName("number_of_subgroups");
                entity.Property(e => e.OtherH).HasColumnName("other_h");
                entity.Property(e => e.PracticeH).HasColumnName("practice_h");
                entity.Property(e => e.Semestr).HasColumnName("semestr");
                entity.Property(e => e.SeminarH).HasColumnName("seminar_h");
                entity.Property(e => e.VkrH).HasColumnName("VKR_h");
                entity.Property(e => e.ZachetH).HasColumnName("zachet_h");
            });

            modelBuilder.Entity<Tsch>(entity =>
            {
                entity.HasKey(e => e.TId).HasName("tsch_pkey");

                entity.ToTable("tsch");

                entity.Property(e => e.TId).HasColumnName("t_id");
                entity.Property(e => e.DayId).HasColumnName("day_id");
                entity.Property(e => e.DisName)
                    .HasMaxLength(50)
                    .HasColumnName("dis_name");
                entity.Property(e => e.TeacherId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("teacher_id");
                entity.Property(e => e.Time)
                    .HasMaxLength(20)
                    .HasColumnName("time");

                entity.HasOne(d => d.Day).WithMany(p => p.Tsches)
                    .HasForeignKey(d => d.DayId)
                    .HasConstraintName("tsch_day_id_fkey");

                entity.HasOne(d => d.Teacher).WithMany(p => p.Tsches)
                    .HasForeignKey(d => d.TeacherId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("tsch_teacher_id_fkey");
            });

            modelBuilder.Entity<Workload>(entity =>
            {
                entity.HasKey(e => e.WlId).HasName("workload_pkey");

                entity.ToTable("workload");

                entity.Property(e => e.WlId)
                    .HasDefaultValueSql("nextval('workload_seq'::regclass)")
                    .HasColumnName("wl_id");
                entity.Property(e => e.GroupId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("group_id");
                entity.Property(e => e.SubjectId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("subject_id");
                entity.Property(e => e.TeacherId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("teacher_id");

                entity.HasOne(d => d.Group).WithMany(p => p.Workloads)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("workload_group_id_fkey");

                entity.HasOne(d => d.Subject).WithMany(p => p.Workloads)
                    .HasForeignKey(d => d.SubjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("workload_subject_id_fkey");

                entity.HasOne(d => d.Teacher).WithMany(p => p.Workloads)
                    .HasForeignKey(d => d.TeacherId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("workload_teacher_id_fkey");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        public DbSet<SuperHeroAPI.md2.Program> Program { get; set; } = default!;
    }
}