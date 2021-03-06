﻿using System;
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
        public const int SlowExecutionDelay = 260;
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

    enum LabelStatus : byte {
        Success,
        NoLabel,
        SyntaxError,
        Blacklisted,
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
            _undoStack = new CircularStack<UndoStorage>(1000);
            _instructionPtr = 0;
            _workingRegister = new Bit12(0);
            _input = new Bit12(0);
            _output = new Bit12(0);
            _labels = new Dictionary<string, byte>();

            resetMemory();
        }

        /******************************************************
        CALL: short mask = createMask(short, short);
        TASK: Method for extracting an interval of bits from a 
              16 bit integer.
        *****************************************************/
        private short createMask(short a, short b) {
            short r = 0;
            for (short i = a; i <= b; i++)
                r |= (short)(1 << i);

            return r;
        }

        /******************************************************
        CALL: byte val = extractValFromBits(byte, byte, short);
        TASK: Returns the machine code operation from a series of bits.
        *****************************************************/
        public short extractValFromBits(byte a, byte b, short bits) {
            short mask = (short)(createMask(a, b) & bits);
            short val = (short)(mask >> a);
            return val;
        }

        /******************************************************
        CALL: bool true = extractOperation(short, out Operations opr);
        TASK: Sets the parameter opr from bits. Returns true if 
              the bits include a valid operation.
        *****************************************************/
        public bool extractOperation(short bits, out Operations opr) {
            byte oprVal = (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits);
            if (!Enum.IsDefined(typeof(Operations), oprVal)) {
                opr = Operations.LOAD;
                return false;
            }
            opr = (Operations)oprVal;
            return true;
        }

        /******************************************************
         CALL: byte val = extractVal(short);
         NOTE: Uses extractValFromBits.
         *****************************************************/
        public byte extractVal(short bits) {
            return (byte)extractValFromBits(Constants.StartValBit, Constants.EndValBit, bits);
        }


        /******************************************************
         CALL: LabelStatus labStat = containsLabel(string, out string);
         TASK: Checks if the (assembly) string contains characters
               which indicates that it is supposed to be 
               interpreted as a label. 
         *****************************************************/
        public LabelStatus containsLabel(string str, out string label) {
            label = "";

            if (str.Length == 0)
            {
                return LabelStatus.NoLabel;
            }

            if (str.Length > 0 && str[0] != ':')
            {
                return LabelStatus.NoLabel;
            }

            if (str.Length == 1 && str[0] == ':')
            {
                return LabelStatus.SyntaxError;
            }

            if (str[0] != ':' || string.IsNullOrWhiteSpace(str[1].ToString()) || char.IsDigit(str[1]))
            {
                return LabelStatus.SyntaxError;
            }

            label = "";
            for (int i = 1; i < str.Length; i++)
            {
                if (str[i] == ' ' || str[i] == '\n' || str[i] == '\r')
                {
                    break;
                }
                label += str[i];
            }

            Operations opr = Operations.LOAD;

            if (Enum.TryParse(label, out opr))
            {
                return LabelStatus.Blacklisted;
            }

            return LabelStatus.Success;
        }

        /******************************************************
         CALL: bool yes = referencesLabel(string, out string);
         TASK: Returns true if the label exists. 
         *****************************************************/
        private bool referencesLabel(string str, out string label) {
            label = "";

            if (str.Length == 0)
            {
                return false;
            }

            string[] splitString = str.Split(' ');
            string possibleLabel = splitString[splitString.Length-1];

            if(!_labels.ContainsKey(possibleLabel))
                return false;

            label = possibleLabel;
            return true;
        }

        /******************************************************
         CALL: bool ok = addLabel(string, byte);
         TASK: Adds a new key/value pair to the label dictionary
               which maps the label string to its row.
         *****************************************************/
        public bool addLabel(string str, byte row) {
            _labels[str] = row;
            return true;
        }

        /******************************************************
         CALL: clearLabels();
         TASK: Removes all keys and values from the label 
               dictionary. 
         *****************************************************/
        public void clearLabels() {
            _labels.Clear();
        }


        /******************************************************
         CALL: bool ok = isBinary(string);
         TASK: Returns true if the inputted string only consists 
               of ones and zeros.
         *****************************************************/
        private bool isBinary(string str) {
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
         CALL: bool ok = checkSyntaxMachine(string);
         TASK: Checks if parameter is approved machine code.
        *****************************************************/
        public bool checkSyntaxMachine(string str) {
            // Empty lines to create space are fine
            if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                return true;
            }

            char[] trimChars = new char[3] { '\r', '\n', ' ' };
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

        /******************************************************
         CALL: bool ok = binaryStringToMachine(string, out Bit12);
         TASK: Returns true if the parameter string was converted 
               to Bit12 machine code successfully.
         *****************************************************/
        public bool binaryStringToMachine(string str, out Bit12 machineCode) {            
            if (string.IsNullOrWhiteSpace(str)) {
                machineCode = new Bit12(0);
                return true;
            }

            bool binary = isBinary(str);

            if (!binary || str.Length != 12) {
                machineCode = new Bit12(0);
                return false;
            }

            machineCode = new Bit12(Convert.ToInt16(str, 2));
            return true;
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

            if (binary && str.Length == 12) {
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
            if (assemblyCode == "IN" || assemblyCode == "OUT" || assemblyCode == "RETURN") {
                    return true;
            }

            // Otherwise read the value aswell
            byte addr = extractVal(bits.value());
            assemblyCode += " " + addr;
            return true;
        }


        /******************************************************
         CALL: bool ok = assemblyToMachine(string, out Bit12);
         TASK: Converts the inputted assembly string to Bit12
               machine code.
         *****************************************************/
        public bool assemblyToMachine(string assemblyString, out Bit12 machineCode) {
            if (isBinary(assemblyString) && assemblyString.Length == 12) {
                machineCode = new Bit12(0);
                return false;
            }

            
            string label = "";
            if (containsLabel(assemblyString, out label) == LabelStatus.Success) {
                label = ":" + label;
                assemblyString = assemblyString.Replace(label, "");
                if(assemblyString.Length != 0)
                    assemblyString =  assemblyString.TrimStart(' ');
            }

            // Empty lines to create space are fine
            if (assemblyString == "\r\n" || assemblyString == "\r" || assemblyString == "\n" || string.IsNullOrWhiteSpace(assemblyString)) {
                machineCode = new Bit12(0);
                return true;
            }

            char[] trimChars = new char[3] { '\r', '\n', ' ' };
            assemblyString = assemblyString.TrimEnd(trimChars);

            string[] splitString = assemblyString.Split(' ');

            // Special case where length is 1 and is a constant(number)
            if (splitString.Length == 1){
                short val = 0;
                if (short.TryParse(splitString[0], out val)) {
                    if (val < -2048 || val > 2047)
                    {
                        machineCode = new Bit12(0);
                        return false;
                    } 
                    machineCode = new Bit12(val);                                       
                    return true;
                }
            }

            Operations opr = Operations.LOAD;
            byte addr = 0;
            label = "";
            if (splitString.Length == 2
                && Enum.TryParse(splitString[0], false, out opr)
                && (byte.TryParse(splitString[1], out addr) || referencesLabel(assemblyString, out label))
                && !(splitString[0] == "IN" || splitString[0] == "OUT" || splitString[0] == "RETURN")) {                

                if(label.Length > 0)
                    addr = _labels[label];

                machineCode = new Bit12((short)opr);
                machineCode = new Bit12((short)(machineCode.value() << Constants.StartOprBit));
                machineCode += new Bit12(addr);

                return true;
            }

            if (splitString.Length == 1 
                && (splitString[0] == "IN" || splitString[0] == "OUT" || splitString[0] == "RETURN")
                && Enum.TryParse(splitString[0], false, out opr)) {
                    machineCode = new Bit12((short)((short)opr << Constants.StartOprBit));
                    return true;
            }

            machineCode = new Bit12(0);
            return false;
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

            while (_undoStack.size() > 0) {
                _undoStack.pop();
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
         CALL: setInput(Bit12);
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

        /******************************************************
         CALL: CircularStack<UndoStorage> undoStack = undoStack();
         TASK: Returns the _undoStack variable.
         *****************************************************/
        public CircularStack<UndoStorage> undoStack() {
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
         CALL: setAddr(byte, Bit12);
         TASK: Sets position "byte" in memory to value "Bit12".
        *****************************************************/ 
        public void setAddr(byte idx, Bit12 val) {
            Debug.Assert(idx >= 0 && idx < _size);
            _memory[idx] = val;
        }


        /******************************************************
         CALL: bool ok = addrIdxToUpdate(Bit12, out byte);
         TASK: Handles indexing of STORE, CALL and RETURN 
               instructions.
         *****************************************************/
        public bool addrIdxToUpdate(Bit12 command, out byte idx) {
            byte val = (byte)extractVal(command.value());
            Operations opr = Operations.LOAD;
            if (!extractOperation(command.value(), out opr)) {
                idx = 0;
                return false;
            }

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
        public bool processCurrentAddr() {
            Bit12 current = _memory[_instructionPtr];
            Operations opr = Operations.LOAD;            
            byte addr = (byte)extractVal(current.value());

            if (!extractOperation(current.value(), out opr)) {
                return false;
            }

            _undoStack.push(new UndoStorage(_memory, _memoryStack, _instructionPtr, _workingRegister, _input, _output));


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
                    else {
                        _instructionPtr++;
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
            }

            return true;
        }


        /******************************************************
         CALL: UndoStorage valuesUndone = undo();
         TASK: Returns an object which contains the previous 
               state of various objects.
         *****************************************************/
        public UndoStorage undo() {
            UndoStorage undoValues = _undoStack.top();
            _undoStack.pop();
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
        public bool SelfTest() {
            bool ok = false;
            ok = (_size == _memory.GetLength(0));

            if (!ok) {
                Debug.Write("SelfTest failed in GUIProjekt.AssemblerModel: size of _memory was "  
                    + _memory.GetLength(0)
                    + ", expected " + _size + "\n");
            }


            ok = ok && 255 == createMask(0, 7)
                 && 63 == createMask(0, 5)
                 && 2032 == createMask(4, 10)
                 && 3840 == createMask(8, 11)
                 && 4095 == createMask(0, 11)
                 && 1020 == createMask(2, 9);
            System.Diagnostics.Debug.WriteLine("createMask: " + ok);


            Operations opr;
            ok = ok && extractOperation(2800, out opr)
                 && extractOperation(256, out opr)
                 && extractOperation(0, out opr)
                 && extractOperation(1, out opr)
                 && extractOperation(1337, out opr);
            System.Diagnostics.Debug.WriteLine("extractOperation: " + ok);


            short bits1 = 400;
            short bits2 = 255;
            short bits3 = 600;
            short bits4 = 800;
            short bits5 = 1028;
            short bits6 = 2950;
            ok = ok && 1 == (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits1)
                 && 0 == (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits2)
                 && 2 == (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits3)
                 && 3 == (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits4)
                 && 4 == (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits5)
                 && 11 == (byte)extractValFromBits(Constants.StartOprBit, Constants.EndOprBit, bits6);
            System.Diagnostics.Debug.WriteLine("extractValFromBits: " + ok);


            string labelStr = "";
            ok = ok && LabelStatus.NoLabel == containsLabel("", out labelStr)
                 && LabelStatus.NoLabel == containsLabel("MUL 420", out labelStr)
                 && LabelStatus.SyntaxError == containsLabel(":", out labelStr)
                 && LabelStatus.SyntaxError == containsLabel(":5", out labelStr)
                 && LabelStatus.Blacklisted == containsLabel(":RETURN", out labelStr)
                 && LabelStatus.Blacklisted == containsLabel(":ADD 60", out labelStr)
                 && LabelStatus.Success == containsLabel(":HEJ 42", out labelStr)
                 && LabelStatus.Success == containsLabel(":VARMT 255", out labelStr);
            System.Diagnostics.Debug.WriteLine("containsLabel: " + ok);


            ok = ok && isBinary("00000000")
                 && isBinary("11111111")
                 && isBinary("01010110")
                 && isBinary("0")
                 && isBinary("1");
            System.Diagnostics.Debug.WriteLine("isBinary: " + ok);


            Bit12 machineCode = new Bit12(0);
            ok = ok && stringToMachine("000100010001", out machineCode)
                 && stringToMachine("101011101110", out machineCode)
                 && stringToMachine("011011110000", out machineCode)
                 && stringToMachine(" ", out machineCode);
            System.Diagnostics.Debug.WriteLine("stringToMachine: " + ok);


            ok = ok && binaryStringToMachine(" ", out machineCode)
                 && binaryStringToMachine("011111111110", out machineCode)
                 && binaryStringToMachine("100000000001", out machineCode);
            System.Diagnostics.Debug.WriteLine("binaryStringToMachine: " + ok);


            string assemCode = "";
            Bit12 val1 = new Bit12(1024);
            Bit12 val2 = new Bit12(2048);
            Bit12 val3 = new Bit12(2560);
            Bit12 val4 = new Bit12(1500);
            Bit12 val5 = new Bit12(666);
            Bit12 val6 = new Bit12(1);
            ok = ok && machineToAssembly(val1, out assemCode)
                 && machineToAssembly(val2, out assemCode)
                 && machineToAssembly(val3, out assemCode)
                 && machineToAssembly(val4, out assemCode)
                 && machineToAssembly(val5, out assemCode)
                 && machineToAssembly(val6, out assemCode);
            System.Diagnostics.Debug.WriteLine("machineToAssembly: " + ok);


            Bit12 bit = new Bit12(0);
            ok = ok && assemblyToMachine(" ", out bit)
                 && assemblyToMachine("\n", out bit)
                 && assemblyToMachine(":VARMT 100", out bit)
                 && assemblyToMachine("IN", out bit)
                 && assemblyToMachine("OUT", out bit)
                 && assemblyToMachine("PJUMP 200", out bit);
            System.Diagnostics.Debug.WriteLine("assemblyToMachine: " + ok);


            ok = ok && checkSyntaxMachine("000011111111")
                 && checkSyntaxMachine("001101011111")
                 && checkSyntaxMachine("100111111111")
                 && checkSyntaxMachine("111100000000")
                 && checkSyntaxMachine(" ")
                 && checkSyntaxMachine("\n");
            System.Diagnostics.Debug.WriteLine("checkSyntaxMachine: " + ok);

            return ok;
        }


        private Bit12[] _memory;
        private MyStack<Bit12> _memoryStack;
        private CircularStack<UndoStorage> _undoStack;
        private Dictionary<string, byte> _labels;
        private byte _instructionPtr;
        private Bit12 _workingRegister;
        private Bit12 _input;
        private Bit12 _output;
        private readonly int _size;
    }
}
