using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SuperHeroAPI.Migrations
{
    /// <inheritdoc />
    public partial class NewRoleSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    attendance_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    at_student_id = table.Column<int>(type: "integer", nullable: true),
                    at_tegrsu_id = table.Column<int>(type: "integer", nullable: true),
                    at_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attendances_pkey", x => x.attendance_id);
                });

            migrationBuilder.CreateTable(
                name: "audit_table_students",
                columns: table => new
                {
                    audit_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: true),
                    first_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    last_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    patronymic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    gender = table.Column<char>(type: "character(1)", maxLength: 1, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    passport_series_and_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    INN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SNILS = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    place_of_birth = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    student_login = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    enrollment_order = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    enrolled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    course = table.Column<int>(type: "integer", nullable: true),
                    course_work_id = table.Column<int>(type: "integer", nullable: true),
                    group_id = table.Column<int>(type: "integer", nullable: true),
                    operation = table.Column<char>(type: "character(1)", maxLength: 1, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audit_table_students_pkey", x => x.audit_id);
                });

            migrationBuilder.CreateTable(
                name: "auditorium",
                columns: table => new
                {
                    aud_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("auditorium_pkey", x => x.aud_id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    course_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    course = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    group_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("courses_pkey", x => x.course_id);
                });

            migrationBuilder.CreateTable(
                name: "days",
                columns: table => new
                {
                    day_id = table.Column<int>(type: "integer", nullable: false),
                    dayofweek = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("day_pkey", x => x.day_id);
                });

            migrationBuilder.CreateTable(
                name: "departaments",
                columns: table => new
                {
                    dep_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dep_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep_abb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("departaments_pkey", x => x.dep_id);
                });

            migrationBuilder.CreateTable(
                name: "directions",
                columns: table => new
                {
                    dir_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dir_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dir_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    magister = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("directions_pkey", x => x.dir_id);
                });

            migrationBuilder.CreateTable(
                name: "disciplines",
                columns: table => new
                {
                    disciplines_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    group_number = table.Column<int>(type: "integer", nullable: false),
                    discipline_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("disciplines_pkey", x => x.disciplines_id);
                });

            migrationBuilder.CreateTable(
                name: "for_sched_lec",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    napr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    disc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    number_of_streams = table.Column<int>(type: "integer", nullable: true),
                    number_of_groups = table.Column<int>(type: "integer", nullable: true),
                    number_of_subgroups = table.Column<int>(type: "integer", nullable: true),
                    lec_h = table.Column<decimal>(type: "numeric", nullable: true),
                    fio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("for_sched_lec_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "for_sched_prac",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    napr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    disc = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    number_of_subgroups = table.Column<int>(type: "integer", nullable: true),
                    fio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    display_audit = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("for_sched_prac_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    grade_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gr_student_id = table.Column<int>(type: "integer", nullable: true),
                    gr_tegrsu_id = table.Column<int>(type: "integer", nullable: true),
                    gr_date = table.Column<DateOnly>(type: "date", nullable: true),
                    grade = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("grades_pkey", x => x.grade_id);
                });

            migrationBuilder.CreateTable(
                name: "journal",
                columns: table => new
                {
                    j_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    grade = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    student_id = table.Column<int>(type: "integer", nullable: true),
                    teacher_id = table.Column<int>(type: "integer", nullable: true),
                    subject_id = table.Column<int>(type: "integer", nullable: true),
                    group_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "kurs_VKR",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    years = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    form = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    teach_fio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    napr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    st_fio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("kurs_VKR_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "l_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_program_id = table.Column<int>(type: "integer", nullable: true),
                    hours = table.Column<int>(type: "integer", nullable: true),
                    group_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    starttime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    endtime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    people_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("l_groups_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "l_wish_days",
                columns: table => new
                {
                    l_wish_day_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    day_id = table.Column<int>(type: "integer", nullable: true),
                    starttime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    endtime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    listener_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("l_wish_day", x => x.l_wish_day_id);
                });

            migrationBuilder.CreateTable(
                name: "listener_wishes",
                columns: table => new
                {
                    wish_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    people_count = table.Column<int>(type: "integer", nullable: true),
                    hours = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    listener_id = table.Column<int>(type: "integer", nullable: true),
                    wish_description = table.Column<string>(type: "text", nullable: true),
                    suitable_days = table.Column<int[]>(type: "integer[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("listener_wishes_pkey", x => x.wish_id);
                });

            migrationBuilder.CreateTable(
                name: "listeners",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    people_count = table.Column<int>(type: "integer", nullable: true),
                    payer_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    surname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    lastname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    snils = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    passport = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    issued_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    department_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    registration_address = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    group_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("listeners_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pay_graph",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: true),
                    expiration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    deposited_amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true, defaultValueSql: "0.00"),
                    all_sum = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    left_to_pay = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    date_40 = table.Column<DateOnly>(type: "date", nullable: true),
                    bank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pay_graph_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    surname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    lastname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    snils = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    passport = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    issued_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    department_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    registration_address = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payers_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    position_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pos_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("positions_pkey", x => x.position_id);
                });

            migrationBuilder.CreateTable(
                name: "Program_u",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequiredAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ProgramName = table.Column<string>(type: "text", nullable: false),
                    Hours = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Program_u", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    subject_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subject_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sub_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("subjects_pkey", x => x.subject_id);
                });

            migrationBuilder.CreateTable(
                name: "SuperHeroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Place = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperHeroes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "teach_gruz",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    dis_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    code_napr = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    semestr = table.Column<int>(type: "integer", nullable: true),
                    kont_budg = table.Column<int>(type: "integer", nullable: true),
                    kont_dog = table.Column<int>(type: "integer", nullable: true),
                    number_of_streams = table.Column<int>(type: "integer", nullable: true),
                    number_of_groups = table.Column<int>(type: "integer", nullable: true),
                    number_of_subgroups = table.Column<int>(type: "integer", nullable: true),
                    lec_h = table.Column<decimal>(type: "numeric", nullable: true),
                    seminar_h = table.Column<decimal>(type: "numeric", nullable: true),
                    lab_h = table.Column<decimal>(type: "numeric", nullable: true),
                    consult_h = table.Column<decimal>(type: "numeric", nullable: true),
                    exam_h = table.Column<decimal>(type: "numeric", nullable: true),
                    zachet_h = table.Column<decimal>(type: "numeric", nullable: true),
                    kursach_h = table.Column<decimal>(type: "numeric", nullable: true),
                    control_h = table.Column<decimal>(type: "numeric", nullable: true),
                    VKR_h = table.Column<decimal>(type: "numeric", nullable: true),
                    magistr_h = table.Column<decimal>(type: "numeric", nullable: true),
                    GEK_h = table.Column<decimal>(type: "numeric", nullable: true),
                    practice_h = table.Column<decimal>(type: "numeric", nullable: true),
                    manage_h = table.Column<decimal>(type: "numeric", nullable: true),
                    other_h = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("teach_gruz_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "teachers",
                columns: table => new
                {
                    teacher_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    patronymic = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("teachers_pkey", x => x.teacher_id);
                });

            migrationBuilder.CreateTable(
                name: "tegrsus",
                columns: table => new
                {
                    tegrsu_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tegrsu_teacher_id = table.Column<int>(type: "integer", nullable: true),
                    tegrsu_group_id = table.Column<int>(type: "integer", nullable: true),
                    tegrsu_subject_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tegrsus_pkey", x => x.tegrsu_id);
                });

            migrationBuilder.CreateTable(
                name: "temp_departaments",
                columns: table => new
                {
                    dep_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dep_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep_abb = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_departaments_pkey", x => x.dep_id);
                });

            migrationBuilder.CreateTable(
                name: "temp_distrib_KIT",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    kaf = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sem1 = table.Column<decimal>(type: "numeric", nullable: true),
                    sem2 = table.Column<decimal>(type: "numeric", nullable: true),
                    lec = table.Column<decimal>(type: "numeric", nullable: true),
                    sem = table.Column<decimal>(type: "numeric", nullable: true),
                    lab = table.Column<decimal>(type: "numeric", nullable: true),
                    practice = table.Column<decimal>(type: "numeric", nullable: true),
                    VKR = table.Column<decimal>(type: "numeric", nullable: true),
                    GEK = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_distrib_KIT_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_fac_names",
                columns: table => new
                {
                    fac_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    short_name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_fac_names_pkey", x => x.fac_id);
                });

            migrationBuilder.CreateTable(
                name: "temp_itogVO",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    years = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lek_budj = table.Column<decimal>(type: "numeric", nullable: true),
                    lek_dogovor = table.Column<decimal>(type: "numeric", nullable: true),
                    p_budg = table.Column<decimal>(type: "numeric", nullable: true),
                    p_dogovor = table.Column<decimal>(type: "numeric", nullable: true),
                    lab_budj = table.Column<decimal>(type: "numeric", nullable: true),
                    lab_dogovor = table.Column<decimal>(type: "numeric", nullable: true),
                    cons_ex = table.Column<decimal>(type: "numeric", nullable: true),
                    cons_zfo = table.Column<decimal>(type: "numeric", nullable: true),
                    control_work = table.Column<decimal>(type: "numeric", nullable: true),
                    zachet_h = table.Column<decimal>(type: "numeric", nullable: true),
                    ex_speak = table.Column<decimal>(type: "numeric", nullable: true),
                    ex_wr = table.Column<decimal>(type: "numeric", nullable: true),
                    practice = table.Column<decimal>(type: "numeric", nullable: true),
                    manageVKR = table.Column<decimal>(type: "numeric", nullable: true),
                    recVKR = table.Column<decimal>(type: "numeric", nullable: true),
                    normContVKR = table.Column<decimal>(type: "numeric", nullable: true),
                    checkPlagVKR = table.Column<decimal>(type: "numeric", nullable: true),
                    GOSexam = table.Column<decimal>(type: "numeric", nullable: true),
                    defVKR = table.Column<decimal>(type: "numeric", nullable: true),
                    manag = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_itogVO_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_OFO_VO",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dis_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    code_napr = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    kont_budg = table.Column<int>(type: "integer", nullable: true),
                    kont_dog = table.Column<int>(type: "integer", nullable: true),
                    number_of_streams = table.Column<int>(type: "integer", nullable: true),
                    number_of_groups = table.Column<int>(type: "integer", nullable: true),
                    number_of_subgroups = table.Column<int>(type: "integer", nullable: true),
                    lek_budj = table.Column<decimal>(type: "numeric", nullable: true),
                    lek_dogovor = table.Column<decimal>(type: "numeric", nullable: true),
                    p_budg = table.Column<decimal>(type: "numeric", nullable: true),
                    p_dogovor = table.Column<decimal>(type: "numeric", nullable: true),
                    lab_budj = table.Column<decimal>(type: "numeric", nullable: true),
                    lab_dogovor = table.Column<decimal>(type: "numeric", nullable: true),
                    cons_ex = table.Column<decimal>(type: "numeric", nullable: true),
                    zachet_h = table.Column<decimal>(type: "numeric", nullable: true),
                    ex_speak = table.Column<decimal>(type: "numeric", nullable: true),
                    ex_wr = table.Column<decimal>(type: "numeric", nullable: true),
                    practice = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_OFO_VO_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_practice",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    years = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    napr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    profile = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    practice_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    srok = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    semestr = table.Column<int>(type: "integer", nullable: true),
                    number_of_weeks = table.Column<int>(type: "integer", nullable: true),
                    number_of_work_days = table.Column<int>(type: "integer", nullable: true),
                    students_budg = table.Column<int>(type: "integer", nullable: true),
                    students_dog = table.Column<int>(type: "integer", nullable: true),
                    number_of_groups = table.Column<int>(type: "integer", nullable: true),
                    number_of_subgroups = table.Column<int>(type: "integer", nullable: true),
                    budg_h = table.Column<decimal>(type: "numeric", nullable: true),
                    dog_h = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_practice_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_proffesions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fac = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    napr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    profile = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    kval = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    srok = table.Column<int>(type: "integer", nullable: true),
                    proffesions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_proffesions_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_sostav",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name1 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name2 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name3 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fac = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dolj = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deg = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_sostav_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_sostav_test",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name1 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name2 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    name3 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fac = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dolj = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    deg = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_sostav_test_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "temp_task_type",
                columns: table => new
                {
                    fac = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    dep = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    napr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    profile = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    kval = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    srok = table.Column<int>(type: "integer", nullable: true),
                    task_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "temp_teach_gruz",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fam = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fac = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dep = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    dis_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    code_napr = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    kurs = table.Column<int>(type: "integer", nullable: true),
                    semestr = table.Column<int>(type: "integer", nullable: true),
                    kont_budg = table.Column<int>(type: "integer", nullable: true),
                    kont_dog = table.Column<int>(type: "integer", nullable: true),
                    number_of_streams = table.Column<int>(type: "integer", nullable: true),
                    number_of_groups = table.Column<int>(type: "integer", nullable: true),
                    number_of_subgroups = table.Column<int>(type: "integer", nullable: true),
                    lec_h = table.Column<decimal>(type: "numeric", nullable: true),
                    seminar_h = table.Column<decimal>(type: "numeric", nullable: true),
                    lab_h = table.Column<decimal>(type: "numeric", nullable: true),
                    consult_h = table.Column<decimal>(type: "numeric", nullable: true),
                    exam_h = table.Column<decimal>(type: "numeric", nullable: true),
                    zachet_h = table.Column<decimal>(type: "numeric", nullable: true),
                    kursach_h = table.Column<decimal>(type: "numeric", nullable: true),
                    control_h = table.Column<decimal>(type: "numeric", nullable: true),
                    VKR_h = table.Column<decimal>(type: "numeric", nullable: true),
                    magistr_h = table.Column<decimal>(type: "numeric", nullable: true),
                    GEK_h = table.Column<decimal>(type: "numeric", nullable: true),
                    practice_h = table.Column<decimal>(type: "numeric", nullable: true),
                    manage_h = table.Column<decimal>(type: "numeric", nullable: true),
                    other_h = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("temp_teach_gruz_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                columns: table => new
                {
                    prof_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prof_dir_id = table.Column<int>(type: "integer", nullable: true),
                    prof_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("profiles_pkey", x => x.prof_id);
                    table.ForeignKey(
                        name: "pr_dir_id",
                        column: x => x.prof_dir_id,
                        principalTable: "directions",
                        principalColumn: "dir_id");
                });

            migrationBuilder.CreateTable(
                name: "l_groups_day",
                columns: table => new
                {
                    l_groups_days_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    day_id = table.Column<int>(type: "integer", nullable: true),
                    starttime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    endtime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    l_groups = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("l_groups_days", x => x.l_groups_days_id);
                    table.ForeignKey(
                        name: "l_groups",
                        column: x => x.l_groups,
                        principalTable: "l_groups",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    listener_id = table.Column<int>(type: "integer", nullable: true),
                    payer_id = table.Column<int>(type: "integer", nullable: true),
                    program_id = table.Column<int>(type: "integer", nullable: true),
                    cert_date = table.Column<DateOnly>(type: "date", nullable: true),
                    listened_hours = table.Column<int>(type: "integer", nullable: true),
                    date_enroll = table.Column<DateOnly>(type: "date", nullable: true),
                    date_kick = table.Column<DateOnly>(type: "date", nullable: true),
                    group_to_move = table.Column<int>(type: "integer", nullable: true),
                    contr_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contracts_pkey", x => x.id);
                    table.ForeignKey(
                        name: "gr_id",
                        column: x => x.group_to_move,
                        principalTable: "l_groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "lr_id",
                        column: x => x.listener_id,
                        principalTable: "listeners",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tsch",
                columns: table => new
                {
                    t_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    teacher_id = table.Column<int>(type: "integer", nullable: false),
                    day_id = table.Column<int>(type: "integer", nullable: true),
                    time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    dis_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tsch_pkey", x => x.t_id);
                    table.ForeignKey(
                        name: "tsch_day_id_fkey",
                        column: x => x.day_id,
                        principalTable: "days",
                        principalColumn: "day_id");
                    table.ForeignKey(
                        name: "tsch_teacher_id_fkey",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "teacher_id");
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_Roles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_dir_id = table.Column<int>(type: "integer", nullable: true),
                    group_prof_id = table.Column<int>(type: "integer", nullable: true),
                    group_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    course = table.Column<int>(type: "integer", nullable: true, defaultValueSql: "1"),
                    magister = table.Column<bool>(type: "boolean", nullable: true, defaultValueSql: "false")
                },
                constraints: table =>
                {
                    table.PrimaryKey("groups_pkey", x => x.group_id);
                    table.ForeignKey(
                        name: "fk_dir",
                        column: x => x.group_dir_id,
                        principalTable: "directions",
                        principalColumn: "dir_id");
                    table.ForeignKey(
                        name: "fk_main",
                        column: x => x.group_prof_id,
                        principalTable: "profiles",
                        principalColumn: "prof_id");
                });

            migrationBuilder.CreateTable(
                name: "schedule",
                columns: table => new
                {
                    schedule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    aud_id = table.Column<int>(type: "integer", nullable: false),
                    day_id = table.Column<int>(type: "integer", nullable: true),
                    timerange = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    subject_id = table.Column<int>(type: "integer", nullable: false),
                    teacher_id = table.Column<int>(type: "integer", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("schedule_pkey", x => x.schedule_id);
                    table.ForeignKey(
                        name: "schedule_aud_id_fkey",
                        column: x => x.aud_id,
                        principalTable: "auditorium",
                        principalColumn: "aud_id");
                    table.ForeignKey(
                        name: "schedule_day_id_fkey",
                        column: x => x.day_id,
                        principalTable: "days",
                        principalColumn: "day_id");
                    table.ForeignKey(
                        name: "schedule_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "schedule_subject_id_fkey",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "subject_id");
                    table.ForeignKey(
                        name: "schedule_teacher_id_fkey",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "teacher_id");
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    last_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    patronymic = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    passport_series_and_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    INN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SNILS = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    place_of_birth = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    student_login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    enrollment_order = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    enrolled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    course = table.Column<int>(type: "integer", nullable: true),
                    course_work_id = table.Column<int>(type: "integer", nullable: true),
                    group_id = table.Column<int>(type: "integer", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    phone_number_rod = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    zachetka_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    subgroup = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("students_pkey", x => x.student_id);
                    table.ForeignKey(
                        name: "fk_group",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                });

            migrationBuilder.CreateTable(
                name: "workload",
                columns: table => new
                {
                    wl_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('workload_seq'::regclass)"),
                    group_id = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<int>(type: "integer", nullable: false),
                    teacher_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("workload_pkey", x => x.wl_id);
                    table.ForeignKey(
                        name: "workload_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "workload_subject_id_fkey",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "subject_id");
                    table.ForeignKey(
                        name: "workload_teacher_id_fkey",
                        column: x => x.teacher_id,
                        principalTable: "teachers",
                        principalColumn: "teacher_id");
                });

            migrationBuilder.CreateTable(
                name: "course_work",
                columns: table => new
                {
                    course_work_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    course_work_teacher_id = table.Column<int>(type: "integer", nullable: true),
                    course_work_theme = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    course_work_student_id = table.Column<int>(type: "integer", nullable: true),
                    course_work_kafedra = table.Column<int>(type: "integer", nullable: true),
                    course_work_ocenka = table.Column<int>(type: "integer", nullable: true),
                    course_work_year = table.Column<int>(type: "integer", nullable: true),
                    course_work_vipysk = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("course_work_pkey", x => x.course_work_id);
                    table.ForeignKey(
                        name: "fk_kafedra",
                        column: x => x.course_work_kafedra,
                        principalTable: "departaments",
                        principalColumn: "dep_id");
                    table.ForeignKey(
                        name: "fk_std",
                        column: x => x.course_work_student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                    table.ForeignKey(
                        name: "fk_teacher",
                        column: x => x.course_work_teacher_id,
                        principalTable: "teachers",
                        principalColumn: "teacher_id");
                });

            migrationBuilder.CreateTable(
                name: "teachschedule",
                columns: table => new
                {
                    lesson_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    wl_id = table.Column<int>(type: "integer", nullable: false),
                    time = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    day_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("teachschedule_pkey", x => x.lesson_id);
                    table.ForeignKey(
                        name: "teachschedule_day_id_fkey",
                        column: x => x.day_id,
                        principalTable: "days",
                        principalColumn: "day_id");
                    table.ForeignKey(
                        name: "teachschedule_wl_id_fkey",
                        column: x => x.wl_id,
                        principalTable: "workload",
                        principalColumn: "wl_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_contracts_group_to_move",
                table: "contracts",
                column: "group_to_move");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_listener_id",
                table: "contracts",
                column: "listener_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_work_course_work_kafedra",
                table: "course_work",
                column: "course_work_kafedra");

            migrationBuilder.CreateIndex(
                name: "IX_course_work_course_work_student_id",
                table: "course_work",
                column: "course_work_student_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_work_course_work_teacher_id",
                table: "course_work",
                column: "course_work_teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_group_dir_id",
                table: "groups",
                column: "group_dir_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_group_prof_id",
                table: "groups",
                column: "group_prof_id");

            migrationBuilder.CreateIndex(
                name: "IX_l_groups_day_l_groups",
                table: "l_groups_day",
                column: "l_groups");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_prof_dir_id",
                table: "profiles",
                column: "prof_dir_id");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_UserId",
                table: "Roles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_aud_id",
                table: "schedule",
                column: "aud_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_day_id",
                table: "schedule",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_group_id",
                table: "schedule",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_subject_id",
                table: "schedule",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_teacher_id",
                table: "schedule",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_group_id",
                table: "students",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_teachschedule_day_id",
                table: "teachschedule",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "IX_teachschedule_wl_id",
                table: "teachschedule",
                column: "wl_id");

            migrationBuilder.CreateIndex(
                name: "IX_tsch_day_id",
                table: "tsch",
                column: "day_id");

            migrationBuilder.CreateIndex(
                name: "IX_tsch_teacher_id",
                table: "tsch",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_workload_group_id",
                table: "workload",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_workload_subject_id",
                table: "workload",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_workload_teacher_id",
                table: "workload",
                column: "teacher_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendances");

            migrationBuilder.DropTable(
                name: "audit_table_students");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "course_work");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "disciplines");

            migrationBuilder.DropTable(
                name: "for_sched_lec");

            migrationBuilder.DropTable(
                name: "for_sched_prac");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "journal");

            migrationBuilder.DropTable(
                name: "kurs_VKR");

            migrationBuilder.DropTable(
                name: "l_groups_day");

            migrationBuilder.DropTable(
                name: "l_wish_days");

            migrationBuilder.DropTable(
                name: "listener_wishes");

            migrationBuilder.DropTable(
                name: "pay_graph");

            migrationBuilder.DropTable(
                name: "payers");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "Program_u");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "schedule");

            migrationBuilder.DropTable(
                name: "SuperHeroes");

            migrationBuilder.DropTable(
                name: "teach_gruz");

            migrationBuilder.DropTable(
                name: "teachschedule");

            migrationBuilder.DropTable(
                name: "tegrsus");

            migrationBuilder.DropTable(
                name: "temp_departaments");

            migrationBuilder.DropTable(
                name: "temp_distrib_KIT");

            migrationBuilder.DropTable(
                name: "temp_fac_names");

            migrationBuilder.DropTable(
                name: "temp_itogVO");

            migrationBuilder.DropTable(
                name: "temp_OFO_VO");

            migrationBuilder.DropTable(
                name: "temp_practice");

            migrationBuilder.DropTable(
                name: "temp_proffesions");

            migrationBuilder.DropTable(
                name: "temp_sostav");

            migrationBuilder.DropTable(
                name: "temp_sostav_test");

            migrationBuilder.DropTable(
                name: "temp_task_type");

            migrationBuilder.DropTable(
                name: "temp_teach_gruz");

            migrationBuilder.DropTable(
                name: "tsch");

            migrationBuilder.DropTable(
                name: "listeners");

            migrationBuilder.DropTable(
                name: "departaments");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "l_groups");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "auditorium");

            migrationBuilder.DropTable(
                name: "workload");

            migrationBuilder.DropTable(
                name: "days");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "teachers");

            migrationBuilder.DropTable(
                name: "profiles");

            migrationBuilder.DropTable(
                name: "directions");
        }
    }
}
