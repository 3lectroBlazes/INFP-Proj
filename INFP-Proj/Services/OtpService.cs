using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace INFP_Proj.Services
{
    public class OtpService : IOtpService
    {
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

        private readonly IEmailService emailService;

        public OtpService(IEmailService emailService)
        {
            this.emailService = emailService;
        }

        public async Task<bool> GenerateAndSendAsync(ISession session, string purpose, string email, string subject, string bodyTemplate)
        {
            string otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            string body = string.Format(bodyTemplate, otp);

            bool sent = await emailService.SendEmailAsync(email, subject, body);
            if (!sent)
            {
                return false;
            }

            session.SetString(CodeKey(purpose), otp);
            session.SetString(EmailKey(purpose), email);
            session.SetString(ExpiryKey(purpose), DateTime.UtcNow.Add(CodeLifetime).ToString("O"));
            return true;
        }

        public OtpVerifyResult Verify(ISession session, string purpose, string email, string submittedCode)
        {
            string? pendingOtp = session.GetString(CodeKey(purpose));
            string? pendingEmail = session.GetString(EmailKey(purpose));
            string? expiryRaw = session.GetString(ExpiryKey(purpose));

            if (string.IsNullOrEmpty(pendingOtp) || string.IsNullOrEmpty(pendingEmail) || string.IsNullOrEmpty(expiryRaw))
            {
                return OtpVerifyResult.Expired;
            }

            if (!string.Equals(pendingEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                return OtpVerifyResult.Mismatch;
            }

            if (DateTime.UtcNow > DateTime.Parse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
            {
                return OtpVerifyResult.Expired;
            }

            if (string.IsNullOrWhiteSpace(submittedCode) || !string.Equals(submittedCode.Trim(), pendingOtp, StringComparison.Ordinal))
            {
                return OtpVerifyResult.Mismatch;
            }

            return OtpVerifyResult.Success;
        }

        public string? GetPendingEmail(ISession session, string purpose) => session.GetString(EmailKey(purpose));

        public void ClearCode(ISession session, string purpose)
        {
            session.Remove(CodeKey(purpose));
            session.Remove(ExpiryKey(purpose));
        }

        public void Clear(ISession session, string purpose)
        {
            session.Remove(CodeKey(purpose));
            session.Remove(EmailKey(purpose));
            session.Remove(ExpiryKey(purpose));
        }

        private static string CodeKey(string purpose) => $"{purpose}_Otp";
        private static string EmailKey(string purpose) => $"{purpose}_Email";
        private static string ExpiryKey(string purpose) => $"{purpose}_Expiry";
    }
}
