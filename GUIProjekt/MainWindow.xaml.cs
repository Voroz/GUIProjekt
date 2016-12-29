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

        private bool checkSyntaxTextBox(TextBox textBox) {
            // TODO: Implement (intellisens) error notification.

            for (byte i = 0; i < textBox.LineCount; i++) {
                string str = textBox.GetLineText(i);

                if (!_assemblerModel.checkSyntax(str)) {
                    return false;
                }
            }
            return true;
        }

        private void TextBox_MK_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox textBox = sender as TextBox;

        }

        private void Button_Run_Click(object sender, RoutedEventArgs e) {
            TextBox textBox = TextBox_MK;
            if (!checkSyntaxTextBox(textBox)) {
                return;
            }

            // Adds users text input to the model
            for (byte i = 0; i < textBox.LineCount; i++) {
                string str = textBox.GetLineText(i);
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

        private AssemblerModel _assemblerModel;
    }
}
