namespace AuthService.Application.Interfaces
{
    /// <summary>MailKit (mục 38). Gửi mã OTP reset password + các email hệ thống khác.</summary>
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
    }
}
