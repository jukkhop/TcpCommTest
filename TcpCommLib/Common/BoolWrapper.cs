using MicroMvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpCommLib
{
    public class BoolWrapper : ObservableObject
    {
        private bool _value;
        public bool Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }

        public BoolWrapper(bool value)
        {
            Value = value;
        }
    }
}