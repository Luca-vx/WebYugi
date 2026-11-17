using System.Net;
using System.Net.Mail;

namespace WebYugi.Services;

public interface IPasswordResetEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string email, string name, string resetUrl, CancellationToken cancellationToken);
}

public sealed class PasswordResetEmailSender(IConfiguration configuration) : IPasswordResetEmailSender
{
    private readonly IConfiguration _configuration = configuration;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"])
        && !string.IsNullOrWhiteSpace(_configuration["Email:From"]);

    public async Task SendAsync(string email, string name, string resetUrl, CancellationToken cancellationToken)
    {
        var host = _configuration["Email:SmtpHost"]!;
        var port = int.TryParse(_configuration["Email:SmtpPort"], out var parsedPort) ? parsedPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 10000
        };
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, password);
        using var message = new MailMessage(_configuration["Email:From"]!, email)
        {
            Subject = "Redefinição de senha — WebYugi",
            Body = $"Olá, {name}.\n\nUse o link a seguir para redefinir sua senha. Ele expira em 20 minutos e pode ser usado uma única vez:\n{resetUrl}\n\nSe você não solicitou esta alteração, ignore esta mensagem.",
            IsBodyHtml = false
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
