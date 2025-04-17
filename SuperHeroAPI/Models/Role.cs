using SuperHeroAPI.md4;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperHeroAPI.Models
{
    [Table("ums_roles", Schema = "ums")]
    public class Role
    {
        [Key]
        [Column("role_id")]
        public int RoleId { get; set; }
        
        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;
        

        [JsonIgnore]
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        [JsonIgnore]
        public virtual ICollection<GlobalPermission> GlobalPermissions { get; set; } = new List<GlobalPermission>();
        [JsonIgnore]
        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}
