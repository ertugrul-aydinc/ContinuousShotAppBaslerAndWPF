using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContinuousShotApp.Utilities.ReturnTypes
{
    public class ValidationResult : IResult
    {
        public bool IsSuccess { get; set; } = true;

        public string Message { get; set; } = string.Empty;
    }
}
