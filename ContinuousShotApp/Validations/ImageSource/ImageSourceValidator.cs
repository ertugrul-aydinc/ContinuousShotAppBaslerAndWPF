using ContinuousShotApp.Utilities.Constants.ValidationMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using myResult = ContinuousShotApp.Utilities.ReturnTypes;

namespace ContinuousShotApp.Validations.ImageSource
{
    public class ImageSourceValidator
    {
        public static myResult.ValidationResult CheckImageSourceIsNull(Image image)
        {
            myResult.ValidationResult validationResult = new();

            if (image.Source is null)
            {
                validationResult.IsSuccess = false;
                validationResult.Message += ImageSourceValidationMessages.ImageSourceIsNull;
            }

            return validationResult;
        }
    }
}