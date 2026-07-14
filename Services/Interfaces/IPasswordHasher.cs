namespace OSBIS.Services.Interfaces
{
    /// <summary>
    /// Password Hasher - BCrypt theo OWASP khuyến nghị
    /// </summary>
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}