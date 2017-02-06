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


            _inputTimerAssembly.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerAssembly.Tick += OnInputTimerAssemblyElapsed;
            _inputTimerMK.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerMK.Tick += OnInputTimerMKElapsed;
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, (int)Slider_FastForward.Value);
            _runTimer.Tick += OnInputTimerRunElapsed;

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));
            ValueRow_InstructionPointer.HideChildElements();
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
         CALL: bool ok = checkSyntaxMachineTextBox(TextBox);
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
                if (!_assemblerModel.binaryStringToMachine(str, out val)) {
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
                if (!_assemblerModel.assemblyToMachine(str, out val)) {
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
            TextBox mkBox = sender as TextBox;

            if (!mkBox.IsFocused || mkBox.IsReadOnly)
            {
                return;
            }

            _inputTimerMK.Stop();
            _inputTimerMK.Start();
        }




        private void OnInputTimerMKElapsed(object source, EventArgs e)
        {
            _inputTimerMK.Stop();

            if (TextBox_MK.LineCount > 256)
            {
                errorCode("Exceeded maximum lines in assembler editor.");
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
            TextBox assemblerBox = sender as TextBox;
            
            if (!assemblerBox.IsFocused || assemblerBox.IsReadOnly)
            {
                return;
            }
            
            _inputTimerAssembly.Stop();
            _inputTimerAssembly.Start();
        }




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




        private void programTick()
        {

            Bit12 currentAddr = _assemblerModel.getAddr(_assemblerModel.instructionPtr());
            Operations opr;
            byte val = (byte)_assemblerModel.extractVal(currentAddr.value());

            _assemblerModel.extractOperation(currentAddr.value(), out opr);

            if (opr == Operations.RETURN && _assemblerModel.stack().size() == 0)
            {
                _runTimer.Stop();
                errorCode("Attempted Return on an empty stack.");
                return;
            }

            if (!_assemblerModel.processCurrentAddr())
            {
                errorCode("Invalid operation.");
            }

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            // Uppdatera grafiskt minnet som ändrats
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




        private bool InitProgramStart()
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

            _currentTextBox.IsReadOnly = true;
            _currentTextBox.Foreground = Brushes.LightGray;            
            userMsg("Running...");
            return true;
        }




        private void OnInputTimerRunElapsed(object source, EventArgs e)
        {
            programTick();
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

            if (_runTimer.IsEnabled || _inputTimerAssembly.IsEnabled || _inputTimerMK.IsEnabled)
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
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".txt";
            sfd.AddExtension = true;      
     
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, _currentTextBox.Text);               
                String time = DateTime.Now.ToString();
                userMsg("Saved successfully " + time);
            }
        }




        /******************************************************
         CALL: When clicking the Exit button in the Menu
         TASK: Gives the user a messageBox Yes/No to Exit.
         *****************************************************/
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Exit the application without saving?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
             
  


        /******************************************************
        CALL: Clicking Assembly in the Mode header.
        TASK: Changes the program to assembly mode.
        *****************************************************/
        private void Assembler_Click(object sender, RoutedEventArgs e)
        {
            if (Assembler.IsChecked)
                return;
            _currentTextBox = TextBox_Assembler;
            updateGUIMemory(0, 255, _currentTextBox);
            _assemblerModel.reset();
            label_txtBox_header.Content = "Assembly";
            Assembler.IsChecked = true;
            MachineCode.IsChecked = false;
            TextBox_Assembler.Visibility = Visibility.Visible;
            TextBox_MK.Visibility = Visibility.Collapsed;
        }




        /******************************************************
        CALL: Clicking Machine Code in the Mode header.
        TASK: Changes the program to machine code mode.
        *****************************************************/
        private void MachineCode_Click(object sender, RoutedEventArgs e)
        {
            if (MachineCode.IsChecked)
                return;
            _currentTextBox = TextBox_MK;
            updateGUIMemory(0, 255, _currentTextBox);
            _assemblerModel.reset();
            label_txtBox_header.Content = "Machine Code";
            MachineCode.IsChecked = true;
            Assembler.IsChecked = false;
            TextBox_MK.Visibility = Visibility.Visible;
            TextBox_Assembler.Visibility = Visibility.Collapsed;
        }




        /******************************************************
       CALL: Clicking one of the options in the Skins header.
       TASK: Changes the colors of the application depending on
             which option was chosen.
       *****************************************************/
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

            Default.IsChecked = false;
            Orange.IsChecked = false;
            Visual.IsChecked = false;
            DefaultAlt.IsChecked = false;
            item.IsChecked = true;

            Skins selected;
            Enum.TryParse(item.Header.ToString(), out selected);

            ResourceDictionary selectedDictionary = SkinManager.GetSkin(selected);
            this.Resources.MergedDictionaries.Add(selectedDictionary);

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




        /******************************************************
         CALL: When clicking the About button in the menu.
         TASK: Displays info about the devolopment.
         *****************************************************/
        private void About_Click(object sender, RoutedEventArgs e)
        {
            _aboutWin.Close();
            _aboutWin = new About();
            _aboutWin.Left = Application.Current.MainWindow.Left + 60;
            _aboutWin.Top = Application.Current.MainWindow.Top + 60;

            _aboutWin.Show();
        }




        /***********************************************************
         CALL: When clicking the Commands button in the menu.
         TASK: Displays leagal Commands supported by the application.
         ************************************************************/
        private void Commands_Click(object sender, RoutedEventArgs e)
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
            _aboutWin.Close();
            _commandWindow.Close();
        }
       



        /******************************************************
         CALL: When clicking the run button.
         TASK: Runs through the entered instructions. 
         *****************************************************/
        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            if (!InitProgramStart())
            {
                return;
            }
            _runTimer.Start();
            playOn();

        }

        void playOn()
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/media-play-8x-green.png", UriKind.Relative);
            var uriSource1 = new Uri(@"/GUIProjekt;component/images/media-stop-8x.png", UriKind.Relative);
            var uriSource2 = new Uri(@"/GUIProjekt;component/images/media-pause-8x.png", UriKind.Relative);

            Playicon.Source = new BitmapImage(uriSource);
            Stopicon.Source = new BitmapImage(uriSource1);
            Pauseicon.Source = new BitmapImage(uriSource2);
        }

        void stopOn()
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/media-play-8x.png", UriKind.Relative);
            var uriSource1 = new Uri(@"/GUIProjekt;component/images/media-stop-8x-red.png", UriKind.Relative);
            var uriSource2 = new Uri(@"/GUIProjekt;component/images/media-pause-8x.png", UriKind.Relative);

            Playicon.Source = new BitmapImage(uriSource);
            Stopicon.Source = new BitmapImage(uriSource1);
            Pauseicon.Source = new BitmapImage(uriSource2);
        }

        void pauseOn()
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/media-play-8x.png", UriKind.Relative);
            var uriSource1 = new Uri(@"/GUIProjekt;component/images/media-stop-8x.png", UriKind.Relative);
            var uriSource2 = new Uri(@"/GUIProjekt;component/images/media-pause-8x-blue.png", UriKind.Relative);

            Playicon.Source = new BitmapImage(uriSource);
            Stopicon.Source = new BitmapImage(uriSource1);
            Pauseicon.Source = new BitmapImage(uriSource2);
        }



        /******************************************************
         CALL: When clicking the pause button in the menu.
         TASK: Pauses the run through of the program and enables 
               input in the textboxes again.
         *****************************************************/
        private void Button_Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_assemblerModel.undoStack().size() == 0)
                return;

            _runTimer.Stop();            
            pauseOn();
        }




        /******************************************************
        CALL: When clicking the stop button.
        TASK: Stops execution and makes the input fields changeable again.
        *****************************************************/
        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {

            if (_assemblerModel.undoStack().size() == 0)
                return;

            _runTimer.Stop();
            _assemblerModel.reset();
            updateGUIMemory(0, 255, _currentTextBox);

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));

            stopOn();
            lightOff();
            _currentTextBox.Foreground = Brushes.Black;
            clearUserMsg();

            _currentTextBox.IsReadOnly = false;

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));
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
                errorCode("Cannot do this while running the application.");
                return;
            }

            if (_assemblerModel.undoStack().size() == 0) {
                errorCode("Cannot do this with an empty return stack.");
                return;
            }

            if (_assemblerModel.undoStack().size() == 1)
            {
                _currentTextBox.Foreground = Brushes.Black;
                clearUserMsg();
                _currentTextBox.IsReadOnly = false;
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
            if (_assemblerModel.undoStack().size() == 0 && !InitProgramStart())
            {
                return;
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
       CALL: Toggling the fast forward button on.
       TASK: Increases the execution speed of the run through 
             of the program.
       *****************************************************/
        
        /*
        private void Button_FastForward_Checked(object sender, RoutedEventArgs e) {
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, Constants.FastExecutionDelay);
        }
        */

        /******************************************************
        CALL: Toggling the fast forward button off.
        TASK: Sets the execution speed to it's usual setting.
        *****************************************************/
        /*
        private void Button_FastForward_Unchecked(object sender, RoutedEventArgs e) {
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, Constants.SlowExecutionDelay);
        }
        /*


          
         
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
         
       


        /******************************************************
         CALL: Clicking the drop down list to change skin.
         TASK: Changes the skin color (theme).
        *****************************************************/
        private void changeSkinEvent(object sender, RoutedEventArgs e)
        {
            ComboBoxItem item = sender as ComboBoxItem;
            
            if (!item.IsFocused)
                return;

            Skins selected;
            Enum.TryParse(item.Content.ToString(), out selected);
            ResourceDictionary selectedDictionary = SkinManager.GetSkin(selected);
            this.Resources.MergedDictionaries.Add(selectedDictionary);


            for (int i = 0; i <= 255; i++)
            {
                getMMRowOfPosition(255 - i).ChangeSkin(selectedDictionary);
            }

            for (int i = 0; i < 5; i++)
            {
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
       
    }
}
