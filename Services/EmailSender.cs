using Microsoft.AspNetCore.Identity.UI.Services;

namespace SIRH.EY.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Pour le moment : fake (log console)
            Console.WriteLine("=== EMAIL ===");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");

            return Task.CompletedTask;
        }
    }
}