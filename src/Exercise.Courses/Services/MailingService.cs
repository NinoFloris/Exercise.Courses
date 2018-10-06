using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Exercise.Courses.Services
{
    public class MailingService
    {
        private readonly ILogger<MailingService> _logger;

        public MailingService(ILogger<MailingService> logger)
            => _logger = logger;

        public Task SendConfirmationEmail()
        {
            _logger.LogInformation("Confirmation Email sent");
            return Task.CompletedTask;
        }
        public Task SendDenyEmail()
        {
            _logger.LogInformation("Deny Email sent");
            return Task.CompletedTask;
        }
    }
}