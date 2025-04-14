using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperHeroAPI.Models
{
    [Table("ums_permissions")]
    public class Permission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("role_id")]
        public int RoleId { get; set; }
        
        [Column("table_name")]
        public string TableName { get; set; } = string.Empty;
        
        [Column("operation")]
        public int Operation { get; set; }
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}
