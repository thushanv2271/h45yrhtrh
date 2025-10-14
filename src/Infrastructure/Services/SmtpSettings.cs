namespace Infrastructure.Services;

public class SmtpSettings
{
	public string Host { get; set; } = string.Empty;
	public int Port { get; set; }
	public bool EnableSsl { get; set; }
	public string From { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}
