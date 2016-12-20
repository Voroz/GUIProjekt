using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt
{
    enum Constants : byte
    {
        StartOprBit = 8, //Defines start position for the Assembler operator in a 16 bit
        EndOprBit = 11,   //Defines end position for the Assembler operator in a 16 bit
        StartAdrBit = 0, //Defines start position for the Assembler value in a 16 bit
        EndAdrBit = 7,  //Defines end position for the Assembler value in a 16 bit
    }
    enum Operations : byte {
        LOAD = 0,
        STORE = 1,
        ADD = 2,
        SUB = 3,
        JUMP = 4,
        PJUMP = 5,
        IN = 6,
        OUT = 7,
        CALL = 8,
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

        ushort extractValFromBits(byte a, byte b, ushort bits) {
            ushort mask = (ushort)(createMask(a, b) & bits);
            ushort val = (ushort)(mask >> a);
            return val;
        }

        // Interprets the current address and runs the corresponding function
        public void processCurrentAddr() {
            ushort currAddrVal = _memory[_instructionPtr];
            Operations op = (Operations)(extractValFromBits((byte)Constants.StartOprBit, (byte)Constants.EndOprBit, currAddrVal));
            byte addr = (byte)(extractValFromBits((byte)Constants.StartAdrBit, (byte)Constants.EndAdrBit, currAddrVal));

            Debug.Assert(op >= Operations.LOAD && op <= Operations.CALL);

            switch (op) {
                case Operations.LOAD: {
                    byte valAtAddr = (byte)(createMask(9, 16) & _memory[addr]);
                    _workingRegister = valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.STORE: {
                    _memory[addr] = _workingRegister;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.ADD: {
                    byte valAtAddr = (byte)(createMask(9, 16) & _memory[addr]);
                    _workingRegister += valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.SUB: {
                    byte valAtAddr = (byte)(createMask(9, 16) & _memory[addr]);
                    _workingRegister -= valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.JUMP: {
                    _instructionPtr = addr;
                } break;

                case Operations.PJUMP: {
                    if (_workingRegister > 0) {
                        _instructionPtr = addr;
                    }
                } break;

                case Operations.IN: {
                    _workingRegister = _input;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.OUT: {
                    _output = _workingRegister;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.CALL: {
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
