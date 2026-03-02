using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Jullius.ServiceApi.Application.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        var frontendBaseUrl = _configuration["App:FrontendBaseUrl"]
            ?? throw new InvalidOperationException("App:FrontendBaseUrl não configurado.");
        var encodedToken = Uri.EscapeDataString(resetToken);
        var resetLink = $"{frontendBaseUrl}/auth/reset-password?token={encodedToken}";

        var subject = "Jullius Finanças - Redefinição de Senha";
        var body = BuildResetEmailBody(userName, resetLink);

        await SendEmailAsync(toEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpSettings = _configuration.GetSection("Smtp");
        var host = smtpSettings["Host"]
            ?? throw new InvalidOperationException("Smtp:Host não configurado.");
        var port = int.Parse(smtpSettings["Port"] ?? "587");
        var username = smtpSettings["Username"]
            ?? throw new InvalidOperationException("Smtp:Username não configurado.");
        var password = smtpSettings["Password"]
            ?? throw new InvalidOperationException("Smtp:Password não configurado.");
        var fromEmail = smtpSettings["FromEmail"] ?? username;
        var fromName = smtpSettings["FromName"] ?? "Jullius Finanças";
        var useSsl = bool.Parse(smtpSettings["UseSsl"] ?? "true");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureSocketOptions = useSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(host, port, secureSocketOptions);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);

            _logger.LogInformation("E-mail de redefinição de senha enviado para {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Email}", toEmail);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }

    private static string BuildResetEmailBody(string userName, string resetLink)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <style>
                    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }
                    .container { max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
                    .header { background-color: #1976d2; color: white; padding: 24px; text-align: center; }
                    .header h1 { margin: 0; font-size: 24px; }
                    .content { padding: 32px 24px; color: #333; }
                    .content p { line-height: 1.6; margin-bottom: 16px; }
                    .btn { display: inline-block; background-color: #1976d2; color: white !important; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 16px; }
                    .btn:hover { background-color: #1565c0; }
                    .footer { padding: 16px 24px; text-align: center; font-size: 12px; color: #999; border-top: 1px solid #eee; }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1>Jullius Finanças</h1>
                    </div>
                    <div class="content">
                        <p>Olá, <strong>{{userName}}</strong>!</p>
                        <p>Recebemos uma solicitação para redefinir a senha da sua conta. Clique no botão abaixo para criar uma nova senha:</p>
                        <p style="text-align: center; margin: 32px 0;">
                            <a href="{{resetLink}}" class="btn">Redefinir Senha</a>
                        </p>
                        <p>Este link é válido por <strong>1 hora</strong>. Se você não solicitou a redefinição, ignore este e-mail.</p>
                        <p style="font-size: 13px; color: #666;">Se o botão não funcionar, copie e cole o link abaixo no navegador:<br>
                        <a href="{{resetLink}}" style="color: #1976d2; word-break: break-all;">{{resetLink}}</a></p>
                    </div>
                    <div class="footer">
                        <p>&copy; Jullius Finanças. Todos os direitos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
            """;
    }
}
