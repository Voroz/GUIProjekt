using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt
{
    enum Operations : byte {
        Load = 0,
        Store = 1,
        Add = 2,
        Sub = 3,
        Jump = 4,
        Pjump = 5,
        In = 6,
        Out = 7,
        Call = 8,
    }

    class AssemblerModel
    {
        public AssemblerModel()
        {
            _size = 256; // Leave this at 256 (many of our attributes are 8 bit)
            _memory = new ushort[_size];
            _instructionPtr = 0;
            _workingRegister = 0;
            _input = 0;
            _output = 0;
        }

        public bool SelfTest()
        {

            // Onödig test
            bool ok = false;
            ok = (_size == _memory.GetLength(0));

            if (!ok) {
                Debug.Write("SelfTest failed IN GUIProjekt.AssemblerModel: size of _memory  was "
                    + _memory.GetLength(0)
                    + ", expected " + _size + "\n");
            }
            return ok;
        }

        // (stolen) function for extracting an interval of bits from a 16 bit integer
        ushort createMask(ushort a, ushort b) {
            ushort r = 0;
            for (ushort i = a; i <= b; i++)
                r |= (ushort)(1 << i);

            return r;
        }

        private void add(byte val) {
            _workingRegister += val;
        }

        // Interprets the current address and runs the corresponding function
        public void processCurrentAddr() {
            ushort currAddrVal = _memory[_instructionPtr];
            byte op = (byte)(createMask(5, 8) & currAddrVal);
            byte val = (byte)(createMask(9, 16) & currAddrVal);

            Debug.Assert(op >= 0 && op <= 8);

            // Switch fungerar ej (C#?)
            /*switch (op) {
                case: Operations.Load
            }*/

            if (op == (byte)Operations.Add) {
                add(val);
                _instructionPtr = (byte)(++_instructionPtr % _size);
            }
            // else if... etc




        }




        private UInt16[] _memory;
        private byte _instructionPtr;
        private byte _workingRegister;
        private byte _input;
        private byte _output;
        private readonly int _size;
    }
}
