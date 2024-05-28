namespace Retetar.DataModels
{
    public class ResetPasswordDto
    {
        public string Email { get; set; }
        public string? NewPassword { get; set; }
    }
}
