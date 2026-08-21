using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Sending test email using SmtpClient...");
            var client = new SmtpClient("smtp.resend.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("resend", "re_iNn2VnAp_LT8vmM1wwom4YS4wHbYasvZY"),
                EnableSsl = true,
                UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("noreply@polyraspad.online", "Polyraspad Test"),
                Subject = "Test from C# SmtpClient",
                Body = "This is a test from SmtpClient.",
                IsBodyHtml = false
            };
            mailMessage.To.Add("karatov8@gmail.com");

            await client.SendMailAsync(mailMessage);
            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to send email:");
            Console.WriteLine(ex.ToString());
        }
    }
}
