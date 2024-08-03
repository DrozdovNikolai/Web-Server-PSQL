namespace SuperHeroAPI.Models
{
    public class UserUpd
    {

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<int> RoleIds { get; set; }
    }
}

