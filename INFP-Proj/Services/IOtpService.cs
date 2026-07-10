using Microsoft.AspNetCore.Http;

namespace INFP_Proj.Services
{
    public enum OtpVerifyResult
    {
        Success,
        Expired,
        Mismatch
    }

    public interface IOtpService
    {
        Task<bool> GenerateAndSendAsync(ISession session, string purpose, string email, string subject, string bodyTemplate);

        OtpVerifyResult Verify(ISession session, string purpose, string email, string submittedCode);

        string? GetPendingEmail(ISession session, string purpose);

        void ClearCode(ISession session, string purpose);

        void Clear(ISession session, string purpose);
    }
}
