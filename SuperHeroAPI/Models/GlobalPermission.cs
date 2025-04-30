using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperHeroAPI.Models
{
    [Table("ums_global_permissions", Schema = "ums")]
    public class GlobalPermission
    {
        [Key]
        [Column("permission_id")]
        public int PermissionId { get; set; }
        [Column("schema_name")]
        public string SchemaName { get; set; } = null!;
        [Column("role_id")]
        public int RoleId { get; set; }
        
        [Column("grant_create_obj")]
        public bool CreateTableGrant { get; set; }
        
        [Column("grant_update_tbl")]
        public bool UpdateTableGrant { get; set; }
        
        [Column("grant_delete_tbl")]
        public bool DeleteTableGrant { get; set; }
        
        [Column("grant_create_db")]
        public bool CreateGrant { get; set; }
        
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}
