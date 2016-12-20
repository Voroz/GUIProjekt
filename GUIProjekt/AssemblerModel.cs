using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt
{
    static class Constants
    {
        public const ushort startOprBit = 5; //Defines start position for the Assembler operator in a 16 bit
        public const ushort endOprBit = 8;   //Defines end position for the Assembler operator in a 16 bit
        public const ushort startAdrBit = 9; //Defines start position for the Assembler value in a 16 bit
        public const ushort endAdrBit = 16;  //Defines end position for the Assembler value in a 16 bit
    }
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

        // Interprets the current address and runs the corresponding function
        public void processCurrentAddr() {
            ushort currAddrVal = _memory[_instructionPtr];
            Operations op = (Operations)(createMask(Constants.startOprBit, Constants.endOprBit) & currAddrVal);
            byte addr = (byte)(createMask(9, 16) & currAddrVal);

            Debug.Assert(op >= Operations.Load && op <= Operations.Call);

            switch (op) {
                case Operations.Load: {
                    byte valAtAddr = (byte)(createMask(9, 16) & _memory[addr]);
                    _workingRegister = valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.Store: {
                    _memory[addr] = _workingRegister;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.Add: {
                    byte valAtAddr = (byte)(createMask(9, 16) & _memory[addr]);
                    _workingRegister += valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.Sub: {
                    byte valAtAddr = (byte)(createMask(9, 16) & _memory[addr]);
                    _workingRegister -= valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.Jump: {
                    _instructionPtr = addr;
                } break;

                case Operations.Pjump: {
                    if (_workingRegister > 0) {
                        _instructionPtr = addr;
                    }
                } break;

                case Operations.In: {
                    _workingRegister = _input;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.Out: {
                    _output = _workingRegister;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.Call: {
                    // CALL adr
                    // RETURN
                } break;
            }
        }




        private UInt16[] _memory;
        private byte _instructionPtr;
        private byte _workingRegister;
        private byte _input;
        private byte _output;
        private readonly int _size;
    }
}
