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
        public const ushort UshortMax = 65535;  //Max range of an ushort
    }

    enum Operations : byte {
        LOAD = 0,
        STORE,
        ADD,
        SUB,
        MUL,
        JUMP,
        PJUMP,
        IN,
        OUT,
        CALL,
        RETURN,
    }

    struct UndoStorage {
        public UndoStorage(ushort[] memory
            , MyStack<ushort> memoryStack
            , byte instructionPtr
            , ushort workingRegister
            , ushort input
            , ushort output) {
                _memory = memory;
                _memoryStack = memoryStack;
                _instructionPtr = instructionPtr;
                _workingRegister = workingRegister;
                _input = input;
                _output = output;
        }

        public ushort[] _memory;
        public MyStack<ushort> _memoryStack;
        public byte _instructionPtr;
        public ushort _workingRegister;
        public ushort _input;
        public ushort _output;
    }

    class AssemblerModel
    {
        public AssemblerModel()
        {
            _size = 256; // Leave this at 256 (many of our attributes are 8 bit)
            _memory = new ushort[_size];
            _memoryStack = new MyStack<ushort>(_memory);
            _undoStack = new Stack<UndoStorage>();
            _instructionPtr = 0;
            _workingRegister = 0;
            _input = 0;
            _output = 0;
            _executionDelay = 200;

            resetMemory();
        }


        /******************************************************
         CALL: bool ok = SelfTest();
         TASK: Used at debugging. The method calls every (testable) 
               method in the class and returns true if no bug could 
               be found. 
        *****************************************************/ 
        public bool SelfTest()
        {

            // Onödig test
            bool ok = false;
            ok = (_size == _memory.GetLength(0));

            if (!ok) {
                Debug.Write("SelfTest failed IN GUIProjekt.AssemblerModel: size of _memory was "
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

        public bool extractOperation(ushort bits, out Operations opr) {
            byte oprVal = (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits);
            if (!Enum.IsDefined(typeof(Operations), oprVal)) {
                opr = Operations.LOAD;
                return false;
            }
            opr = (Operations)oprVal;
            return true;
        }

        public byte extractVal(ushort bits) {
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
            assemblyCode = opr.ToString();

            // Special case
            if (assemblyCode == "IN"
                || assemblyCode == "OUT"
                || assemblyCode == "RETURN") {
                    return true;
            }

            // Otherwise read the value aswell
            byte addr = extractVal(bits);
            assemblyCode += " " + addr;
            return true;
        }

        public bool assemblyToMachine(string assemblyString, out ushort machineCode) {
            string[] splitString = assemblyString.Split(' ');

            // Special case where length is 1 and is a constant(number)
            if (splitString.Length == 1
                && ushort.TryParse(splitString[0], out machineCode)
                ) {
                return true;
            }

            Operations opr;
            if (!Enum.TryParse(splitString[0], false, out opr)) {
                machineCode = 0;
                return false;
            }

            // Special case where length is 1
            if (splitString.Length == 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                ) {
                    machineCode = (ushort)((ushort)opr << Constants.StartOprBit);
                    return true;
            }

            if (splitString.Length != 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                ) {
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


        /******************************************************
         CALL: ushort workingReg = workingRegister();
         TASK: Returns the working register.
        *****************************************************/ 
        public ushort workingRegister() {
            return _workingRegister;
        }


        /******************************************************
         CALL: ushort currentValue = currentAddr();
         TASK: Returns the value of the adress in the memory
               where the instruction pointer is currently at.
        *****************************************************/ 
        public ushort currentAddr() {
            return _memory[_instructionPtr];
        }


        /******************************************************
         CALL: reset();
         TASK: Sets the member variables to their initiated value.
        *****************************************************/ 
        public void reset()
        {
            _input = 0;
            _output = 0;
            _instructionPtr = 0;
            _workingRegister = 0;
            resetMemory();
        }


        /******************************************************
         CALL: resetMemory();
         TASK: Resets the memory.
        *****************************************************/ 
        public void resetMemory()
        {
            for (int i = 0; i < _size; i++)
            {
                _memory[i] = (ushort)Constants.UshortMax;
            }
            while (_memoryStack.size() > 0) {
                _memoryStack.pop();
            }
            while (_undoStack.Count > 0) {
                _undoStack.Pop();
            }
        }


        /******************************************************
         CALL: resetInstructionPtr();
         TASK: Rolls the instruction pointer back to the beginning 
               (adress 0).
        *****************************************************/ 
        public void resetInstructionPtr() {
            _instructionPtr = 0;
        }


        /******************************************************
         CALL: byte current = instructionPtr();
         TASK: Returns the instruction pointer (which adress
               it's currently pointing at).
        *****************************************************/ 
        public byte instructionPtr() {
            return _instructionPtr;
        }

        public ushort input() {
            return _input;
        }

        /******************************************************
         CALL: setInput(ushort);
         TASK: Sets input.
         *****************************************************/
        public void setInput(ushort input) {
            _input = input;
        }

        public ushort output() {
            return _output;
        }

        public void setOutput(ushort output) {
            _output = output;
        }

        public MyStack<ushort> stack()
        {
            return _memoryStack;
        }

        public Stack<UndoStorage> undoStack() {
            return _undoStack;
        }

        /******************************************************
         CALL: ushort addr = getAddr(byte);
         TASK: Returns the value in the memory of the parameter 
               value.
        *****************************************************/ 
        public ushort getAddr(byte idx) {
            Debug.Assert(idx >= 0 && idx < _size);
            return _memory[idx];
        }


        /******************************************************
         CALL: setAddr(byte, ushort);
         TASK: Sets position "byte" in memory to value "ushort".
        *****************************************************/ 
        public void setAddr(byte idx, ushort val) {
            Debug.Assert(idx >= 0 && idx < _size);
            _memory[idx] = val;
        }


        /******************************************************
         CALL: int execDelay = delay();
         TASK: Return execution delay variable. Used to improve
               smoothness of running program.
         *****************************************************/
        public int delay() {
            return _executionDelay;
        }


        /******************************************************
         CALL: bool ok = checkSyntaxMachine(string);
         TASK: Checks if parameter is approved machine code.
        *****************************************************/ 
        public bool checkSyntaxMachine(string str) {
            // TODO: Add error code as return value instead of boolean
            // Maybe a struct with error code + line number
            
            // Empty lines to create space are fine
            if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                return true;
            }

            char[] trimChars = new char[2] { '\r', '\n' };
            str = str.TrimEnd(trimChars);

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
        // Maybe a struct with error code + line number
        public bool checkSyntaxAssembly(string str) {
            if (isBinary(str) && str.Length == 12) {
                return false;
            }

            // Empty lines to create space are fine
            if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                return true;
            }

            char[] trimChars = new char[2] { '\r', '\n' };
            str = str.TrimEnd(trimChars);

            string[] splitString = str.Split(' ');

            // Special case where length is 1
            ushort constant;
            if (splitString.Length == 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                || (ushort.TryParse(splitString[0], out constant) && constant < 4096)
                ) {
                return true;
            }

            if (splitString.Length != 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                ) {
                return false;
            }

            if (splitString.Length != 2) {
                return false;
            }

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

        public bool addrIdxToUpdate(ushort command, out byte idx) {
            byte val = (byte)extractVal(command);
            Operations opr = Operations.LOAD;
            bool success = extractOperation(command, out opr);
            Debug.Assert(success);

            switch (opr) {
                case Operations.STORE: {
                        idx = (byte)(val);
                        return true;
                    }

                case Operations.CALL: {
                        idx = (byte)(255 - _memoryStack.size());
                        return true;
                    }

                case Operations.RETURN: {
                        idx = (byte)(255 - _memoryStack.size() - 1);
                        return true;
                    }

                default: {
                        idx = 0;
                        return false;
                    }
            }
        }


        /******************************************************
         CALL: processCurrentAddr();
         TASK: Interprets the current address and runs the 
               corresponding function.
        *****************************************************/
        public void processCurrentAddr() {
            ushort current = _memory[_instructionPtr];
            Operations opr = Operations.LOAD;            
            byte addr = (byte)extractVal(current);

            bool success = extractOperation(current, out opr);
            Debug.Assert(success);

            _undoStack.Push(new UndoStorage(_memory, _memoryStack, _instructionPtr, _workingRegister, _input, _output));


            switch (opr) {
                case Operations.LOAD: {
                    _workingRegister = _memory[addr];
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.STORE: {
                    _memory[addr] = _workingRegister;
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.ADD: {
                    _workingRegister += _memory[addr];
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.SUB: {
                    _workingRegister -= _memory[addr];
                    _instructionPtr = (byte)(++_instructionPtr % _size);
                } break;

                case Operations.MUL: {
                    _workingRegister *= _memory[addr];
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
                    _instructionPtr++;
                    _memoryStack.push(_instructionPtr);
                    _instructionPtr = addr;                    
                } break;

                case Operations.RETURN: {
                    _instructionPtr = (byte)_memoryStack.top();                    
                    _memoryStack.pop();
                    _memory[255 - _memoryStack.size()] = Constants.UshortMax;
                } break;

                default: {
                    Debug.Assert(false);
                } break;
            } 
        }

        public UndoStorage undo() {
            UndoStorage undoValues = _undoStack.Pop();
            _memory = undoValues._memory;
            _memoryStack = undoValues._memoryStack;
            _instructionPtr = undoValues._instructionPtr;
            _workingRegister = undoValues._workingRegister;
            _input = undoValues._input;
            _output = undoValues._output;

            return undoValues;
        }
        
        private ushort[] _memory;
        private MyStack<ushort> _memoryStack;
        private Stack<UndoStorage> _undoStack;
        private byte _instructionPtr;
        private ushort _workingRegister;
        private ushort _input;
        private ushort _output;
        private readonly int _size;
        private int _executionDelay;
    }
}
