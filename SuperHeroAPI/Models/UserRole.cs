using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperHeroAPI.Models
{
    [Table("ums_user_role")]
    public class UserRole
    {
        [Key]
        [Column("user_role_id")]
        public int UserRoleId { get; set; }
        
        [Column("user_id")]
        public int UserId { get; set; }
        
        [Column("role_id")]
        public int RoleId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}
