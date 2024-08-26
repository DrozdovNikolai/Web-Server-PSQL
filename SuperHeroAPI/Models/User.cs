using SuperHeroAPI.md2;

namespace SuperHeroAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        
        public virtual ICollection<UserRole> UserRoles { get; set; }

        public virtual ICollection<ProcedureUser> ProcedureUsers { get; } = new List<ProcedureUser>();

        public virtual ICollection<TableUser> TableUsers { get; } = new List<TableUser>();


        public virtual ICollection<TriggerUser> TriggerUsers { get; } = new List<TriggerUser>();
    }
}
