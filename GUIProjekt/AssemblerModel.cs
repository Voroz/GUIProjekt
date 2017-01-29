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
        public const int FastExecutionDelay = 0;
        public const int SlowExecutionDelay = 200;
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
        public UndoStorage(Bit12[] memory
            , MyStack<Bit12> memoryStack
            , byte instructionPtr
            , Bit12 workingRegister
            , Bit12 input
            , Bit12 output) {
                _memory = new Bit12[memory.Length];
                _memoryStack = new MyStack<Bit12>(_memory);
                Array.Copy(memory, _memory, memory.Length);
                for (int i = 0; i < memoryStack.size(); i++) {
                    _memoryStack.push(_memory[255 - i]);
                }
                _instructionPtr = instructionPtr;
                _workingRegister = workingRegister;
                _output = output;
        }

        public Bit12[] _memory;
        public MyStack<Bit12> _memoryStack;
        public byte _instructionPtr;
        public Bit12 _workingRegister;
        public Bit12 _output;
    }

    class AssemblerModel
    {
        public AssemblerModel()
        {
            _size = 256; // Leave this at 256 (many of our attributes are 8 bit)
            _memory = new Bit12[_size];
            for (int i = 0; i < _size; i++) {
                _memory[i] = new Bit12(0);
            }
            _memoryStack = new MyStack<Bit12>(_memory);
            _undoStack = new Stack<UndoStorage>();
            _instructionPtr = 0;
            _workingRegister = new Bit12(0);
            _input = new Bit12(0);
            _output = new Bit12(0);

            resetMemory();
        }



        // (stolen) function for extracting an interval of bits from a 16 bit integer
        short createMask(short a, short b) {
            short r = 0;
            for (short i = a; i <= b; i++)
                r |= (short)(1 << i);

            return r;
        }

        short extractValFromBits(byte a, byte b, short bits) {
            short mask = (short)(createMask(a, b) & bits);
            short val = (short)(mask >> a);
            return val;
        }

        public bool extractOperation(short bits, out Operations opr) {
            byte oprVal = (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits);
            if (!Enum.IsDefined(typeof(Operations), oprVal)) {
                opr = Operations.LOAD;
                return false;
            }
            opr = (Operations)oprVal;
            return true;
        }

        public byte extractVal(short bits) {
            return (byte)extractValFromBits(Constants.StartValBit, Constants.EndValBit, bits);
        }


        /******************************************************
         CALL: bool ok = isBinary(string);
         TASK: Returns true if the inputted string only consists 
               of ones and zeros.
         *****************************************************/
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

        /******************************************************
         CALL: bool ok = stringToMachine(string, out Bit12);
         TASK: Converts the string to machine code and returns
               true if doing so successfully.
         *****************************************************/
        public bool stringToMachine(string str, out Bit12 machineCode) {
            if (string.IsNullOrWhiteSpace(str)) {
                machineCode = new Bit12(0);
                return true;
            }

            bool binary = isBinary(str);

            if (binary) {
                machineCode = new Bit12(Convert.ToInt16(str, 2));
                return true;
            }
            else {
                if (!assemblyToMachine(str, out machineCode)) {
                    machineCode = new Bit12(0);
                    return false;
                }

                return true;
            }
        }

        /******************************************************
         CALL: bool ok = machineToAssembly(Bit12, out string);
         TASK: Returns true if conversion from machine code
               to assembly code was done successfully.
         NOTE: Returns false if the inputted Bit12 doesn't 
               contain any (or unapproved) assembly instruction.
         *****************************************************/
        public bool machineToAssembly(Bit12 bits, out string assemblyCode) {
            Operations opr;
            if (!extractOperation(bits.value(), out opr)) {
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
            byte addr = extractVal(bits.value());
            assemblyCode += " " + addr;
            return true;
        }

        public bool assemblyToMachine(string assemblyString, out Bit12 machineCode) {
            string[] splitString = assemblyString.Split(' ');

            // Special case where length is 1 and is a constant(number)
            if (splitString.Length == 1){
                short val = 0;
                if (short.TryParse(splitString[0], out val)
                ) {
                    machineCode = new Bit12(val);
                    if (val < -2048 || val > 2047) {
                        return false;
                    }                    
                    return true;
                }
            }

            Operations opr;
            if (!Enum.TryParse(splitString[0], false, out opr)) {
                machineCode = new Bit12(0);
                return false;
            }

            // Special case where length is 1
            if (splitString.Length == 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                ) {
                    machineCode = new Bit12((short)((short)opr << Constants.StartOprBit));
                    return true;
            }

            if (splitString.Length > 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                ) {
                    machineCode = new Bit12(0);
                    return false;
            }

            byte addr = 0;
            if (!byte.TryParse(splitString[1], out addr)) {
                machineCode = new Bit12(0);
                return false;
            }

            machineCode = new Bit12((short)opr);
            machineCode = new Bit12((short)(machineCode.value() << Constants.StartOprBit));
            machineCode += new Bit12(addr);
            return true;
        }


        /******************************************************
         CALL: Bit12 workingReg = workingRegister();
         TASK: Returns the working register.
        *****************************************************/ 
        public Bit12 workingRegister() {
            return _workingRegister;
        }


        /******************************************************
         CALL: Bit12 currentValue = currentAddr();
         TASK: Returns the value of the adress in the memory
               where the instruction pointer is currently at.
        *****************************************************/
        public Bit12 currentAddr() {
            return _memory[_instructionPtr];
        }


        /******************************************************
         CALL: reset();
         TASK: Sets the member variables to their initiated value.
        *****************************************************/ 
        public void reset() {
            _output = new Bit12(0);
            _instructionPtr = 0;
            _workingRegister = new Bit12(0);
            resetMemory();
        }


        /******************************************************
         CALL: resetMemory();
         TASK: Resets the memory.
        *****************************************************/ 
        public void resetMemory() {
            for (int i = 0; i < _size; i++) {
                _memory[i] = new Bit12(0);
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

        /******************************************************
         CALL: Bit12 input = input();
         TASK: Returns the _input property.
        *****************************************************/ 
        public Bit12 input() {
            return _input;
        }

        /******************************************************
         CALL: setInput(ushort);
         TASK: Sets input.
         *****************************************************/
        public void setInput(Bit12 input) {
            _input = input;
        }

        /******************************************************
         CALL: Bit12 output = output();
         TASK: Returns the _output property.
        *****************************************************/ 
        public Bit12 output() {
            return _output;
        }

        /******************************************************
         CALL: setOutput(Bit12);
         TASK: Sets the _output property to the parameter Bit12.
        *****************************************************/ 
        public void setOutput(Bit12 output) {
            _output = output;
        }

        /******************************************************
         CALL: MyStack<Bit12> stack = stack();
         TASK: Returns the memory stack.
        *****************************************************/ 
        public MyStack<Bit12> stack()
        {
            return _memoryStack;
        }

        public Stack<UndoStorage> undoStack() {
            return _undoStack;
        }

        /******************************************************
         CALL: Bit12 addr = getAddr(byte);
         TASK: Returns the value in the memory of the parameter 
               value.
        *****************************************************/
        public Bit12 getAddr(byte idx) {
            Debug.Assert(idx >= 0 && idx < _size);
            return _memory[idx];
        }


        /******************************************************
         CALL: setAddr(byte, ushort);
         TASK: Sets position "byte" in memory to value "ushort".
        *****************************************************/ 
        public void setAddr(byte idx, Bit12 val) {
            Debug.Assert(idx >= 0 && idx < _size);
            _memory[idx] = val;
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

            Bit12 bits;
            if (!stringToMachine(str, out bits)) {
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

            // Special case where length is 1 and is a constant(number)
            if (splitString.Length == 1) {
                short val = 0;
                if (short.TryParse(splitString[0], out val)
                ) {
                    if (val < -2048 || val > 2047) {
                        return false;
                    }
                    return true;
                }
            }

            // Special case where length is 1
            if (splitString.Length == 1
                && (splitString[0] == "IN"
                || splitString[0] == "OUT"
                || splitString[0] == "RETURN")
                ) {
                return true;
            }

            if (splitString.Length > 1
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

        public bool addrIdxToUpdate(Bit12 command, out byte idx) {
            byte val = (byte)extractVal(command.value());
            Operations opr = Operations.LOAD;
            bool success = extractOperation(command.value(), out opr);
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
            Bit12 current = _memory[_instructionPtr];
            Operations opr = Operations.LOAD;            
            byte addr = (byte)extractVal(current.value());

            bool success = extractOperation(current.value(), out opr);
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
                    if (_workingRegister > new Bit12(0)) {
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
                    _memoryStack.push(new Bit12(_instructionPtr));
                    _instructionPtr = addr;                    
                } break;

                case Operations.RETURN: {
                    _instructionPtr = (byte)_memoryStack.top().value();                    
                    _memoryStack.pop();
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
            _output = undoValues._output;

            return undoValues;
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
                Debug.Write("SelfTest failed in GUIProjekt.AssemblerModel: size of _memory was "  
                    + _memory.GetLength(0)
                    + ", expected " + _size + "\n");
            }

            ok = ok && isBinary("00000000")
                 && isBinary("11111111")
                 && isBinary("01010110");
            System.Diagnostics.Debug.WriteLine("isBinary: " + ok);

            ok = ok && checkSyntaxMachine("000011111111")
                 && checkSyntaxMachine("001101011111")
                 && checkSyntaxMachine("100111111111")
                 && checkSyntaxMachine("111100000000");
            System.Diagnostics.Debug.WriteLine("checkSyntaxMachine: " + ok);

            ok = ok && checkSyntaxAssembly("LOAD 0")
                 && checkSyntaxAssembly("STORE 48")
                 && checkSyntaxAssembly("SUB 5")
                 && checkSyntaxAssembly("IN")
                 && checkSyntaxAssembly("OUT")
                 && checkSyntaxAssembly("ADD 255")
                 && checkSyntaxAssembly("RETURN")
                 && checkSyntaxAssembly("CALL 10")
                 && checkSyntaxAssembly("PJUMP 0");
            System.Diagnostics.Debug.WriteLine("checkSyntaxAssembly: " + ok);

            Bit12 machineCode = new Bit12(0);
            ok = ok && stringToMachine("000100010001", out machineCode)
                 && stringToMachine("101011101110", out machineCode)
                 && stringToMachine("011011110000", out machineCode);
            System.Diagnostics.Debug.WriteLine("stringToMachine: " + ok);


            return ok;
        }


        private Bit12[] _memory;
        private MyStack<Bit12> _memoryStack;
        private Stack<UndoStorage> _undoStack;
        private byte _instructionPtr;
        private Bit12 _workingRegister;
        private Bit12 _input;
        private Bit12 _output;
        private readonly int _size;
    }
}
