namespace INFP_Proj.Services
{
    public interface INotificationService
    {
        Task SendAlertAsync(IEnumerable<string> userIds, string subject, string message);
    }

    public interface IUserService
    {
        Task<IEnumerable<string>> GetCareTeamAndFamilyIdsAsync(int patientId);
    }
}
