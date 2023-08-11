using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContinuousShotApp.Utilities.Constants.ValidationMessages
{
    public static class CameraValidationMessages
    {
        public static readonly string CameraIsNull = "Camera is null or not started yet\n";
        public static readonly string CameraIsNotGrabbing = "Camera is not grabbing\n";
        public static readonly string CameraIsAlreadyGrabbing = "Camera is already grabbing\n";
    }
}
