using Basler.Pylon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContinuousShotApp.Utilities
{
    public class ComboBoxPairs
    {
        public ICameraInfo _Key { get; set; }
        public string _Value { get; set; }

        public ComboBoxPairs(ICameraInfo _key, string _value)
        {
            _Key = _key;
            _Value = _value;
        }
    }
}
