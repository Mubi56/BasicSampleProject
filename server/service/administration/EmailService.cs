using System;
using System.Threading.Tasks;
using Paradigm.Data;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using Paradigm.Server.Interface;
using Microsoft.EntityFrameworkCore;
using Paradigm.Data.Model;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using Paradigm.Contract.Interface;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Paradigm.Server.Application
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private DbContextBase _dbContext;
        private IResponse _response;
        private ICountResponse _countResp;
        private readonly string _apiKey;
        private ICryptoService _crypto;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public EmailService(DbContextBase dbContext, IResponse response, ICountResponse countResp, IConfiguration config, ICryptoService crypto, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _response = response;
            _countResp = countResp;
            _config = config;
            _crypto = crypto;
            _apiKey = _config["SendGrid:ApiKey"];
        }
        public async Task<IResponse> SendEmailUsingSendGrid(string toEmail, string subject, string body)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress("mubashariqbalkhan1@gmail.com"); // Sender email and name
                var to = new EmailAddress(toEmail);
                var plainTextContent = body;
                var htmlContent = $"<strong>{body}</strong>"; // HTML content

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);

                // You can check the response status code if needed
                if (!response.IsSuccessStatusCode)
                {
                    // Handle failed email sending here
                    _response.Success = Constants.ResponseFailure;
                    _response.Message = Constants.UnableToSendEmail;
                    return _response;
                }
                _response.Success = Constants.ResponseSuccess;
                _response.Message = Constants.Emailsent;
                return _response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.UnableToSendEmail;
                return _response;
            }
        }
        public async Task<IResponse> SendEmailUsingSmtp(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("mubashariqbalkhan1@gmail.com", "bmny pwto uhkp qqay"),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("mubashariqbalkhan1@gmail.com", "voyager"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);

            _response.Success = Constants.ResponseSuccess;
            _response.Message = "Email sent successfully";
            return _response;
        }
        public async Task<IResponse> ForgotPassword(ForgotPassword forgotPassword, int type)
        {
            try
            {
                var alluser = await _dbContext.User.Select(x => new { x.Username, x.UserId }).AsNoTracking().ToListAsync();
                var allusers = alluser.Select(x => new { email = x.Username, x.UserId }).ToList();
                var userId = allusers.FirstOrDefault(x => x.email == forgotPassword.Email)?.UserId;
                if (userId == null)
                {
                    _response.Success = Constants.ResponseFailure;
                    _response.Message = Constants.InvalidEmail;
                    return _response;
                }
                var user = await _dbContext.User.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
                if (user == null)
                {
                    _response.Success = Constants.ResponseFailure;
                    _response.Message = Constants.InvalidEmail;
                    return _response;
                }

                // Generate a token
                var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

                // Save token to the database
                var expiration = HelperStatic.GetCurrentTimeStamp() + 3600; // Token valid for 1 hour
                var resetToken = new PasswordResetToken
                {
                    TokenId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Token = token,
                    Expiry = expiration,
                    CreatedOn = HelperStatic.GetCurrentTimeStamp(),
                    Status = 1
                };
                _dbContext.PasswordResetToken.Add(resetToken);
                await _dbContext.SaveChangesAsync();

                // Create the reset link
                var request = _httpContextAccessor.HttpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";

                var clientUri = baseUrl; // Frontend base URL
                var resetLink = $"{clientUri}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(forgotPassword.Email)}";

                var subject = "Forgot Password";
                var body = $"Click the link to reset your password: <a href='{resetLink}'>Reset Password</a>";
                if (type == 1)
                {
                    _response.Message = body;
                    _response.Success = Constants.ResponseSuccess;
                    return _response;
                }
                var response = await SendEmailUsingSendGrid(forgotPassword.Email, subject, body);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.UnableToSendEmail;
                return _response;
            }
        }
        public async Task<IResponse> ResetPassword(ResetPassword resetPassword)
        {
            try
            {
                var token = Uri.UnescapeDataString(resetPassword.Token);
                var resetToken = await _dbContext.PasswordResetToken.FirstOrDefaultAsync(x => x.Token == token);
                if (resetToken == null)
                {
                    _response.Success = Constants.ResponseFailure;
                    _response.Message = Constants.InvalidAccessToken;
                    return _response;
                }

                if (resetToken.Expiry < HelperStatic.GetCurrentTimeStamp() || resetToken.Status == 0)
                {
                    _response.Success = Constants.ResponseFailure;
                    _response.Message = Constants.ExpiredNonce;
                    return _response;
                }

                var user = await _dbContext.User.FirstOrDefaultAsync(x => x.UserId == resetToken.UserId);
                if (user == null)
                {
                    _response.Success = Constants.ResponseFailure;
                    _response.Message = Constants.InvalidEmail;
                    return _response;
                }

                var salt = _crypto.CreateSalt();
                var hash = _crypto.CreateKey(salt, resetPassword.Password);

                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                _dbContext.User.Update(user);
                await _dbContext.SaveChangesAsync();

                resetToken.Status = 0;
                resetToken.UsedOn = HelperStatic.GetCurrentTimeStamp();
                _dbContext.PasswordResetToken.Update(resetToken);
                await _dbContext.SaveChangesAsync();

                _response.Success = Constants.ResponseSuccess;
                _response.Message = Constants.DataSaved;
                return _response;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.UnableToSendEmail;
                return _response;
            }
        }
        public async Task SendTestEmail()
        {
            var subject = "Test Email";
            var body = "This is a test email sent from the EmailService.";
            var response = await SendEmailUsingSendGrid("mubashariqbalkhan1@gmail.com", subject, body);
            Console.WriteLine($"Test Email Send Status: {response.Message}");
        }
    }
}