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
        }


        /******************************************************
         CALL: bool ok = checkSyntaxMachineTextBox(TextBox);
         TASK: Checks if any line entered in the machine code 
               section contains unapproved characters.
        *****************************************************/ 
        // TODO: Add error code as return value instead of boolean
        // Maybe a struct with error code + line number
        private bool checkSyntaxMachineTextBox(TextBox textBox) {
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || str == "") {
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
        // TODO: Add error code as return value instead of boolean
        // Maybe a struct with error code + line number
        private bool checkSyntaxAssemblyTextBox(TextBox textBox) {
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || str == "") {
                    continue;
                    
                }

                if (!_assemblerModel.checkSyntaxAssembly(str)) {
                    return false;
                }             
            }
            
            return true;
        }

        private void TextBox_MK_TextChanged(object sender, TextChangedEventArgs e) {
            // TODO: Intellisens stuff
            // (use struct from checkSyntax functions with error code and line number to create highlighting and error information for user)

            TextBox mkBox = sender as TextBox;
            TextBox assemblerBox = TextBox_Assembler;

            if (!mkBox.IsFocused) {  // Breaking this event if textbox is updated from assembler textbox
                return;
            }

            assemblerBox.Clear();

            if (string.IsNullOrWhiteSpace(mkBox.Text) || !checkSyntaxMachineTextBox(mkBox)) {
                return;
            }

            for (int i = 0; i < mkBox.LineCount; i++) {
                string str = mkBox.GetLineText(i);
                ushort bits;

                if (!string.IsNullOrWhiteSpace(str)) {
                    char[] trimChars = new char[2] { '\r', '\n' };
                    _assemblerModel.stringToMachine(str.TrimEnd(trimChars), out bits);
                    _assemblerModel.machineToAssembly(bits, out str);
                }
                else {
                    str = "";
                }

                assemblerBox.AppendText(str + '\n');
                
            }
        }

        private void TextBox_Assembler_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox assemblerBox = sender as TextBox;
            TextBox mkBox = TextBox_MK;
            
            if (!assemblerBox.IsFocused) {
                return;
            }

            updateLineNumber(assemblerBox);
            mkBox.Clear();
            clearMemoryRows();

            // Todo: lägga allt i en funktion?
            if (string.IsNullOrWhiteSpace(assemblerBox.Text) || !checkSyntaxAssemblyTextBox(assemblerBox)) {
                return;
            }

            int rowPos = 255;

            for (int i = 0; i < assemblerBox.LineCount; i++) {
                string str = assemblerBox.GetLineText(i);
                ushort bits;                

                if (!string.IsNullOrWhiteSpace(str)) {
                    char[] trimChars = new char[2] { '\r', '\n' };
                    _assemblerModel.assemblyToMachine(str.TrimEnd(trimChars), out bits);  
                    str = Convert.ToString(bits, 2).PadLeft(12, '0') + '\n';                                    
                }
                else {
                    str = "\n";                    
                }

                mkBox.AppendText(str);
                MemoryRow rad = getMMRowOfPosition(rowPos - i);
                rad.ShowMemoryAdress(str);
            }
            
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
            //Vid körning av programmet vill vi inte att användaren skall kunna ändra i maskinkoden därför görs textBoxen till readOnly, sätt tillbaka när StopButton aktiveras
            textBox.IsReadOnly = true;
            textBoxAssembler.IsReadOnly = true;
            // Adds users text input to the model
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);
                ushort bits;

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || str == "") {
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
         CALL: updateLineNumber(TextBox);
         TASK: Updates the line numbers in the assembler section.
         *****************************************************/
        private void updateLineNumber(TextBox textBox) {
            if (textBox.LineCount != _numberOfLines) {
                AssemblerLineNumbers.Items.Clear();
                for (int i = 0; i < textBox.LineCount; i++) {
                    AssemblerLineNumbers.Items.Add(i);
                }
                _numberOfLines = textBox.LineCount;
            }
        }


        /******************************************************
         CALL: When clicking the stop button.
         TASK: Makes the input fields changeable again.
         *****************************************************/
        private void Button_Stop_Click(object sender, RoutedEventArgs e) {
            TextBox textBox = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            textBoxAssembler.IsReadOnly = false;
            textBox.IsReadOnly = false;
        }


        /******************************************************
         CALL: MemoryRow mr = getMMRowOfPosition(int);
         TASK: Returns the MemoryRow of the position of the paramater.
         *****************************************************/
        private MemoryRow getMMRowOfPosition(int pos)
        {
            int row = (theMemory.Children.Count)-pos;
            MemoryRow mmRow = theMemory.Children[row] as MemoryRow;
            return mmRow;
        }


        /******************************************************
         CALL: clearMemoryRows();
         TASK: Clears everything in the memory.
         *****************************************************/
        private void clearMemoryRows(){
            // TODO optimera. Testar just nu bara att cleara allt i minnet
            for (int i = 255; i > 0; i--)
            {
                MemoryRow rad = getMMRowOfPosition(i);
                rad.ClearMemoryAdress();
            }
        }


        private AssemblerModel _assemblerModel;
        private int _numberOfLines;
    }
}
