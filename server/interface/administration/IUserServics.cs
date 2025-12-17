using System;
using System.Threading.Tasks;
using Paradigm.Data.Model;
using Paradigm.Server.Application;

namespace Paradigm.Server.Interface
{
    public interface IUserService
    {
        Task<IResponse> SignUp(UserSignup signUp);
        Task<IResponse> AddEdit(AddEditUser addEdit, AuditTrack audit);
        Task<IResponse> GetSingle(Guid id);
        Task<IResponse> GetAllByProc(User_Listing model, int diff);
        Task<IResponse> ActiveInactive(ActiveInactiveBool model, AuditTrack audit);
        Task<IResponse> GoogleSignUp(GoogleUserSignup signUp);
    }
}
