using Microsoft.Extensions.Logging;

namespace INFP_Proj.Services
{
    public class MockSmsService : ISmsService
    {
        private readonly ILogger<MockSmsService> _logger;

        public MockSmsService(ILogger<MockSmsService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation("[SMS SENT to {PhoneNumber}]: {Message}", phoneNumber, message);

            return Task.FromResult(true);
        }
    }
}