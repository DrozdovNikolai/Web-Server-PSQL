namespace SuperHeroAPI.Models
{
    public class UserReturn
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; }
        public string accessToken { get; set; } = string.Empty;
    }
}
