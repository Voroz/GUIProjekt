using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt
{
    class AssemblerModel
    {
        public AssemblerModel(int size)
        {
            _size = size;
            _memory = new Int16[_size];
        }

        public bool SelfTest()
        {

            // Test memory allocation
            bool ok = false;
            ok = (_size == _memory.GetLength(0));

            if (!ok) {
                Debug.Write("SelfTest failed IN GUIProjekt.AssemblerModel: size of _memory  was "
                    + _memory.GetLength(0)
                    + ", expected " + _size + "\n");
            }
            return ok;
        }


        private Int16[] _memory;
        private int _size;
    }
}
