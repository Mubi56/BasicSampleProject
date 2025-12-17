namespace Paradigm.Server.Authentication
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Microsoft.AspNetCore.Antiforgery;

    using Paradigm.Server;
    using Paradigm.Service;
    using Paradigm.Server.Model;
    using Paradigm.Service.Security;
    using Paradigm.Service.Repository;
    using Paradigm.Contract.Interface;
    using Paradigm.Server.Application;
    using Paradigm.Data;
    using Microsoft.EntityFrameworkCore;
    using Paradigm.Server.Interface;
    using Paradigm.Data.Model;

    [Route("api/auth/[action]")]
    public class AuthController : Server.ControllerBase
    {
        private readonly IAntiforgery antiForgeryService;
        private readonly IUserRepository repository;
        private readonly CultureService cultureService;
        private readonly ITokenProviderService<Token> tokenService;
        private readonly AppConfig config;
        private readonly IResponse _response;
        private readonly DbContextBase _dbContext;
        private readonly IAuditService _audit;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public AuthController(IEmailService emailService, IAuditService audit, DbContextBase dbContext, IResponse response, IAntiforgery antiForgeryService, IUserRepository repository, ITokenProviderService<Token> tokenService, CultureService cultureService, IOptions<AppConfig> config, IDomainContextResolver resolver, ILocalizationService localization, IUserService userService) : base(resolver, localization)
        {
            this.antiForgeryService = antiForgeryService;
            this.repository = repository;
            this.tokenService = tokenService;
            this.cultureService = cultureService;
            this.config = config.Value;
            this._response = response;
            this._dbContext = dbContext;
            this._audit = audit;
            this._userService = userService;
            this._emailService = emailService;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<object> GoogleSignup([FromBody] GoogleUserSignup credentials)
        {
            if (!ModelState.IsValid)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.ModelStateStateIsInvalid;
                return Ok(_response);
            }
            Guid currentGuid = Guid.NewGuid();
            if (credentials.IsFirstTime)
            {
                var google = await _userService.GoogleSignUp(credentials);
                if (!google.Success)
                {
                    return Ok(google);
                }
                currentGuid = (Guid)google.Data;
            }

            //check if user exists
            var user = await repository.ResolveUser(credentials.Email);
            if (user == null)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.InvalidUsernameOrPassword;
                return Ok(_response);
            }
            currentGuid = Guid.Parse(user.UserId);

            //login process
            var identity = await repository.ResolveGoogleUser(credentials.Email, null, false);
            if (identity == null)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.IncorrectUsernamePassword;
                return Ok(_response);
            }

            Guid userId = currentGuid;
            UserDetail userdetail = await _dbContext.UserDetail.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            if (userdetail == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "UserDetail");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            UserRole userRole = await _dbContext.UserRole.FirstOrDefaultAsync(x => x.UserId == userId);
            if (userRole == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "UserRole");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            string role = userRole.RoleId;
            string name = user?.DisplayName ?? "";
            string userid = userId.ToString();

            this.SetAntiforgeryCookies();

            string cultureClaimKey = this.config.Service.ClaimsNamespace + ProfileClaimTypes.CultureName;
            string timeZoneIdKey = this.config.Service.ClaimsNamespace + ProfileClaimTypes.TimeZoneId;

            string cultureName = identity.Claims.FirstOrDefault(o => o.Type == cultureClaimKey).Value;
            string timeZoneId = identity.Claims.FirstOrDefault(o => o.Type == timeZoneIdKey).Value;

            //AuditTrails trails = new AuditTrails() { Location = "KHAZANA", Country = "PK", City = "LHR", DeviceType = "LAPTOP", IPAddress = "122.233.234.23", Time = HelperStatic.GetCurrentTimeStamp(), OperatingSystem = "WINDOWS", UserId = null, UserName = "admin@paradigm.org", AuditType = null, AuditSessionID = null };

            // credentials.AuditTrails.AuditType = Constants.ActionMethods.LoginSuccess;
            // credentials.AuditTrails.UserId = userId;
            // var res = await this._audit.AddOne(credentials.AuditTrails);
            // string resStr = res.ToString();
            // string sessionId = resStr.Split("*")[1];

            this.cultureService.RefreshCookie(this.HttpContext, cultureName, timeZoneId);

            var token = await this.tokenService.IssueToken(identity, string.Concat(credentials?.FirstName ?? "", " ", credentials?.LastName ?? ""), role, name, userid);

            _response.Data = token;
            _response.Success = Constants.ResponseSuccess;
            return Ok(_response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<object> SignUp([FromBody] UserSignup userSignup)
        {
            if (!ModelState.IsValid)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.ModelStateStateIsInvalid;
                return Ok(_response);
            }
            var user = await repository.ResolveUser(userSignup.Username);
            if (user != null)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.EmailAddressInUse;
                return Ok(_response);
            }
            var response = await _userService.SignUp(userSignup);
            return Ok(response);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<object> ForgotPassword([FromBody] ForgotPassword forgotPassword)
        {
            if (!ModelState.IsValid)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.ModelStateStateIsInvalid;
                return Ok(_response);
            }
            var user = await repository.ResolveUser(forgotPassword.Email);
            if (user == null)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.InvalidUsernameOrPassword;
                return Ok(_response);
            }
            var response = await _emailService.ForgotPassword(forgotPassword, 0);
            return Ok(response);
        }
        [HttpPost]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<object> ResetPassword([FromBody] ResetPassword resetPassword)
        {
            if (!ModelState.IsValid)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.ModelStateStateIsInvalid;
                return Ok(_response);
            }
            var response = await _emailService.ResetPassword(resetPassword);
            return Ok(response);
        }

        [HttpGet]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<object> SampleLogin()
        {
            //Sample Values
            //AuditTrails trails = new AuditTrails() { Location = "KHAZANA", Country = "PK", City = "LHR", DeviceType = "LAPTOP", IPAddress = "122.233.234.23", Time = HelperStatic.GetCurrentTimeStamp(), OperatingSystem = "WINDOWS", UserId = null, UserName = "admin@paradigm.org", AuditType = null, AuditSessionID = null };
            TokenRequest credentials = new TokenRequest() { Username = "admin@paradigm.org", Password = "P@ssw0rd", IsSuperAdmin = false };
            return Ok(await this.Login(credentials));
        }

        [HttpPost()]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<object> Login([FromBody] TokenRequest credentials)
        {
            if (!ModelState.IsValid)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.ModelStateStateIsInvalid;
                return Ok(_response);
            }
            var identity = await repository.ResolveUser(credentials.Username, credentials.Password, credentials.IsSuperAdmin);
            var user = await repository.ResolveUser(credentials.Username);

            if (identity == null || user == null)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.IncorrectUsernamePassword;
                return Ok(_response);
            }
            if (!user.Enabled)
            {
                _response.Success = Constants.ResponseFailure;
                _response.Message = Constants.IncorrectUsernamePassword;
                return Ok(_response);
            }
            Guid userId = Guid.Parse(user.UserId);
            UserDetail userdetail = await _dbContext.UserDetail.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            if (userdetail == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "UserDetail");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            UserRole userRole = await _dbContext.UserRole.FirstOrDefaultAsync(x => x.UserId == userId);
            if (userRole == null)
            {
                _response.Message = Constants.NotFound.Replace("{data}", "UserRole");
                _response.Success = Constants.ResponseFailure;
                return _response;
            }
            string role = userRole.RoleId;
            string name = user.DisplayName;
            string userid = user.UserId;

            this.SetAntiforgeryCookies();

            string cultureClaimKey = this.config.Service.ClaimsNamespace + ProfileClaimTypes.CultureName;
            string timeZoneIdKey = this.config.Service.ClaimsNamespace + ProfileClaimTypes.TimeZoneId;

            string cultureName = identity.Claims.FirstOrDefault(o => o.Type == cultureClaimKey).Value;
            string timeZoneId = identity.Claims.FirstOrDefault(o => o.Type == timeZoneIdKey).Value;

            //AuditTrails trails = new AuditTrails() { Location = "KHAZANA", Country = "PK", City = "LHR", DeviceType = "LAPTOP", IPAddress = "122.233.234.23", Time = HelperStatic.GetCurrentTimeStamp(), OperatingSystem = "WINDOWS", UserId = null, UserName = "admin@paradigm.org", AuditType = null, AuditSessionID = null };

            // credentials.AuditTrails.AuditType = Constants.ActionMethods.LoginSuccess;
            // credentials.AuditTrails.UserId = userId;
            // var res = await this._audit.AddOne(credentials.AuditTrails);
            // string resStr = res.ToString();
            // string sessionId = resStr.Split("*")[1];

            this.cultureService.RefreshCookie(this.HttpContext, cultureName, timeZoneId);

            var token = await this.tokenService.IssueToken(identity, identity.Name, role, name, userid);

            _response.Data = token;
            _response.Success = Constants.ResponseSuccess;
            return Ok(_response);
        }

        [HttpPut()]
        [IgnoreAntiforgeryToken(Order = 1000)]
        public async Task<Object> Logout()
        {
            string cookieName = this.config.Server.AntiForgery.CookieName;

            if (this.HttpContext.Request.Cookies[cookieName] != null)
                this.HttpContext.Response.Cookies.Delete(cookieName);

            string clientName = this.config.Server.AntiForgery.ClientName;

            if (this.HttpContext.Request.Cookies[clientName] != null)
                this.HttpContext.Response.Cookies.Delete(clientName);

            return await Task.FromResult(true);
        }

        private void ClearAntiforgeryCookies()
        {
            string cookieName = this.config.Server.AntiForgery.CookieName;

            if (this.HttpContext.Request.Cookies[cookieName] != null)
                this.HttpContext.Response.Cookies.Delete(cookieName);

            string clientName = this.config.Server.AntiForgery.ClientName;

            if (this.HttpContext.Request.Cookies[clientName] != null)
                this.HttpContext.Response.Cookies.Delete(clientName);
        }

        private void SetAntiforgeryCookies()
        {
            var context = this.HttpContext;
            var tokenSet = antiForgeryService.GetAndStoreTokens(context);

            if (tokenSet.RequestToken != null)
            {
                string clientName = this.config.Server.AntiForgery.ClientName;
                context.Response.Cookies.Append(clientName, tokenSet.RequestToken, new CookieOptions() { HttpOnly = false, Secure = true });
            }
        }
    }
}
