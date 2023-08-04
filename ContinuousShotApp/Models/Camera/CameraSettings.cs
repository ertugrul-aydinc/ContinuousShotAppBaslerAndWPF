using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContinuousShotApp.Models.Camera
{
    public class CameraSettings
    {
        public double ExposureTime { get; set; }
        public double Gain { get; set; }
        public double Gamma { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
