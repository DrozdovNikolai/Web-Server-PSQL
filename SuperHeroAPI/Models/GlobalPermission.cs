using System.ComponentModel.DataAnnotations;

namespace SuperHeroAPI.Models
{
    public class GlobalPermission
    {
       
        public int PermissionId { get; set; }
        public int RoleId { get; set; }
        public string ActionType { get; set; }
       


    }
}
