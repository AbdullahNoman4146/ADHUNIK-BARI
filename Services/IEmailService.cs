namespace ADHUNIK_BARI.Services
{
    public interface IEmailService
    {
        Task<bool> SendAsync(string toEmail, string subject, string htmlBody);
    }
}
