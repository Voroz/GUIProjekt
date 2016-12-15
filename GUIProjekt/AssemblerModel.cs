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
            _memory = new bool[12, size];
        }

        public bool SelfTest()
        {

            // Test memory allocation
            bool ok = false;
            ok = (12 == _memory.GetLength(0)
                && _size == _memory.GetLength(1));

            if (!ok) {
                Debug.Write("SelfTest failed IN GUIProjekt.AssemblerModel: size of _memory  was ["
                    + _memory.GetLength(0) + "]["
                    + _memory.GetLength(1) + "]"
                    + ", expected [12][" + _size + "]\n");
            }
            return ok;
        }


        private bool[,] _memory;
        private int _size;
    }
}
