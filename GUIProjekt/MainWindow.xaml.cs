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

        private bool checkSyntaxMachineTextBox(TextBox textBox) {
            // TODO: Implement (intellisens) error notification.

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

        private bool checkSyntaxAssemblyTextBox(TextBox textBox) {
            // TODO: Implement (intellisens) error notification.

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

            TextBox mkBox = sender as TextBox;
            TextBox assemblerBox = TextBox_Assembler;

            if (!mkBox.IsFocused)  // Breaking this event if textbox is updated from assembler textbox
                return;

            if (string.IsNullOrWhiteSpace(mkBox.Text)) {
                mkBox.Clear();
                return;
            }

            if (checkSyntaxMachineTextBox(mkBox)) {
                assemblerBox.Clear();
                for (int i = 0; i < mkBox.LineCount; i++) {
                    string assembly;
                    ushort bits;

                    char[] trimChars = new char[2] { '\r', '\n' };
                    _assemblerModel.stringToMachine(mkBox.GetLineText(i).TrimEnd(trimChars), out bits);
                    _assemblerModel.machineToAssembly(bits, out assembly);
                    assemblerBox.AppendText(assembly + '\n');
                }
            }
        }

        private void Button_Run_Click(object sender, RoutedEventArgs e) {
            TextBox textBox = TextBox_MK;
            if (!checkSyntaxMachineTextBox(textBox)) {
                return;
            }

            // Adds users text input to the model
            for (byte i = 0; i < textBox.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);
                ushort bits;

                Debug.Assert(_assemblerModel.stringToMachine(str, out bits));
                _assemblerModel.setAddr(i, bits);
            }
                        
            _assemblerModel.resetInstructionPtr();
            // TODO: Valid address can be 0, since LOAD is 0. How to know when program should stop?
            // TODO: Other solution over while loop, since while loop make user unable to use interface while program is running.
            while (_assemblerModel.currentAddr() != 0) {                
                // TODO: Mark current row
                Thread.Sleep(_assemblerModel.delay());
                _assemblerModel.processCurrentAddr();
            }
        }

        private void TextBox_Assembler_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox assemblerBox = sender as TextBox;
            TextBox mkBox = TextBox_MK;

            if (!assemblerBox.IsFocused)
                return;

            if (string.IsNullOrWhiteSpace(assemblerBox.Text)) {
                mkBox.Clear();
                return;
            }

            if (checkSyntaxAssemblyTextBox(assemblerBox)) {
                mkBox.Clear();
                for (int i = 0; i < assemblerBox.LineCount; i++) {
                    ushort bits;

                    char[] trimChars = new char[2] { '\r', '\n'};
                    _assemblerModel.assemblyToMachine(assemblerBox.GetLineText(i).TrimEnd(trimChars), out bits);
                    mkBox.AppendText(Convert.ToString(bits, 2).PadLeft(12, '0') + '\n');
                }
            }
        }

        private AssemblerModel _assemblerModel;
    }
}
