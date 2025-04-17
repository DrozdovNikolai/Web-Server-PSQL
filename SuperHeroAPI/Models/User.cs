using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SuperHeroAPI.md2;
using SuperHeroAPI.md4;

namespace SuperHeroAPI.Models
{
    [Table("ums_users", Schema = "ums")]
    public class User
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("username")]
        public string Username { get; set; } = string.Empty;
        
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        public virtual ICollection<TableUser> TableUsers { get; set; } = new List<TableUser>();
        public virtual ICollection<TriggerUser> TriggerUsers { get; set; } = new List<TriggerUser>();
        public virtual ICollection<ProcedureUser> ProcedureUsers { get; set; } = new List<ProcedureUser>();
        public virtual ICollection<FunctionUser> FunctionUsers { get; set; } = new List<FunctionUser>();
        public virtual ICollection<RequestLog> RequestUsers { get; set; } = new List<RequestLog>();
        public virtual UserAuthToken? UserAuthToken { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
