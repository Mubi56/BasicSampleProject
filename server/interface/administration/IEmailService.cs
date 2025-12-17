using System;
using System.Threading.Tasks;
using Paradigm.Data.Model;
using Paradigm.Server.Application;

namespace Paradigm.Server.Interface
{
    public interface IEmailService
    {
        Task<IResponse> SendEmailUsingSendGrid(string toEmail, string subject, string body);
        Task<IResponse> ForgotPassword(ForgotPassword forgotPassword, int type);
        Task<IResponse> ResetPassword(ResetPassword resetPassword);
    }
}
