﻿using System;
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
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.InteropServices;


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
            _currentTextBox = TextBox_Assembler;
            updateGUIMemory(0, 255, _currentTextBox);
            _inputTimerAssembly = new System.Windows.Threading.DispatcherTimer();
            _inputTimerMK = new System.Windows.Threading.DispatcherTimer();
            _runTimer = new System.Windows.Threading.DispatcherTimer();
            _commandWindow = new Commands();
            _aboutWin = new About();
            Closing += OnClosing;
            _assemblySaved = true;
            _mkSaved = true;

            _keyPressStack = new CircularStack<Key>(20);
            _password = "12321";

            _inputTimerAssembly.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerAssembly.Tick += OnInputTimerAssemblyElapsed;
            _inputTimerMK.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerMK.Tick += OnInputTimerMKElapsed;
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Slider_FastForward.Value);
            _runTimer.Tick += OnInputTimerRunElapsed;

            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr())); // Mark current row
            Slider_FastForward.Value = 200; // Can't be specified in the XAML file, bug

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));
            ValueRow_InstructionPointer.HideChildElements();

            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyDownEvent,new KeyEventHandler(keyDown), true);
        }

        


        enum ButtonType : byte
        {
            Play,
            Stop,
            Pause,
        }




        public enum MapType : uint {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] 
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key) {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result) {
                case -1:
                    break;
                case 0:
                    break;
                case 1: {
                        ch = stringBuilder[0];
                        break;
                    }
                default: {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }




        private void keyDown(object sender, KeyEventArgs e) {
            _keyPressStack.push(e.Key);
            CircularStack<Key> stackCopy = _keyPressStack.Copy();
            string str = "";

            for (int i = 0; i < _password.Length && stackCopy.size() > 0; i++) {
                str += GetCharFromKey(stackCopy.top());
                stackCopy.pop();
            }
            str = new string(str.Reverse().ToArray());
            if (str == _password) {
                MenuItem_Secret.Visibility = Visibility.Visible;
                MenuItem_Default.IsChecked = false;
                MenuItem_Visual.IsChecked = false;
                MenuItem_Dark.IsChecked = false;
                MenuItem_Secret.IsChecked = true;
                changeSkin(Skins.Secret);
                userMsg("Secret skin unlocked!");
            }
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


            for (int i = 0; i < 5; i++)
            {
                MemoryRow stackRow = getStackRowOfPosition(i);
                stackRow.ShowMemoryRowNumber((byte)(255 - i));
            }
        }




        /******************************************************
         CALL: updateGUIMemory(byte, byte, TextBox);
         TASK: Updates the graphics of the different parts
               of the memory.
         *****************************************************/
        private void updateGUIMemory(byte from, byte to, TextBox textBox)
        {
            for (int i = from; i <= to; i++)
            {
                string str = "";
                if (i < textBox.LineCount)
                {
                    str = textBox.GetLineText(i);
                }

                char[] trimChars = new char[3] { '\r', '\n', ' ' };
                str = str.TrimEnd(trimChars);

                Bit12 val = new Bit12(0);
                if (!string.IsNullOrWhiteSpace(str))
                {
                    if (textBox == TextBox_Assembler)
                        _assemblerModel.assemblyToMachine(str, out val);
                    else
                    {
                        if (_assemblerModel.checkSyntaxMachine(str))
                        {
                            short tempval = Convert.ToInt16(str, 2);
                            val = new Bit12(tempval);
                        }
                    }
                }

                if (i > 250)
                {
                    MemoryRow stackRow = getStackRowOfPosition(255 - i);
                    stackRow.ShowMemoryAdress(val);
                }

                MemoryRow rad = getMMRowOfPosition(255 - i);
                rad.ShowMemoryAdress(val);
            }
        }




        /******************************************************
         CALL: bool syntaxOK = checkSyntaxActiveTextbox();
         TASK: Checks if the inputted text in the currently
               active textbox is valid.
        *****************************************************/
        private bool checkSyntaxActiveTextbox()
        {
            if (_currentTextBox == TextBox_Assembler)
            {
                return checkSyntaxAssemblyTextBox(_currentTextBox);
            }
            else if (_currentTextBox == TextBox_MK)
            {
                return checkSyntaxMKTextBox(_currentTextBox);
            }

            Debug.Assert(true);
            return false;
        }




        /******************************************************
         CALL: bool ok = checkSyntaxMKTextBox(TextBox);
         TASK: Checks if any line entered in the machine code 
               section contains unapproved characters.
        *****************************************************/
        private bool checkSyntaxMKTextBox(TextBox textBox)
        {
            bool ok = true;

            for (int i = 0; i < textBox.LineCount; i++)
            {
                char[] trimChars = new char[3] { '\r', '\n', ' ' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);

                Bit12 val = new Bit12(0);
                if (!_assemblerModel.binaryStringToMachine(str, out val))
                {
                    errorCode("Syntax error, row " + i + ": " + "\"" + str + "\"");
                    ok = false;
                }
            }

            return ok;
        }




        /******************************************************
         CALL: bool ok = checkSyntaxAssemblyTextBox(TextBox);
         TASK: Checks if any line entered in the assembler
               section contains unapproved characters.
         *****************************************************/
        private bool checkSyntaxAssemblyTextBox(TextBox textBox)
        {
            bool ok = true;

            for (int i = 0; i < textBox.LineCount; i++)
            {
                char[] trimChars = new char[3] { '\r', '\n', ' ' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);

                Bit12 val = new Bit12(0);
                if (!_assemblerModel.assemblyToMachine(str, out val))
                {
                    errorCode("Syntax error, row " + i + ": " + "\"" + str + "\"");
                    ok = false;
                }
            }

            return ok;
        }




        /******************************************************
        CALL: When writing in the machine code section.
        TASK: Updates the assembler section.
       *****************************************************/
        private void TextBox_MK_TextChanged(object sender, TextChangedEventArgs e)
        {
            _mkSaved = false;

            if (!TextBox_MK.IsFocused || TextBox_MK.IsReadOnly)
            {
                return;
            }

            _inputTimerMK.Stop();
            _inputTimerMK.Start();
        }




        /******************************************************
         CALL: When the dispatch timer interval controlling 
               input in the machine code textbox has elapsed.
         TASK: Processes the input in the machine textbox.
         *****************************************************/
        private void OnInputTimerMKElapsed(object source, EventArgs e)
        {
            _inputTimerMK.Stop();

            if (TextBox_MK.LineCount > 256)
            {
                errorCode("Exceeded maximum lines in machine code editor.");
                return;
            }

            updateGUIMemory((byte)0, (byte)(TextBox_MK.LineCount - 1), TextBox_MK);

            // Update deleted lines memory aswell
            int nrOfDeletedLines = _previousLineCount - TextBox_MK.LineCount;
            if (nrOfDeletedLines >= 0)
            {
                updateGUIMemory((byte)(TextBox_MK.LineCount - 1), (byte)(TextBox_MK.LineCount - 1 + nrOfDeletedLines), TextBox_MK);
            }

            updateGUIMemory((byte)0, (byte)(TextBox_MK.LineCount - 1), TextBox_MK);
            _previousLineCount = (byte)TextBox_MK.LineCount;
        }




        /******************************************************
         CALL: When writing in the assembler section.
         TASK: Updates the machine code section and the memory.
        *****************************************************/
        private void TextBox_Assembler_TextChanged(object sender, TextChangedEventArgs e)
        {
            _assemblySaved = false;

            if (!TextBox_Assembler.IsFocused || TextBox_Assembler.IsReadOnly)
            {
                return;
            }

            _inputTimerAssembly.Stop();
            _inputTimerAssembly.Start();
        }




        /******************************************************
         CALL: When the dispatch timer interval controlling 
               input in the assembly box has elapsed.
         TASK: Processes the input in the assembly textbox.
         *****************************************************/
        private void OnInputTimerAssemblyElapsed(object source, EventArgs e)
        {
            _inputTimerAssembly.Stop();

            if (TextBox_Assembler.LineCount > 256)
            {
                errorCode("Exceeded maximum lines in assembler editor.");
                return;
            }

            storeLabels();
            updateGUIMemory((byte)0, (byte)TextBox_Assembler.LineCount, TextBox_Assembler);

            // Update deleted lines memory aswell
            int nrOfDeletedLines = _previousLineCount - TextBox_Assembler.LineCount;
            if (nrOfDeletedLines >= 0)
            {
                updateGUIMemory((byte)(TextBox_Assembler.LineCount - 1), (byte)(TextBox_Assembler.LineCount - 1 + nrOfDeletedLines), TextBox_Assembler);
            }

            updateGUIMemory((byte)0, (byte)(TextBox_Assembler.LineCount - 1), TextBox_Assembler);
            _previousLineCount = (byte)TextBox_Assembler.LineCount;
        }




        /******************************************************
         CALL: storeLabels();
         TASK: Stores every label added in the assembly textbox.
        *****************************************************/
        private void storeLabels()
        {
            _assemblerModel.clearLabels();

            for (int i = 0; i < TextBox_Assembler.LineCount; i++)
            {
                string label;
                if (_assemblerModel.containsLabel(TextBox_Assembler.GetLineText(i), out label) == LabelStatus.Success)
                {
                    _assemblerModel.addLabel(label, (byte)i);
                }
            }
        }




        /******************************************************
         CALL: splitString(string str, char ch, int maxSplit);
         TASK: Splits string when finding ch,
               and if no ch is found before Count
               reaches maxSplit, split anyway.
        *****************************************************/
        private string[] splitString(string str, char ch, int maxSplit)
        {
            List<string> strList = new List<string>();
            bool addedEndOfStr = false;

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ch)
                {
                    strList.Add(str.Remove(i, str.Length - i));
                    str = str.Substring(i + 1);
                    i = -1;
                    continue;
                }
                if (maxSplit != 0)
                {
                    if (i == maxSplit)
                    {
                        strList.Add(str.Remove(i, str.Length - i));
                        str = str.Substring(i + 1);
                        i = -1;
                        continue;
                    }
                }
                if (i == str.Length - 1 && str[i] != ch)
                {
                    strList.Add(str);
                    str = "";
                    addedEndOfStr = true;
                    continue;
                }

            }

            if (!addedEndOfStr)
            {
                strList.Add("");
            }


            string[] strArr = new string[strList.Count];
            for (int i = 0; i < strList.Count; i++)
            {
                strArr[i] = strList[i];
            }

            return strArr;
        }




        /******************************************************
         CALL: markRow(MemoryRow row);
         TASK: Marks which row to be executed in runtime.
        *****************************************************/
        private void markRow(MemoryRow row)
        {
            if (_previousInstructionPtr != -1)
            {
                MemoryRow previousRow = getMMRowOfPosition((byte)(255 - _previousInstructionPtr));
                previousRow.MemoryRow_Border.Visibility = System.Windows.Visibility.Hidden;
                Grid.SetZIndex(previousRow, 999);
            }

            row.MemoryRow_Border.Visibility = System.Windows.Visibility.Visible;
            Grid.SetZIndex(row, 1000);

            _previousInstructionPtr = _assemblerModel.instructionPtr();
        }




        /******************************************************
         CALL: programTick();
         TASK: Progresses the execution of the program 
               one instruction.
         *****************************************************/
        private void programTick()
        {

            Bit12 currentAddr = _assemblerModel.getAddr(_assemblerModel.instructionPtr());
            Operations opr;
            byte val = (byte)_assemblerModel.extractVal(currentAddr.value());

            _assemblerModel.extractOperation(currentAddr.value(), out opr);

            if (opr == Operations.RETURN && _assemblerModel.stack().size() == 0)
            {
                errorCode("Attempted Return on an empty stack.");
                pauseProgram();
                return;
            }

            if (!_assemblerModel.processCurrentAddr())
            {
                errorCode("Invalid operation.");
                pauseProgram();
                return;
            }

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            // Update graphics of changed memory
            byte index;
            if (_assemblerModel.addrIdxToUpdate(currentAddr, out index))
            {
                if (opr != Operations.STORE)
                {
                    index++;
                }

                MemoryRow row = getMMRowOfPosition(255 - index);
                row.ShowMemoryAdress(_assemblerModel.getAddr(index));

                if (index > 250)
                {
                    MemoryRow stackRow = getStackRowOfPosition(255 - index);
                    stackRow.ShowMemoryAdress(_assemblerModel.getAddr(index));
                }
            }

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));

            //First bit on output sets the light
            lightIfOutputIsOn();
        }




        /******************************************************
         CALL: bool ok = textToModel(TextBox);
         TASK: Inserts the input in the textbox to the memory.
         *****************************************************/
        private bool textToModel(TextBox textBox)
        {

            storeLabels();

            if (!checkSyntaxActiveTextbox())
            {
                return false;
            }

            for (int i = 0; i < textBox.LineCount; i++)
            {
                char[] trimChars = new char[3] { '\r', '\n', ' ' };
                string str = textBox.GetLineText(i).TrimEnd(trimChars);
                Bit12 bits = new Bit12(0);

                bool success = _assemblerModel.stringToMachine(str, out bits);
                Debug.Assert(success);

                _assemblerModel.setAddr((byte)i, bits);
            }
            return true;
        }




        /******************************************************
         CALL: bool starting = InitProgramStart();
         TASK: Returns true if successfully converting the
               state of the application to running.
         *****************************************************/
        private bool initProgramStart()
        {
            if (_runTimer.IsEnabled || _inputTimerAssembly.IsEnabled || _inputTimerMK.IsEnabled)
            {
                return false;
            }
            clearUserMsg();

            if (!textToModel(_currentTextBox))
            {
                return false;
            }

            Keyboard.ClearFocus();
            _currentTextBox.IsReadOnly = true;
            _currentTextBox.Foreground = (Brush)FindResource("TextBoxForegroundOn");
            userMsg("Running...");
            return true;
        }




        /******************************************************
         CALL: stopProgram();
         TASK: Stops the execution of the program and resets
               various elements.
        *****************************************************/
        private void stopProgram()
        {
            _runTimer.Stop();
            _assemblerModel.reset();
            updateGUIMemory(0, 255, _currentTextBox);

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));

            showButtonAsEnabled(ButtonType.Stop);
            lightOff();
            Keyboard.ClearFocus();
            _currentTextBox.Foreground = (Brush)FindResource("TextBoxForegroundOff");
            clearUserMsg();
            userMsg("Execution was stopped.");

            _currentTextBox.IsReadOnly = false;

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));
        }




        /******************************************************
         CALL: pauseProgram();
         TASK: Pauses execution of the program.
        *****************************************************/
        private void pauseProgram()
        {
            _runTimer.Stop();
            showButtonAsEnabled(ButtonType.Pause);
            userMsg("Execution was paused.");
        }




        /******************************************************
         CALL: When the _runTimer interval has elapsed.
         TASK: Calls programTick().
         *****************************************************/
        private void OnInputTimerRunElapsed(object source, EventArgs e)
        {
            programTick();
        }




        /******************************************************
         CALL: When clicking the Open button in the Menu.
         TASK: Open a txt file from the directory.
         *****************************************************/
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".txt";
            ofd.Filter = "Text Document (.txt)|*.txt";

            if (_assemblerModel.undoStack().size() != 0 || _inputTimerAssembly.IsEnabled || _inputTimerMK.IsEnabled)
            {
                errorCode("Cannot open file right now.");
                return;
            }

            if (ofd.ShowDialog() == true)
            {
                _currentTextBox.Focus();
                _currentTextBox.Text = File.ReadAllText(ofd.FileName);
                userMsg("Opened file \"" + ofd.FileName + "\"");
            }
        }




        /******************************************************
         CALL: When clicking the Save button in the menu.
         TASK: Saves the inputted assembler code as a .txt file.
         *****************************************************/
        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".txt";
            sfd.AddExtension = true;

            if (sfd.ShowDialog() == true)
            {
                if (_currentTextBox == TextBox_MK)
                {
                    _mkSaved = true;
                }
                else if (_currentTextBox == TextBox_Assembler)
                {
                    _assemblySaved = true;
                }
                File.WriteAllText(sfd.FileName, _currentTextBox.Text);
                String time = DateTime.Now.ToString();
                userMsg("Saved successfully " + time);
            }
        }




        /******************************************************
         CALL: When clicking the Exit button in the Menu
         TASK: Closes the application.
         *****************************************************/
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }




        /******************************************************
        CALL: Clicking Assembly in the Mode header.
        TASK: Changes the program to assembly mode.
        *****************************************************/
        private void MenuItem_Assembler_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Assembly.IsChecked)
            {
                return;
            }
            if (_assemblerModel.undoStack().size() != 0)
            {
                errorCode("Cannot change mode while program is running.");
                return;
            }
            _currentTextBox = TextBox_Assembler;
            updateGUIMemory(0, 255, _currentTextBox);
            _assemblerModel.reset();
            label_txtBox_header.Content = "Assembly";
            MenuItem_Assembly.IsChecked = true;
            MenuItem_MachineCode.IsChecked = false;
            TextBox_Assembler.Visibility = Visibility.Visible;
            TextBox_MK.Visibility = Visibility.Collapsed;
            userMsg("Mode was changed from Machine to Assembly.");
        }




        /******************************************************
        CALL: Clicking Machine Code in the Mode header.
        TASK: Changes the program to machine code mode.
        *****************************************************/
        private void MenuItem_MachineCode_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_MachineCode.IsChecked)
            {
                return;
            }
            if (_assemblerModel.undoStack().size() != 0)
            {
                errorCode("Cannot change mode while program is running.");
                return;
            }

            _currentTextBox = TextBox_MK;
            updateGUIMemory(0, 255, _currentTextBox);
            _assemblerModel.reset();
            label_txtBox_header.Content = "Machine Code";
            MenuItem_MachineCode.IsChecked = true;
            MenuItem_Assembly.IsChecked = false;
            TextBox_MK.Visibility = Visibility.Visible;
            TextBox_Assembler.Visibility = Visibility.Collapsed;
            userMsg("Mode was changed from Assembly to Machine.");
        }




        /******************************************************
       CALL: Clicking one of the options in the Skins header.
       TASK: Changes the colors of the application depending on
             which option was chosen.
       *****************************************************/
        private void MenuItem_Skins_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

            MenuItem_Default.IsChecked = false;
            MenuItem_Visual.IsChecked = false;
            MenuItem_Dark.IsChecked = false;
            MenuItem_Secret.IsChecked = false;
            item.IsChecked = true;

            Skins selected;
            Enum.TryParse(item.Header.ToString(), out selected);

            changeSkin(selected);
        }




        /******************************************************
         CALL: When clicking the About button in the menu.
         TASK: Displays info about the development.
         *****************************************************/
        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            _aboutWin.Close();
            _aboutWin = new About();
            _aboutWin.Left = Application.Current.MainWindow.Left + 60;
            _aboutWin.Top = Application.Current.MainWindow.Top + 60;

            _aboutWin.Show();
        }




        /***********************************************************
         CALL: When clicking the Commands button in the menu.
         TASK: Displays legal Commands supported by the application.
         ************************************************************/
        private void MenuItem_Commands_Click(object sender, RoutedEventArgs e)
        {
            _commandWindow.Close();
            _commandWindow = new Commands();
            _commandWindow.Left = Application.Current.MainWindow.Left;
            _commandWindow.Top = Application.Current.MainWindow.Top;
            _commandWindow.Show();
        }




        /***********************************************************
         CALL: When closing the application
         TASK: Closing all windows opened within the application.
         ************************************************************/
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (!_assemblySaved || !_mkSaved)
            {
                MessageBoxResult result = MessageBox.Show("Exit the application without saving?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }
            _aboutWin.Close();
            _commandWindow.Close();
        }




        /******************************************************
         CALL: When clicking the run button.
         TASK: Runs through the entered instructions. 
         *****************************************************/
        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            if (!initProgramStart())
            {
                return;
            }
            _runTimer.Start();
            showButtonAsEnabled(ButtonType.Play);

        }




        /******************************************************
         CALL: When clicking the pause button in the menu.
         TASK: Pauses the run through of the program.
         *****************************************************/
        private void Button_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_assemblerModel.undoStack().size() == 0)
            {
                return;
            }

            pauseProgram();
        }




        /******************************************************
        CALL: When clicking the stop button.
        TASK: Calls stopProgram();
        *****************************************************/
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            stopProgram();
        }




        /******************************************************
         CALL: Clicking the step back button.
         TASK: Rolls back the program one step i.e. undo the 
               previous operation.
        *****************************************************/
        private void Button_StepBack_Click(object sender, RoutedEventArgs e)
        {
            if (_runTimer.IsEnabled)
            {
                errorCode("Cannot undo while running the application.");
                return;
            }

            if (_assemblerModel.undoStack().size() == 0)
            {
                errorCode("Nothing to undo.");
                return;
            }

            if (_assemblerModel.undoStack().size() == 1)
            {
                Keyboard.ClearFocus();
                _currentTextBox.Foreground = (Brush)FindResource("TextBoxForegroundOff");
                clearUserMsg();
                _currentTextBox.IsReadOnly = false;
                showButtonAsEnabled(ButtonType.Stop);
            }

            UndoStorage undoValues = _assemblerModel.undo();
            Bit12 currentAddr = _assemblerModel.getAddr(_assemblerModel.instructionPtr());
            Operations opr = Operations.LOAD;
            _assemblerModel.extractOperation(currentAddr.value(), out opr);

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            // Update graphics of changed memory
            byte index;
            if (_assemblerModel.addrIdxToUpdate(currentAddr, out index))
            {
                if (opr == Operations.RETURN)
                {
                    index += 2;
                }

                MemoryRow row = getMMRowOfPosition(255 - index);
                row.ShowMemoryAdress(_assemblerModel.getAddr(index));

                if (index > 250)
                {
                    MemoryRow stackRow = getStackRowOfPosition(255 - index);
                    stackRow.ShowMemoryAdress(_assemblerModel.getAddr(index));
                }
            }

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());

            lightIfOutputIsOn();

            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));
        }




        /******************************************************
         CALL: Clicking the step forward button.
         TASK: Progresses the execution of the program one step.
        *****************************************************/
        private void Button_StepForward_Click(object sender, RoutedEventArgs e)
        {
            if (_assemblerModel.undoStack().size() == 0 && !initProgramStart())
            {
                return;
            }

            if (_assemblerModel.undoStack().size() == 0) {
                pauseProgram();
            }
            programTick();
        }




        /******************************************************
        CALL: Changing the slider.
        TASK: Updates the input depending on how the user
              interacted with the slider.
        *****************************************************/
        private void Slider_Input_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            _assemblerModel.setInput(new Bit12((short)slider.Value));
            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
        }




        /******************************************************
       CALL: Changing the slider.
       TASK: Delays the execution speed of the run through 
             of the program.
       *****************************************************/
        private void Slider_FastForward_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)slider.Value);
        }




        /******************************************************
        CALL: showButtonAsEnabled(ButtonType).
        TASK: Swaps the image file to the image for an activated button.
        *****************************************************/
        private void showButtonAsEnabled(ButtonType buttonType)
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/media-play-8x.png", UriKind.Relative);
            var uriSource1 = new Uri(@"/GUIProjekt;component/images/media-stop-8x.png", UriKind.Relative);
            var uriSource2 = new Uri(@"/GUIProjekt;component/images/media-pause-8x.png", UriKind.Relative);

            switch (buttonType)
            {
                case ButtonType.Play:
                    {
                        uriSource = new Uri(@"/GUIProjekt;component/images/media-play-8x-green.png", UriKind.Relative);
                    } break;
                case ButtonType.Stop:
                    {

                    } break;
                case ButtonType.Pause:
                    {
                        uriSource2 = new Uri(@"/GUIProjekt;component/images/media-pause-8x-blue.png", UriKind.Relative);
                    } break;
                default:
                    {

                    } break;
            }

            Playicon.Source = new BitmapImage(uriSource);
            Stopicon.Source = new BitmapImage(uriSource1);
            Pauseicon.Source = new BitmapImage(uriSource2);
        }




        /******************************************************
         CALL: MemoryRow mr = getMMRowOfPosition(int);
         TASK: Returns the MemoryRow of the position of the paramater.
         *****************************************************/
        private MemoryRow getMMRowOfPosition(int pos)
        {
            return theMemory.Children[(theMemory.Children.Count - 1) - pos] as MemoryRow;
        }




        /******************************************************
         CALL: MemoryRow mr = getStackRowOfPosition(int);
         TASK: Returns the StackRow of the position of the paramater.
         *****************************************************/
        private MemoryRow getStackRowOfPosition(int pos)
        {
            return theStack.Children[(theStack.Children.Count - 1) - pos] as MemoryRow;
        }




        /******************************************************
         CALL: errorCode("I want to display this to the user");
         TASK: Displays msg on screen in TextBoxError.
         *****************************************************/
        private void errorCode(String errorMsg)
        {
            TextBlock_MessageBox.Inlines.Add(new Run(errorMsg + "\n") { Foreground = Brushes.Red });
            ScrollViewer_MessageBox.ScrollToEnd();
        }




        /******************************************************
         CALL: userMsg("I want to display this to the user");
         TASK: Displays msg on screen in TextBoxMsg.
         *****************************************************/
        private void userMsg(String userMsg)
        {
            TextBlock_MessageBox.Inlines.Add(new Run(userMsg + "\n") { Foreground = Brushes.Blue });
            ScrollViewer_MessageBox.ScrollToEnd();
        }




        /******************************************************
         CALL: clearUserMsg();
         TASK: Empty user message screen.
         *****************************************************/
        private void clearUserMsg()
        {
            TextBlock_MessageBox.Text = "";
        }




        /******************************************************
         CALL: lightIfOutputIsRight();
         TASK: Lights bulb if output is ON.
        *****************************************************/
        void lightIfOutputIsOn()
        {
            short lightup = _assemblerModel.extractValFromBits((byte)(0), (byte)(0), _assemblerModel.output().value());
            if (lightup > 0)
                lightOn();
            else
                lightOff();
        }




        /******************************************************
         CALL: lightOn();
         TASK: Makes the light bulb light up.
        *****************************************************/
        private void lightOn()
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/bulbon.png", UriKind.Relative);

            bulb.Source = new BitmapImage(uriSource);
        }




        /******************************************************
         CALL: lightOff();
         TASK: Turns the light bulb off.
        *****************************************************/
        private void lightOff()
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/bulboff.png", UriKind.Relative);

            bulb.Source = new BitmapImage(uriSource);
        }




        // Should be in SkinManager, but how to access getMMRowOfPosition from there?
        public void changeSkin(Skins skin) {
            ResourceDictionary selectedDictionary = SkinManager.GetSkin(skin);
            App.Current.MainWindow.Resources.MergedDictionaries.Add(selectedDictionary);

            if (_assemblerModel.undoStack().size() == 0) {
                _currentTextBox.Foreground = (Brush)FindResource("TextBoxForegroundOff");
            }
            else {
                _currentTextBox.Foreground = (Brush)FindResource("TextBoxForegroundOn");
            }

            for (int i = 0; i <= 255; i++) {
                getMMRowOfPosition(255 - i).ChangeSkin(selectedDictionary);
            }

            for (int i = 0; i < 5; i++) {
                getStackRowOfPosition(i).ChangeSkin(selectedDictionary);
            }

            ValueRow_InstructionPointer.ChangeSkin(selectedDictionary);
            ValueRow_Input.ChangeSkin(selectedDictionary);
            ValueRow_Output.ChangeSkin(selectedDictionary);
            ValueRow_WorkingRegister.ChangeSkin(selectedDictionary);
        }







        private AssemblerModel _assemblerModel;
        private TextBox _currentTextBox;
        private byte _previousLineCount;
        private int _previousInstructionPtr = -1;
        private System.Windows.Threading.DispatcherTimer _runTimer;
        private System.Windows.Threading.DispatcherTimer _inputTimerAssembly;
        private System.Windows.Threading.DispatcherTimer _inputTimerMK;
        private Commands _commandWindow;
        private About _aboutWin;
        private bool _assemblySaved;
        private bool _mkSaved;
        private CircularStack<Key> _keyPressStack;
        private string _password;

    }
}
