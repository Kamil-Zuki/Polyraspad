using System;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Smtp;
using System.Net.Mail;
using System.Net;

class Program
{
    static async Task Main(string[] args)
    {
        for (int i = 0; i < 2; i++)
        {
            Console.WriteLine($"\n--- Request {i+1} ---");
            try
            {
                var smtp = new SmtpSender(() => new SmtpClient("smtp.resend.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("resend", "re_iNn2VnAp_LT8vmM1wwom4YS4wHbYasvZY"),
                    EnableSsl = true,
                    UseDefaultCredentials = false
                });

                Email.DefaultSender = smtp; // Move this BEFORE Email.From!

                var email = Email
                    .From("noreply@polyraspad.online", "Test " + i)
                    .To("karatov8@gmail.com")
                    .Subject("Test " + i)
                    .Body("Test body " + i);

                Console.WriteLine("Sending...");
                var result = await email.SendAsync();
                Console.WriteLine("Success: " + result.Successful);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
