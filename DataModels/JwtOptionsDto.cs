namespace Retetar.DataModels
{
    public class JwtOptionsDto
    {
        public string ValidAudience { get; set; }
        public string ValidIssuer { get; set; }
        public string TokenExpiryTimeInHour { get; set; }
        public string Secret { get; set; }
    }
}
