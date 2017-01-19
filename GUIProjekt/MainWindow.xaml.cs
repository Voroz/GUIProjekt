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
using Microsoft.Win32;
using System.IO;
using System.Timers;

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

            _inputTimerAssembly.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerMK.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerAssembly.Tick += OnInputTimerAssemblyElapsed;
            _inputTimerMK.Tick += OnInputTimerMKElapsed;
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, _assemblerModel.delay());
            _runTimer.Tick += OnInputTimerRunElapsed;

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));
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

        private void updateGUIMemory(byte from, byte to) {
            TextBox mkBox = TextBox_MK;

            clearMemoryRows(from, to);

            for (int i = from; i <= to && i < mkBox.LineCount; i++) {
                string mkStr = mkBox.GetLineText(i);

                if (!_assemblerModel.checkSyntaxMachine(mkStr)) {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(mkStr)) {
                    MemoryRow rad = getMMRowOfPosition(255 - i);
                    rad.ShowMemoryAdress(mkStr);
                }
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

            if (!mkBox.IsFocused || mkBox.IsReadOnly) { 
                return;
            }

            _inputTimerMK.Stop();
            _inputTimerMK.Start();
            assemblerBox.IsReadOnly = true;           
        }

        private void OnInputTimerMKElapsed(object source, EventArgs e) {
            TextBox assemblerBox = TextBox_Assembler;
            TextBox mkBox = TextBox_MK;

            _inputTimerMK.Stop();
            assemblerBox.Clear();
            clearMemoryRows(0, _previousLineCount);

            for (int i = 0; i < mkBox.LineCount; i++) {
                string mkStr = mkBox.GetLineText(i);
                ushort bits = 0;
                string assemblyStr = "";

                if (!_assemblerModel.checkSyntaxMachine(mkStr)) {
                    if (i != mkBox.LineCount - 1) {
                        assemblerBox.AppendText("\n");
                    }
                    continue;
                }

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
            assemblerBox.IsReadOnly = false;
        }


        /******************************************************
         CALL: When writing in the assembler section.
         TASK: Updates the machine code section and the memory.
        *****************************************************/ 
        private void TextBox_Assembler_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox assemblerBox = sender as TextBox;
            TextBox mkBox = TextBox_MK;
            
            if (!assemblerBox.IsFocused || assemblerBox.IsReadOnly) {
                return;
            }
            
            _inputTimerAssembly.Stop();
            _inputTimerAssembly.Start();
            mkBox.IsReadOnly = true;
        }

        private void OnInputTimerAssemblyElapsed(object source, EventArgs e) {
            TextBox assemblerBox = TextBox_Assembler;
            TextBox mkBox = TextBox_MK;
            
            _inputTimerAssembly.Stop();
            mkBox.Clear();
            clearMemoryRows(0, _previousLineCount);

            for (int i = 0; i < assemblerBox.LineCount; i++) {
                string assemblyStr = assemblerBox.GetLineText(i);
                ushort bits = 0;
                string mkStr = "";

                if (!_assemblerModel.checkSyntaxAssembly(assemblyStr)) {
                    if (i != assemblerBox.LineCount - 1) {
                        mkBox.AppendText("\n");
                    }
                    continue;
                }

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
            mkBox.IsReadOnly = false;
        }

        // TODO: Enkel tillfällig funktion för att markera rader
        void markRow(MemoryRow row) {
            if (_previousInstructionPtr != -1) {
                MemoryRow previousRow_ = getMMRowOfPosition((byte)(255 - _previousInstructionPtr));
                previousRow_.BorderThickness = new Thickness(0, 0, 0, 0);
                previousRow_.Margin = new Thickness(0, 0, 0, 0);
                Grid.SetZIndex(previousRow_, 999);
            }

            row.BorderThickness = new Thickness(4, 4, 4, 4);
            row.Margin = new Thickness(-4, -4, -4, -4);
            Grid.SetZIndex(row, 1000);

            _previousInstructionPtr = _assemblerModel.instructionPtr();
        }

        void programTick() {
            if (_assemblerModel.currentAddr() == Constants.UshortMax) {
                TextBox textBoxMK = TextBox_MK;
                TextBox textBoxAssembler = TextBox_Assembler;
                _runTimer.Stop();
                _assemblerModel.reset();
                updateGUIMemory(0, 255);

                textBoxAssembler.IsReadOnly = false;
                textBoxMK.IsReadOnly = false;

                // Mark current row
                markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

                return;
            }

            ushort currentAddr = _assemblerModel.getAddr(_assemblerModel.instructionPtr());
            Operations opr;
            byte val = (byte)_assemblerModel.extractVal(currentAddr);
            _assemblerModel.extractOperation(currentAddr, out opr);

            _assemblerModel.processCurrentAddr();

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            // Uppdatera grafiskt minnet som ändrats
            switch (opr) {
                case Operations.STORE: {
                        byte index = (byte)(val);
                        MemoryRow row = getMMRowOfPosition(255 - index);

                        row.ShowMemoryAdress(Convert.ToString(_assemblerModel.getAddr(index), 2).PadLeft(12, '0'));
                        // TODO: Update missing GUI instructionPtr;
                    } break;

                case Operations.IN: {
                        // TODO: Update missing GUI workingRegister;
                        // TODO: Update missing GUI instructionPtr;
                    } break;

                case Operations.OUT: {
                        // TODO: Update missing GUI output
                        // TODO: Update missing GUI instructionPtr;
                    } break;

                case Operations.CALL: {
                        // TODO: Update missing GUI instructionPtr;
                        byte index = (byte)(256 - _assemblerModel.stack().size());
                        MemoryRow row = getMMRowOfPosition(255 - index);

                        row.ShowMemoryAdress(Convert.ToString(_assemblerModel.getAddr(index), 2).PadLeft(12, '0'));
                    } break;

                case Operations.RETURN: {
                        // TODO: Update missing GUI instructionPtr;
                        byte index = (byte)(256 - _assemblerModel.stack().size() - 1);
                        MemoryRow row = getMMRowOfPosition(255 - index);

                        row.ClearMemoryAdress();
                    } break;

                default: {
                        // TODO: Update missing GUI instructionPtr;
                    } break;
            }
        }

        /******************************************************
         CALL: When clicking the run button.
         TASK: Runs through the entered instructions. 
         *****************************************************/
        private void Button_Run_Click(object sender, RoutedEventArgs e) {
            
            TextBox textBoxMK = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            if (!checkSyntaxMachineTextBox(textBoxMK) || textBoxMK.IsReadOnly || textBoxAssembler.IsReadOnly) {
                return;
            }

            // Vid körning av programmet vill vi inte att användaren skall kunna ändra i maskinkoden därför görs textBoxen till readOnly
            textBoxMK.IsReadOnly = true;
            textBoxAssembler.IsReadOnly = true;

            // Adds users text input to the model
            for (byte i = 0; i < textBoxMK.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBoxMK.GetLineText(i).TrimEnd(trimChars);
                ushort bits = 0;

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                    _assemblerModel.setAddr(i, Constants.UshortMax);
                    continue;
                }

                Debug.Assert(_assemblerModel.stringToMachine(str, out bits));
                _assemblerModel.setAddr(i, bits);
            }
            _runTimer.Start();
            
        }

        private void OnInputTimerRunElapsed(object source, EventArgs e)
        {
            programTick();
            
        }

        /******************************************************
         CALL: When clicking the stop button.
         TASK: Stops execution and makes the input fields changeable again.
         *****************************************************/
        private void Button_Stop_Click(object sender, RoutedEventArgs e) {
            _runTimer.Stop();
            _assemblerModel.reset();
            updateGUIMemory(0, 255);
            
            TextBox textBox = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            textBoxAssembler.IsReadOnly = false;
            textBox.IsReadOnly = false;

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));
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

        
        /******************************************************
         CALL: When clicking the Open button in the Menu
         TASK: Open a txt file from the directory.
         *****************************************************/
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".txt";
            ofd.Filter = "Text Document (.txt)|*.txt";
            if(ofd.ShowDialog() == true)
            {
                string filename = ofd.FileName;
                string txt = File.ReadAllText(filename);

                if (txt[0] == '1' || txt[0] == '0')
                {
                    TextBox_MK.Focus();
                    //TextBox_MK.Text = File.ReadAllText(filename);
                    TextBox_MK.Text = txt;
                    return;
                }

                TextBox_Assembler.Focus();
                //TextBox_Assembler.Text = File.ReadAllText(filename);
                TextBox_Assembler.Text = txt;
            }
        }

        /******************************************************
         CALL: When clicking the Save button in the Menu
         TASK: Saves the assembler code as a txt.
         *****************************************************/
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".txt";
            sfd.AddExtension = true;           
            if(sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, TextBox_Assembler.Text);
            }
        }
        

        //Lånade denna knapp för att testa SkinManager så användaren kan byta layout på programmet
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            //AppSkin orange = AppSkin.Default;
            //SkinManager.SetSkin(orange);
        }


        /******************************************************
         CALL: When clicking the pause button in the menu.
         TASK: Pauses the run through of the program and enables 
               input in the textboxes again.
         *****************************************************/
        private void Button_Pause_Click(object sender, RoutedEventArgs e)
        {
            TextBox textBoxMK = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            _runTimer.Stop();
            textBoxMK.IsReadOnly = false;
            textBoxAssembler.IsReadOnly = false;
        }

        private void Button_StepBack_Click(object sender, RoutedEventArgs e) {

        }

        private void Button_StepForward_Click(object sender, RoutedEventArgs e) {
            // TODO: I stort sett samma kod som i Button_Run_Click. Kanske ska dags att göra en funktion?

            TextBox textBoxMK = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            if (!checkSyntaxMachineTextBox(textBoxMK) || textBoxMK.IsReadOnly || textBoxAssembler.IsReadOnly) {
                return;
            }

            // Vid körning av programmet vill vi inte att användaren skall kunna ändra i maskinkoden därför görs textBoxen till readOnly
            textBoxMK.IsReadOnly = true;
            textBoxAssembler.IsReadOnly = true;

            // Adds users text input to the model
            for (byte i = 0; i < textBoxMK.LineCount; i++) {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBoxMK.GetLineText(i).TrimEnd(trimChars);
                ushort bits = 0;

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str)) {
                    _assemblerModel.setAddr(i, Constants.UshortMax);
                    continue;
                }

                Debug.Assert(_assemblerModel.stringToMachine(str, out bits));
                _assemblerModel.setAddr(i, bits);
            }
            programTick();
            textBoxMK.IsReadOnly = false;
            textBoxAssembler.IsReadOnly = false;
        }

        // Togglar MK-textbox och Assembler-textbox visibility
        // Ganska ful kod, men vågar inte röra XAML-filen så att den inte förstör text alignment
        private void TabsToggle_event(object sender, RoutedEventArgs e) {
            if (Convert.ToBoolean(AssemblyTab.IsChecked)) {
                TextBox_Assembler.Visibility = Visibility.Visible;
                TextBox_MK.Visibility = Visibility.Hidden;
                Grid.SetColumn(TextBox_Assembler, 1);
                Grid.SetColumnSpan(TextBox_Assembler, 2);
                TextBox_Assembler.MinWidth = 400;
            }
            else if(Convert.ToBoolean(MKTab.IsChecked)) {
                TextBox_Assembler.Visibility = Visibility.Hidden;
                TextBox_MK.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(TextBox_MK, 2);
                TextBox_MK.MinWidth = 400;
            }
            else if (Convert.ToBoolean(SplitTab.IsChecked)) {
                TextBox_Assembler.Visibility = Visibility.Visible;
                TextBox_MK.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(TextBox_MK, 1);
                Grid.SetColumn(TextBox_Assembler, 2);
                Grid.SetColumnSpan(TextBox_Assembler, 1);
                TextBox_MK.MinWidth = 200;
                TextBox_Assembler.MinWidth = 200;
            }
        }


        private AssemblerModel _assemblerModel;
        private byte _previousLineCount;
        private int _previousInstructionPtr = -1; // TODO: Remove this. Temporary until we have stack for step back.

        private System.Windows.Threading.DispatcherTimer _runTimer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _inputTimerMK = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _inputTimerAssembly = new System.Windows.Threading.DispatcherTimer();
    }
}
