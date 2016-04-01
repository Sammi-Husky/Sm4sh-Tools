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
                return _inst;
            }
        }
        private static readonly Form1 _inst = new Form1();
    }
}
