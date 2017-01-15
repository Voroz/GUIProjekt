using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace GUIProjekt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _assemblerModel = new AssemblerModel();
            _assemblerModel.SelfTest();
            showMemoryRowNumbers();
        }


        /******************************************************
         CALL: createMemoryRowNumbers();
         TASK: Displays row numbers for the memory.
        *****************************************************/
        private void showMemoryRowNumbers()
        {
            for (int i = 0; i <= 255; i++)
            {
                MemoryRow row = getMMRowOfPosition(255 - i);
                row.ShowMemoryRowNumber((byte)i);
            }
        }


        /******************************************************
         CALL: bool ok = checkSyntaxMachineTextBox(TextBox);
         TASK: Checks if any line entered in the machine code 
               section contains unapproved characters.
        *****************************************************/ 
        private bool checkSyntaxMachineTextBox(TextBox textBox) {
            // TODO: Add error code as return value instead of boolean
            // Maybe a struct with error code + line number
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                    continue;
                }
                
                if (!_assemblerModel.checkSyntaxMachine(str)) {
                    return false;
                }
            }
            return true;
        }


        /******************************************************
         CALL: bool ok = checkSyntaxAssemblyTextBox(TextBox);
         TASK: Checks if any line entered in the assembler
               section contains unapproved characters.
         *****************************************************/
        private bool checkSyntaxAssemblyTextBox(TextBox textBox) {
            // TODO: Add error code as return value instead of boolean
            // Maybe a struct with error code + line number
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                    continue;
                    
                }

                if (!_assemblerModel.checkSyntaxAssembly(str)) {
                    return false;
                }             
            }
            
            return true;
        }


        /******************************************************
         CALL: When writing in the machine code section.
         TASK: Updates the assembler section.
        *****************************************************/ 
        private void TextBox_MK_TextChanged(object sender, TextChangedEventArgs e) {
            // TODO: Intellisens stuff
            // (use struct from checkSyntax functions with error code and line number to create highlighting and error information for user)

            TextBox mkBox = sender as TextBox;
            TextBox assemblerBox = TextBox_Assembler;

            if (!mkBox.IsFocused) { 
                return;
            }

            assemblerBox.Clear();
            clearMemoryRows(0, _previousLineCount);
            updateLineNumber(mkBox.LineCount);

            if (!checkSyntaxMachineTextBox(mkBox)) {
                return;
            }

            for (int i = 0; i < mkBox.LineCount; i++) {
                string mkStr = mkBox.GetLineText(i);                
                ushort bits = 0;
                string assemblyStr = "";

                if (!string.IsNullOrWhiteSpace(mkStr)) {
                    char[] trimChars = new char[2] { '\r', '\n' };
                    _assemblerModel.stringToMachine(mkStr.TrimEnd(trimChars), out bits);
                    _assemblerModel.machineToAssembly(bits, out assemblyStr);
                    MemoryRow rad = getMMRowOfPosition(255 - i);
                    rad.ShowMemoryAdress(mkStr);
                }

                if (mkStr.Length > 0 && mkStr[mkStr.Length - 1] == '\n') {
                    assemblyStr += '\n';
                }
                assemblerBox.AppendText(assemblyStr);
            }
            
            _previousLineCount = (byte)assemblerBox.LineCount;
        }


        /******************************************************
         CALL: When writing in the assembler section.
         TASK: Updates the machine code section and the memory.
        *****************************************************/ 
        private void TextBox_Assembler_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox assemblerBox = sender as TextBox;
            TextBox mkBox = TextBox_MK;
            
            if (!assemblerBox.IsFocused) {
                return;
            }

            mkBox.Clear();
            clearMemoryRows(0, _previousLineCount);
            updateLineNumber(assemblerBox.LineCount);

            if (!checkSyntaxAssemblyTextBox(assemblerBox)) {
                return;
            }

            for (int i = 0; i < assemblerBox.LineCount; i++) {
                string assemblyStr = assemblerBox.GetLineText(i);
                ushort bits = 0;
                string mkStr = "";

                if (!string.IsNullOrWhiteSpace(assemblyStr)) {
                    char[] trimChars = new char[2] { '\r', '\n' };
                    _assemblerModel.assemblyToMachine(assemblyStr.TrimEnd(trimChars), out bits);
                    mkStr = Convert.ToString(bits, 2).PadLeft(12, '0');                    

                    MemoryRow rad = getMMRowOfPosition(255 - i);
                    rad.ShowMemoryAdress(mkStr);
                }

                if (assemblyStr.Length > 0 && assemblyStr[assemblyStr.Length - 1] == '\n') {
                    mkStr += '\n';
                }
                mkBox.AppendText(mkStr);                
            }

            _previousLineCount = (byte)assemblerBox.LineCount;
        }


        /******************************************************
         CALL: When clicking the run button.
         TASK: Runs through the entered instructions. 
         *****************************************************/
        private void Button_Run_Click(object sender, RoutedEventArgs e) {
            TextBox textBox = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            if (!checkSyntaxMachineTextBox(textBox)) {
                return;
            }

            // Vid körning av programmet vill vi inte att användaren skall kunna ändra i maskinkoden därför görs textBoxen till readOnly
            textBox.IsReadOnly = true;
            textBoxAssembler.IsReadOnly = true;

            // Adds users text input to the model
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);
                ushort bits = 0;

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                    _assemblerModel.setAddr(i, Constants.UshortMax);
                    continue;
                }

                Debug.Assert(_assemblerModel.stringToMachine(str, out bits));
                _assemblerModel.setAddr(i, bits);
            }
                        
            _assemblerModel.resetInstructionPtr();
            // TODO: Valid address can be 0, since LOAD is 0. How to know when program should stop?
            // TODO: Other solution over while loop, since while loop make user unable to use interface while program is running.
            while (_assemblerModel.currentAddr() != Constants.UshortMax) {                
                // TODO: Mark current row
                Thread.Sleep(_assemblerModel.delay());
                _assemblerModel.processCurrentAddr();
            }

            textBoxAssembler.IsReadOnly = false;
            textBox.IsReadOnly = false;
        }


        /******************************************************
         CALL: When clicking the stop button.
         TASK: Stops execution and makes the input fields changeable again.
         *****************************************************/
        private void Button_Stop_Click(object sender, RoutedEventArgs e) {
            // TODO: Stop execution
            TextBox textBox = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            textBoxAssembler.IsReadOnly = false;
            textBox.IsReadOnly = false;
        }


        /******************************************************
         CALL: updateLineNumber(TextBox);
         TASK: Updates the line numbers in the assembler section.
         *****************************************************/
        private void updateLineNumber(int newLineCount)
        {
            // Todo: Optimera så att den bara tar bort och lägger till så många rader som behövs.
            AssemblerLineNumbers.Items.Clear();
            for (int i = 0; i < newLineCount; i++) {
                AssemblerLineNumbers.Items.Add(i);
            }
        }


        /******************************************************
         CALL: MemoryRow mr = getMMRowOfPosition(int);
         TASK: Returns the MemoryRow of the position of the paramater.
         *****************************************************/
        private MemoryRow getMMRowOfPosition(int pos) {
            return theMemory.Children[(theMemory.Children.Count-1)-pos] as MemoryRow;
        }

        /******************************************************
         CALL: clearMemoryRows(int);
         TASK: Clears the rows of memory that has been removed.
         *****************************************************/
        private void clearMemoryRows(byte from, byte to) {
            // Debug.Assert(from < memLineCount && to < memLineCount);
            for (int i = from; i <= to; i++) {
                getMMRowOfPosition(255 - i).ClearMemoryAdress();
            }
        }

        private AssemblerModel _assemblerModel;
        private byte _previousLineCount;
    }
}
