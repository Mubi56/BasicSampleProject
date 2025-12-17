using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Paradigm.Contract.Interface;
using Paradigm.Data;
using Paradigm.Data.Model;
using Paradigm.Server.Interface;

namespace Paradigm.Server.Application
{
    public static class HelperStatic
    {
        private static Random random = new Random();
        public static int GetCurrentTimeStamp()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string orderByProperty, bool desc)
        {
            string command = desc ? "OrderByDescending" : "OrderBy";
            var type = typeof(TEntity);
            var property = type.GetProperty(orderByProperty);
            var parameter = Expression.Parameter(type, "p");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
                                          source.Expression, Expression.Quote(orderByExpression));
            return source.Provider.CreateQuery<TEntity>(resultExpression);
        }
        public static string WhereBuilder(string search)
        {
            string extraWhere = "";
            if (!String.IsNullOrEmpty(search))
            {
                string currentSearch;
                string currentValue;
                string currentTable;
                var searchSplit = search.Split("+");
                foreach (var item in searchSplit)
                {
                    currentTable = item.Split("*")[0];
                    currentSearch = item.Split("*")[1];
                    currentValue = item.Split("*")[2];
                    if (currentSearch == "Status")
                    {
                        extraWhere = extraWhere + " " + currentTable + ".\"" + currentSearch + "\"" + " = " + currentValue + " and ";
                    }
                    else
                    {
                        extraWhere = extraWhere + " " + currentTable + ".\"" + currentSearch + "\"" + " ILIKE '%" + currentValue + "%'" + " and ";
                    }
                }
                extraWhere = extraWhere.TrimEnd(' ').TrimEnd('d').TrimEnd('n').TrimEnd('a');
            }
            return extraWhere;
        }
        public static string QueryFinalize(TableParamModel model)
        {
            var where = !String.IsNullOrEmpty(model.Search) ? HelperStatic.WhereBuilder(model.Search) : null;
            string query = (!String.IsNullOrEmpty(model.Search) ? "WHERE " + where : "") +
            " ORDER BY \"" + model.Sort + "\" " + model.Order +
            " OFFSET(" + model.Start + ") ROWS " +
            " FETCH FIRST(" + model.Limit + ") ROW ONLY";
            return query;
        }
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public static string GetURL(Microsoft.AspNetCore.Http.IHeaderDictionary headers)
        {
            string orgn = headers[Constants.Referer].ToString();
            return orgn;
        }
        public static Guid GetAuditSessionIdFromClaims(ClaimsIdentity identity)
        {
            Guid sessionID = Guid.Empty;
            IEnumerable<Claim> claims = identity.Claims;
            var auditSession = claims.FirstOrDefault(e => e.Type == "AuditSession");
            if (auditSession != null)
            {
                sessionID = new Guid(auditSession.Value);
            }
            return sessionID;
        }
    }
    public class GeneralService : IGeneralService
    {
        private DbContextBase _dbContext;
        public GeneralService(DbContextBase dbContext)
        {
            _dbContext = dbContext;
        }
    }

}