namespace Retetar.DataModels
{
    public class SendEmailDto
    {
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public IFormFile? Attachment { get; set; } // Add Attachment property
    }
}
