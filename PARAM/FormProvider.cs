using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parameters
{
    static class FormProvider
    {
        public static Form1 Instance
        {
            get
            {
                if (_inst == null)
                    _inst = new Form1();
                return _inst;
            }
        }
        private static Form1 _inst;
    }
}
