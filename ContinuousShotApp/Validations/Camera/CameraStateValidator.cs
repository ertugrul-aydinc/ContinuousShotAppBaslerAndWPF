using Basler.Pylon;
using ContinuousShotApp.Utilities.Constants.ValidationMessages;
using myResult = ContinuousShotApp.Utilities.ReturnTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ContinuousShotApp.Validations.Camera
{
    public class CameraStateValidator
    {
        public static myResult.ValidationResult CheckCameraIsNull(Basler.Pylon.Camera? camera)
        {
            myResult.ValidationResult validationResult = new();

            if (camera is null)
            {
                validationResult.IsSuccess = false;
                validationResult.Message += CameraValidationMessages.CameraIsNull;
            }

            return validationResult;
        }

        public static myResult.ValidationResult CheckCameraIsGrabbing(Basler.Pylon.Camera? camera)
        {
            myResult.ValidationResult validationResult = new();

            if (camera is not null && camera.StreamGrabber.IsGrabbing)
            {
                validationResult.IsSuccess = false;
                validationResult.Message += CameraValidationMessages.CameraIsAlreadyGrabbing;
            }

            return validationResult;
        }

        public static myResult.ValidationResult CheckCameraIsNotGrabbing(Basler.Pylon.Camera? camera)
        {
            myResult.ValidationResult validationResult = new();

            if (camera is not null && !camera.StreamGrabber.IsGrabbing)
            {
                validationResult.IsSuccess = false;
                validationResult.Message += CameraValidationMessages.CameraIsNotGrabbing;
            }

            return validationResult;
        }

    }
}
