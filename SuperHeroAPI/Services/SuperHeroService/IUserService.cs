namespace SuperHeroAPI.Services.SuperHeroService
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsers();

        Task<User?> GetSingleUser(int id);

        Task<List<User>> AddUser(UserDto request);
        
        Task<List<User>?> UpdateUser(int id, UserUpd request);
        
        Task<List<User>?> DeleteUser(int id);

        Task<List<UserAuthToken>> GetActiveTokens();
        Task<bool> DeauthorizeUserTokens(int userId);
    }
}
