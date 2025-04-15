using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SuperHeroAPI.Models
{
    [Table("ums_roles")]
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
    }
}
