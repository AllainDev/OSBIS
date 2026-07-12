namespace ORBIS.Services
{
    public record ServiceResult(
        bool Succeeded,
        string Message)
    {
        public static ServiceResult Success(string message)
        {
            return new ServiceResult(true, message);
        }

        public static ServiceResult Failure(string message)
        {
            return new ServiceResult(false, message);
        }
    }
}
