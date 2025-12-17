namespace Paradigm.Service.Repository
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Claims;
    using System.Collections.Generic;

    using Microsoft.Extensions.Options;

    using Paradigm.Data;
    using Paradigm.Data.Model;
    using Paradigm.Contract.Model;
    using Paradigm.Contract.Interface;
    using Microsoft.EntityFrameworkCore;
    using System;

    public interface IUserRepository : ILocalAuthenticationService, IEntityRepository<User> { }
    public class UserRepository : EntityRepository<User>, IUserRepository
    {
        private readonly Service.Config config;
        private readonly IDeviceProfiler deviceProfiler;
        private ICryptoService crypto;
        private readonly DbContextBase db;

        public UserRepository(DbContextBase context, IOptions<Service.Config> config, ICryptoService crypto, IDeviceProfiler deviceProfiler) : base(context)
        {
            this.config = config.Value;
            this.crypto = crypto;
            this.deviceProfiler = deviceProfiler;
            this.db = context;
        }
        public async Task<ClaimsIdentity> ResolveGoogleUser(string username, string password, bool isSuperAdmin)
        {
            var login = await db.User.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username && x.IsSuperAdmin == isSuperAdmin);

            if (login != null)
            {
                var roles = await db.UserRole.Where(x => x.UserId == login.UserId).ToListAsync();
                var claims = await (
                    from roleSecurityClaim in db.RoleSecurityClaim
                    join clm in db.SecurityClaim on roleSecurityClaim.SecurityClaimId equals clm.SecurityClaimId
                    join userRole in db.UserRole on roleSecurityClaim.RoleId equals userRole.RoleId
                    where userRole.UserId == login.UserId
                    select new RoleSecurityClaim
                    {
                        RoleId = roleSecurityClaim.RoleId,
                        SecurityClaimId = roleSecurityClaim.SecurityClaimId,
                        Value = roleSecurityClaim.Value
                    }
                ).Distinct().ToListAsync();

                Model.User user = new Model.User()
                {
                    UserId = login.UserId.ToString(),
                    Username = login.Username,
                    CultureName = login.CultureName,
                    DisplayName = login.DisplayName,
                    TimeZoneId = login.TimeZoneId,
                    Enabled = login.Enabled,
                    Roles = roles,
                    Claims = claims
                };

                string fingerprint = this.deviceProfiler.DeriveFingerprint(user);
                ClaimsIdentity identity = user.ToClaimsIdentity(this.config.ClaimsNamespace, fingerprint);
                return identity;
            }

            return null;
        }

        public async Task<ClaimsIdentity> ResolveUser(string username, string password, bool isAdmin)
        {
            //var login = await this.FindByAsync($"WHERE 'Username' = '{username}' AND 'IsSuperAdmin' = '{isSuperAdmin}'"); 
            var login = await db.User.FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower() && x.IsSuperAdmin == isAdmin);

            if (login != null)
            {
                var roles = await db.UserRole.Where(x => x.UserId == login.UserId).ToListAsync();
                var claims = await (
                    from roleSecurityClaim in db.RoleSecurityClaim
                    join clm in db.SecurityClaim on roleSecurityClaim.SecurityClaimId equals clm.SecurityClaimId
                    join userRole in db.UserRole on roleSecurityClaim.RoleId equals userRole.RoleId
                    where userRole.UserId == login.UserId
                    select new RoleSecurityClaim
                    {
                        RoleId = roleSecurityClaim.RoleId,
                        SecurityClaimId = roleSecurityClaim.SecurityClaimId,
                        Value = roleSecurityClaim.Value
                    }
                ).Distinct().ToListAsync();

                Model.User user = new Model.User()
                {
                    UserId = login.UserId.ToString(),
                    Username = login.Username,
                    CultureName = login.CultureName,
                    DisplayName = login.DisplayName,
                    TimeZoneId = login.TimeZoneId,
                    Enabled = login.Enabled,
                    Roles = roles,
                    Claims = claims
                };

                if (this.crypto.CheckKey(login.PasswordHash, login.PasswordSalt, password))
                {
                    string fingerprint = this.deviceProfiler.DeriveFingerprint(user);
                    ClaimsIdentity identity = user.ToClaimsIdentity(this.config.ClaimsNamespace, fingerprint);

                    return identity;
                }
            }

            return null;
        }


        public async Task<IUser> ResolveUser(string username)
        {
            //var user = await this.FindByAsync($"WHERE 'Username' = '{username}'");
            var user = await db.User.FirstOrDefaultAsync(X => X.Username.ToLower() == username.ToLower());

            if (null != user)
            {
                return new Model.User
                {
                    UserId = user.UserId.ToString(),
                    Username = user.Username,
                    CultureName = user.CultureName,
                    DisplayName = user.DisplayName,
                    TimeZoneId = user.TimeZoneId,
                    Enabled = user.Enabled
                };
            }
            return null;
        }


        public async Task<bool> ValidateUser(string username)
        {
            var login = await this.FindByAsync($"WHERE \"Username\" = '{username}'");

            return login == null;
        }
    }
}