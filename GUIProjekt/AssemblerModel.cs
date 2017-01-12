using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GUIProjekt
{
    static class Constants {
        public const byte StartOprBit = 8;   // Defines start position for the Assembler operator in a 16 bit
        public const byte EndOprBit = 11;   // Defines end position for the Assembler operator in a 16 bit
        public const byte StartValBit = 0; // Defines start position for the Assembler value in a 16 bit
        public const byte EndValBit = 7;  // Defines end position for the Assembler value in a 16 bit
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
            _executionDelay = 200;
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

        bool extractOperation(ushort bits, out Operations opr) {
            byte oprVal = (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits);
            if (!Enum.IsDefined(typeof(Operations), oprVal)) {
                opr = Operations.LOAD;
                return false;
            }
            opr = (Operations)oprVal;
            return true;
        }

        byte extractVal(ushort bits) {
            return (byte)extractValFromBits(Constants.StartValBit, Constants.EndValBit, bits);
        }

        bool isBinary(string str) {
            bool binary = true;
            foreach (char ch in str) {
                if (!(ch == '0' || ch == '1')) {
                    binary = false;
                    break;
                }
            }

            if (string.IsNullOrEmpty(str))
                binary = false;

            return binary;
        }

        public bool stringToMachine(string str, out ushort machineCode) {
            bool binary = isBinary(str);

            if (binary) {
                machineCode = Convert.ToUInt16(str, 2);
                return true;
            }
            else {
                if (!assemblyToMachine(str, out machineCode)) {
                    machineCode = 0;
                    return false;
                }

                return true;
            }
        }

        public bool machineToAssembly(ushort bits, out string assemblyCode) {
            Operations opr;
            if (!extractOperation(bits, out opr)) {
                assemblyCode = "";
                return false;
            }
            byte addr = extractVal(bits);
            assemblyCode = opr.ToString() + " " + addr;
            return true;
        }

        public bool assemblyToMachine(string assemblyString, out ushort machineCode) {
            string[] splitString = assemblyString.Split(' ');
            Operations opr;
            if (!Enum.TryParse(splitString[0], false, out opr)) {
                machineCode = 0;
                return false;
            }

            byte addr = 0;
            if (!byte.TryParse(splitString[1], out addr)) {
                machineCode = 0;
                return false;
            }

            machineCode = (ushort)opr;
            machineCode = (ushort)(machineCode << Constants.StartOprBit);
            machineCode += addr;
            return true;
        }

        public ushort currentAddr() {
            return _memory[_instructionPtr];
        }

        public void resetInstructionPtr() {
            _instructionPtr = 0;
        }

        public byte instructionPtr() {
            return _instructionPtr;
        }

        public ushort getAddr(byte idx) {
            Debug.Assert(idx >= 0 && idx < _size);
            return _memory[idx];
        }

        public void setAddr(byte idx, ushort val) {
            Debug.Assert(idx >= 0 && idx < _size);
            _memory[idx] = val;
        }

        public int delay() {
            return _executionDelay;
        }

        // TODO: Add error code as return value instead of boolean
        public bool checkSyntaxMachine(string str) {
            if (!isBinary(str)) {
                return false;
            }

            if (str.Length != 12) {
                return false;
            }

            ushort bits;
            if (!stringToMachine(str, out bits)) {
                return false;
            }

            Operations opr;
            if (!extractOperation(bits, out opr)) {
                return false;
            }

            return true;
        }

        // TODO: Add error code as return value instead of boolean
        public bool checkSyntaxAssembly(string str) {
            if (isBinary(str)) {
                return false;
            }

            string[] splitString = str.Split(' ');

            if (splitString.Length != 2)
                return false;

            Operations opr;
            if (!Enum.TryParse(splitString[0], false, out opr)) {
                return false;
            }

            byte addr = 0;
            if (!byte.TryParse(splitString[1], out addr)) {
                return false;
            }

            return true;
        }

        // TODO: Add error code as return value instead of boolean
        public bool checkSyntax(string str) {

            bool binary = isBinary(str);

            if (binary) {

                if (str.Length != 12) {
                    return false;
                }

                ushort bits;
                if (!stringToMachine(str, out bits)) {
                    return false;
                }

                Operations opr;
                if (!extractOperation(bits, out opr)) {
                    return false;
                }
            }

            else if (!binary) {
                string[] splitString = str.Split(' ');

                Operations opr;
                if (!Enum.TryParse(splitString[0], false, out opr)) {
                    return false;
                }

                byte addr = 0;
                if (!byte.TryParse(splitString[1], out addr)) {
                    return false;
                }
            }

            return true;
        }

        // Interprets the current address and runs the corresponding function
        public void processCurrentAddr() {
            ushort current = _memory[_instructionPtr];
            Operations opr = Operations.LOAD;            
            byte addr = (byte)extractVal(current);

            Debug.Assert(extractOperation(current, out opr));

            switch (opr) {
                case Operations.LOAD: {
                    byte valAtAddr = extractVal(_memory[addr]);
                    _workingRegister = valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.STORE: {
                    _memory[addr] = _workingRegister;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.ADD: {
                    byte valAtAddr = extractVal(_memory[addr]);
                    _workingRegister += valAtAddr;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.SUB: {
                    byte valAtAddr = extractVal(_memory[addr]);
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

                default: {
                    Debug.Assert(false);
                } break;
            } 
        }




        private UInt16[] _memory;
        private byte _instructionPtr;
        private byte _workingRegister;
        private byte _input;
        private byte _output;
        private readonly int _size;

        private int _executionDelay;
    }
}
