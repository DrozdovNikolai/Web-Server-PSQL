using System.ComponentModel.DataAnnotations;

namespace SuperHeroAPI.Models
{
    public class Permission
    {
       
        public int PermissionId { get; set; }
        public int RoleId { get; set; }
        public string TableName { get; set; }
        public CrudOperation Operation { get; set; }
    }
}
