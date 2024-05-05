namespace Retetar.DataModels
{
    public interface IEmailSender
    {
       Task<string> SendEmailAsync(string email, string subject, string message, IFormFile? attachment);
    }
}
