using System;
using System.ComponentModel.DataAnnotations;

namespace PlanAI.Controllers
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true;

            if (DateTime.TryParse(value.ToString(), out var date))
            {
                return date >= DateTime.UtcNow.Date;
            }
            return false;
        }
    }
}
