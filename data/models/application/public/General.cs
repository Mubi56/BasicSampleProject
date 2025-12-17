using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Paradigm.Data.Model
{
    public class TableParamModel
    {
        public int Start { get; set; }
        public int? Limit { get; set; }
        public string Sort { get; set; }
        public string Order { get; set; }
        public string Search { get; set; }
    }
    public class TableParam
    {
        public int Start { get; set; }
        public int Limit { get; set; }
        public string Sort { get; set; }
        public string Order { get; set; }
        public int LimitEx
        {
            get
            {
                return this.Limit == 0 ? 10 : this.Limit;
            }
        }
        public string SortEx
        {
            get
            {
                string ret = this.Sort;
                if (String.IsNullOrEmpty(ret) || String.IsNullOrEmpty(ret))
                {
                    ret = "CreatedOn";
                }
                ret = ret == "createdDate" || ret == "serialNo" ? "CreatedOn" : ret;
                ret = $"{ret[0].ToString().ToUpper()}{ret.Substring(1)}";
                return ret;
            }
        }
        public string OrderEx
        {
            get
            {
                return String.IsNullOrEmpty(this.Order) || String.IsNullOrEmpty(this.Sort) ? "desc" : this.Order;
            }
        }
    }
    public class ActiveInactive
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
    }
    public class ActiveInactiveBool
    {
        public Guid Id { get; set; }
        public bool Status { get; set; }
    }
    public class ActiveInactiveRole
    {
        public string Id { get; set; }
        public bool Status { get; set; }
    }
    public class Education
    {
        public string Degree { get; set; }
        public int PassingYear { get; set; }
        public string Institution { get; set; }
        public string Specialization { get; set; }
    }
    public class Experience
    {
        public string Organization { get; set; }
        public string Designation { get; set; }
        public int StartYear { get; set; }
        public int StartMonth { get; set; }
        public int EndYear { get; set; }
        public int EndMonth { get; set; }
        public string Description { get; set; }
    }
    public class IdText
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
    }
    public class ForgotPassword
    {
        public string Email { get; set; }
    }

    [Table("passwordresettoken")]
    public class PasswordResetToken
    {
        [Key]
        public Guid TokenId { get; set; } // Primary key
        public Guid UserId { get; set; } // Foreign key to User
        public string Token { get; set; } // Token string
        public int Expiry { get; set; } // Expiry timestamp
        public int CreatedOn { get; set; } // Created timestamp
        public int Status { get; set; } = 1; // Default status
        public int? UsedOn { get; set; } // Nullable for when the token is used
    }
    public class ResetPassword
    {
        public string Token { get; set; }
        public string Password { get; set; }
    }
}