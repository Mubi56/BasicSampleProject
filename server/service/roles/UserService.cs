using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Paradigm.Contract.Interface;
using Paradigm.Data;
using Paradigm.Data.Model;
using Paradigm.Server.Interface;

namespace Paradigm.Server.Application
{
    public class UserService : IUserService
    {
        private DbContextBase _dbContext;
        private IResponse _response;
        private ICryptoService _crypto;
        private ICountResponse _countResp;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        public UserService(DbContextBase dbContext, IResponse response, ICryptoService crypto, ICountResponse countResp, IHttpContextAccessor httpContextAccessor, IEmailService emailService)
        {
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _dbContext = dbContext;
            _response = response;
            _crypto = crypto;
            _countResp = countResp;
        }
        
        public async Task<IResponse> GoogleSignUp(GoogleUserSignup signUp)
        {
            //Add User
            string randomString = HelperStatic.RandomString(8);
            var salt = _crypto.CreateSalt();
            var hash = _crypto.CreateKey(salt, randomString);
            var user = new User(signUp, salt, hash);
            await _dbContext.User.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            //Add User Detail
            UserDetail userDetail = new(signUp, HelperStatic.GetCurrentTimeStamp(), user.UserId);
            await _dbContext.UserDetail.AddAsync(userDetail);
            await _dbContext.SaveChangesAsync();

            //Add User Role
            UserRole r1 = new()
            {
                UserId = user.UserId,
                RoleId = Constants.Admin
            };
            _dbContext.UserRole.Add(r1);
            await _dbContext.SaveChangesAsync();

            _response.Data = user.UserId;
            _response.Message = Constants.DataSaved;
            _response.Success = Constants.ResponseSuccess;
            return _response;
        }
        public async Task<IResponse> SignUp(UserSignup signUp)
        {
            //Add User
            var salt = _crypto.CreateSalt();
            var hash = _crypto.CreateKey(salt, signUp.Password);
            var user = new User(signUp, salt, hash);
            _dbContext.User.Add(user);
            await _dbContext.SaveChangesAsync();

            //Add User Detail
            UserDetail userDetail = new(signUp, HelperStatic.GetCurrentTimeStamp(), user.UserId);
            _dbContext.UserDetail.Add(userDetail);
            await _dbContext.SaveChangesAsync();

            //Add User Role
            UserRole r1 = new()
            {
                UserId = user.UserId,
                RoleId = Constants.Admin
            };
            _dbContext.UserRole.Add(r1);
            await _dbContext.SaveChangesAsync();

            
            // Create the reset link
            var request = _httpContextAccessor.HttpContext.Request;
            var baseUrl = $"{request.Host}";

            //send welcom email
            var subject = "Welcome";
            var body = $@"
                <html>
                <body>
                    <p>Hi {signUp.FirstName + " " + signUp.LastName},</p>
                    <p>Your account has been created successfully.</p>
                    <p>Your login details are as follows:</p>
                    <ul>
                        <li><strong>Username:</strong> {signUp.Username}</li>
                        <li><strong>Password:</strong> {signUp.Password}</li>
                    </ul>
                    <p>Please follow this URL to access the system:</p>
                    <p><a href='{baseUrl}'>{baseUrl}</a></p>
                    <p>Once logged in, kindly update your password for security purposes.</p>
                    <p>Thanks,<br>The Voyager Team</p>
                </body>
                </html>
                ";
            var response = await _emailService.SendEmailUsingSendGrid(signUp.Username, subject, body);

            _response.Message = Constants.DataSaved;
            _response.Success = Constants.ResponseSuccess;
            return _response;
        }
        public async Task<IResponse> GetAllByProc(User_Listing model, int diff)
        {
            if (string.IsNullOrEmpty(model.Sort))
            {
                model.Sort = "DisplayName";
                model.Order = "desc";
            }
            var data = (
                            from main in _dbContext.User
                            join det in _dbContext.UserDetail on main.UserId equals det.UserId
                            where (main.Username.Contains(model.Username) || String.IsNullOrEmpty(model.Username))
                            && (main.DisplayName.Contains(model.DisplayName) || String.IsNullOrEmpty(model.DisplayName))
                            && (main.MobileNumber.Contains(model.MobileNumber) || String.IsNullOrEmpty(model.MobileNumber))
                            select new VW_User
                            {
                                UserId = main.UserId,
                                Username = main.Username,
                                MobileNumber = main.MobileNumber,
                                Enabled = main.Enabled,
                                CreatedOn = det.CreatedOn
                            }
                        ).AsQueryable();

            //Sort and Return
            var count = data.Count();
            var sorted = await HelperStatic.OrderBy(data, model.SortEx, model.OrderEx == "desc").Skip(model.Start).Take(model.LimitEx).ToListAsync();
            foreach (var item in sorted)
            {
                item.TotalCount = count;
                item.SerialNo = ++model.Start;
            }
            _countResp.DataList = sorted;
            _countResp.TotalCount = sorted.Count > 0 ? sorted.First().TotalCount : 0;
            _response.Success = Constants.ResponseSuccess;
            _response.Data = _countResp;
            return _response;
        }
        public async Task<IResponse> AddEdit(AddEditUser addEdit, AuditTrack audit)
        {
            if (addEdit.Role.Count == 0)
            {
                _response.Message = "Role Required";
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            string auditOldValue = "N/A";
            string auditNewValue = "N/A";
            if (addEdit.UserId == null)
            {
                //Add User
                var salt = _crypto.CreateSalt();
                var hash = _crypto.CreateKey(salt, addEdit.Password);
                var user = new User(addEdit, salt, hash);
                _dbContext.User.Add(user);
                await _dbContext.SaveChangesAsync();

                //Add User Detail
                UserDetail userDetail = new UserDetail(audit, addEdit);
                _dbContext.UserDetail.Add(userDetail);
                await _dbContext.SaveChangesAsync();

                //Add User Role
                List<UserRole> role = new List<UserRole>() { };
                foreach (var item in addEdit.Role)
                {
                    UserRole r1 = new UserRole();
                    r1.UserId = user.UserId;
                    r1.RoleId = item;
                    role.Add(r1);
                }

            }
            else
            {
                User user = await _dbContext.User.FirstOrDefaultAsync(x => x.UserId == addEdit.UserId);
                if (user == null)
                {
                    _response.Message = Constants.NotFound.Replace("{data}", "User");
                    _response.Success = Constants.ResponseFailure;
                    return _response;
                }
                UserDetail userdetail = await _dbContext.UserDetail.FirstOrDefaultAsync(x => x.UserId == addEdit.UserId);
                if (userdetail == null)
                {
                    _response.Message = Constants.NotFound.Replace("{data}", "User");
                    _response.Success = Constants.ResponseFailure;
                    return _response;
                }
                var role = await _dbContext.UserRole.Where(x => x.UserId == addEdit.UserId).ToListAsync();
                _dbContext.UserRole.RemoveRange(role);
                await _dbContext.SaveChangesAsync();

                auditOldValue = JsonSerializer.Serialize(user, audit.Options) + "*" + JsonSerializer.Serialize(userdetail, audit.Options);
                user.DisplayName = addEdit.DisplayName;
                user.MobileNumber = addEdit.MobileNumber;
                _dbContext.User.Update(user);
                await _dbContext.SaveChangesAsync();

                userdetail.ImagePath = addEdit.ImagePath;
                userdetail.UpdatedBy = audit.UserId;
                userdetail.UpdatedOn = audit.Time;
                _dbContext.UserDetail.Update(userdetail);
                await _dbContext.SaveChangesAsync();

                List<UserRole> roleAdd = new List<UserRole>() { };
                foreach (var item in addEdit.Role)
                {
                    UserRole role1 = new UserRole();
                    role1.UserId = user.UserId;
                    role1.RoleId = item;
                    roleAdd.Add(role1);
                }
                _dbContext.UserRole.AddRange(roleAdd);
                await _dbContext.SaveChangesAsync();

                auditNewValue = JsonSerializer.Serialize(user, audit.Options) + "*" + JsonSerializer.Serialize(userdetail, audit.Options);
            }

            var _auditLogs = new AuditHistory(audit.UserId, audit.Username, auditOldValue, auditNewValue, audit.Time, audit.SessionId, audit.ActionScreen, addEdit.UserId == null ? Constants.ActionMethods.Add : Constants.ActionMethods.Update);
            _dbContext.AuditHistory.Add(_auditLogs);
            await _dbContext.SaveChangesAsync();

            _response.Message = Constants.DataSaved;
            _response.Success = Constants.ResponseSuccess;
            return _response;
        }
        public async Task<IResponse> GetSingle(Guid id)
        {
            User User = await _dbContext.User.FirstOrDefaultAsync(x => x.UserId == id);
            if (User == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "User");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            UserDetail userdetail = await _dbContext.UserDetail.FirstOrDefaultAsync(x => x.UserId == User.UserId);
            if (userdetail == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "User");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            var role = await _dbContext.UserRole.Where(x => x.UserId == User.UserId).ToListAsync();
            AddEditUser addEdit = new AddEditUser();
            addEdit.UserId = User.UserId;
            addEdit.Username = User.Username;
            addEdit.DisplayName = User.DisplayName;
            addEdit.MobileNumber = User.MobileNumber;
            addEdit.Role = role.Select(x => x.RoleId).ToList();
            _response.Data = addEdit;
            _response.Success = Constants.ResponseSuccess;
            return _response;
        }
        public async Task<IResponse> ActiveInactive(ActiveInactiveBool model, AuditTrack audit)
        {
            string auditOldValue = "N/A";
            string auditNewValue = "N/A";
            User User = await _dbContext.User.FirstOrDefaultAsync(x => x.UserId == model.Id);
            if (User == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "User");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            auditOldValue = JsonSerializer.Serialize(User, audit.Options);
            User.Enabled = model.Status;
            _dbContext.User.Update(User);
            await _dbContext.SaveChangesAsync();
            auditNewValue = JsonSerializer.Serialize(User, audit.Options);

            var _auditLogs = new AuditHistory(audit.UserId, audit.Username, auditOldValue, auditNewValue, audit.Time, audit.SessionId, audit.ActionScreen, model.Status == true ? Constants.ActionMethods.Active : Constants.ActionMethods.Deactive);
            _dbContext.AuditHistory.Add(_auditLogs);
            await _dbContext.SaveChangesAsync();

            _response.Success = Constants.ResponseSuccess;
            return _response;
        }
    }
}