using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperHeroAPI.Models
{
    [Table("ums_global_permissions")]
    public class GlobalPermission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("role_id")]
        public int RoleId { get; set; }
        
        [Column("create_table_grant")]
        public bool CreateTableGrant { get; set; }
        
        [Column("update_table_grant")]
        public bool UpdateTableGrant { get; set; }
        
        [Column("delete_table_grant")]
        public bool DeleteTableGrant { get; set; }
        
        [Column("create_grant")]
        public bool CreateGrant { get; set; }
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}
