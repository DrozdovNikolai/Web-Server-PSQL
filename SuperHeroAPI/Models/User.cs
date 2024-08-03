namespace SuperHeroAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}
