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
using System.Windows.Controls.Primitives;

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
            updateGUIMemory(0, 255);

            _inputTimerAssembly.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerMK.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _inputTimerAssembly.Tick += OnInputTimerAssemblyElapsed;
            _inputTimerMK.Tick += OnInputTimerMKElapsed;
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, _assemblerModel.delay());
            _runTimer.Tick += OnInputTimerRunElapsed;

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));
        }


        /******************************************************
         CALL: createMemoryRowNumbers();
         TASK: Displays row numbers for the memory.
        *****************************************************/
        private void showMemoryRowNumbers() {
            for (int i = 0; i <= 255; i++) {
                MemoryRow row = getMMRowOfPosition(255 - i);
                row.ShowMemoryRowNumber((byte)i);
            }

            // TODO: Fixa grafisk stack så den bara visar översta 5 adresser i stacken
            // TODO: Denna for loopen kommer inte visa korrekta radnummer efter det.
            for (int i = 0; i < 5; i++) {
                MemoryRow stackRow = getStackRowOfPosition(i);
                stackRow.ShowMemoryRowNumber((byte)(255-i));
            }
        }

        private void updateGUIMemory(byte from, byte to) {
            TextBox mkBox = TextBox_MK;

            for (int i = from; i <= to; i++) {
                string mkStr = "";
                if (i < mkBox.LineCount) {
                    mkStr = mkBox.GetLineText(i);
                }

                if (!_assemblerModel.checkSyntaxMachine(mkStr)) {
                    continue;
                }

                char[] trimChars = new char[2] { '\r', '\n' };
                mkStr = mkStr.TrimEnd(trimChars);

                short val = 0;
                if (!string.IsNullOrWhiteSpace(mkStr)) {
                    val = Convert.ToInt16(mkStr, 2);
                }
                Bit12 bit12Val = new Bit12(val);

                if (i > 250) {
                    MemoryRow stackRow = getStackRowOfPosition(255 - i);
                    //changeColor(stackRow);
                    stackRow.ShowMemoryAdress(bit12Val);
                }

                MemoryRow rad = getMMRowOfPosition(255 - i);
                rad.ShowMemoryAdress(bit12Val);
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
                    errorCode("Syntax error row " + i + " " + str + " not a valid command \n");
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
                    errorCode("Syntax error row "+ i +" " + str +" not a valid command \n");
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

            for (int i = 0; i < mkBox.LineCount; i++) {
                string mkStr = mkBox.GetLineText(i);
                Bit12 bits = new Bit12(0);
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
                }

                short val = 0;
                short.TryParse(mkStr, out val);
                Bit12 bit12Val = new Bit12(val);

                MemoryRow row = getMMRowOfPosition(255 - i);
                //changeColor(row);                
                row.ShowMemoryAdress(bit12Val);

                if (mkStr.Length > 0 && mkStr[mkStr.Length - 1] == '\n') {
                    assemblyStr += '\n';
                }
                assemblerBox.AppendText(assemblyStr);
            }

            // Update deleted lines memory aswell
            int nrOfDeletedLines = _previousLineCount - mkBox.LineCount;
            if (nrOfDeletedLines >= 0) {
                updateGUIMemory((byte)(mkBox.LineCount - 1), (byte)(mkBox.LineCount - 1 + nrOfDeletedLines));
            }
            updateGUIMemory((byte)0, (byte)(mkBox.LineCount - 1));

            _previousLineCount = (byte)mkBox.LineCount;
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

            for (int i = 0; i < assemblerBox.LineCount; i++) {
                string assemblyStr = assemblerBox.GetLineText(i);
                Bit12 bits = new Bit12(0);
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
                    mkStr = Convert.ToString(bits.value(), 2).PadLeft(12, '0');

                    if (mkStr.Length > 12) {
                        mkStr = mkStr.Substring(mkStr.Length - 12);
                    }
                }

                short val = 0;
                short.TryParse(mkStr, out val);
                Bit12 bit12Val = new Bit12(val);

                MemoryRow row = getMMRowOfPosition(255 - i);
                
                row.ShowMemoryAdress(bit12Val);

                if (assemblyStr.Length > 0 && assemblyStr[assemblyStr.Length - 1] == '\n') {
                    mkStr += '\n';
                }
                mkBox.AppendText(mkStr);
            }

            // Update deleted lines memory aswell
            int nrOfDeletedLines = _previousLineCount - assemblerBox.LineCount;
            if (nrOfDeletedLines >= 0) {
                updateGUIMemory((byte)(assemblerBox.LineCount - 1), (byte)(assemblerBox.LineCount - 1 + nrOfDeletedLines));
            }
            updateGUIMemory((byte)0, (byte)(assemblerBox.LineCount - 1));

            _previousLineCount = (byte)assemblerBox.LineCount;
            mkBox.IsReadOnly = false;
        }

        // TODO: Enkel tillfällig funktion för att markera rader
        void markRow(MemoryRow row) {
            if (_previousInstructionPtr != -1) {                
                MemoryRow previousRow = getMMRowOfPosition((byte)(255 - _previousInstructionPtr));
                previousRow.MemoryRow_Border.Visibility = System.Windows.Visibility.Hidden;
                Grid.SetZIndex(previousRow, 999);
            }

            row.MemoryRow_Border.Visibility = System.Windows.Visibility.Visible;
            Grid.SetZIndex(row, 1000);

            _previousInstructionPtr = _assemblerModel.instructionPtr();
        }

        void programTick() {

            Bit12 currentAddr = _assemblerModel.getAddr(_assemblerModel.instructionPtr());
            Operations opr;
            byte val = (byte)_assemblerModel.extractVal(currentAddr.value());

            _assemblerModel.extractOperation(currentAddr.value(), out opr);

            _assemblerModel.processCurrentAddr();

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            // Uppdatera grafiskt minnet som ändrats
            byte index;
            if (_assemblerModel.addrIdxToUpdate(currentAddr, out index)) {
                if (opr != Operations.STORE) {
                    index++;
                }

                MemoryRow row = getMMRowOfPosition(255 - index);
                row.ShowMemoryAdress(_assemblerModel.getAddr(index));

                if (index > 250) {
                    MemoryRow stackRow = getStackRowOfPosition(255 - index);
                    stackRow.ShowMemoryAdress(_assemblerModel.getAddr(index));
                }
            }

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());

            //TODO testade att tända lampan om output är över 0
            short lightup = 0;
            if(_assemblerModel.output().value() > (short)lightup)
                lightBulb("bulbon");
            else
                lightBulb("bulboff");
            ////////////////////////////////////////////////////
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));
        }

        //TODO Test för lampan
        void lightBulb(String imagename)
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/"+imagename+".png", UriKind.Relative);
            bulb.Source = new BitmapImage(uriSource);
        }
        //TODO Test för lampan
        void lightBulb()
        {
            var uriSource = new Uri(@"/GUIProjekt;component/images/bulboff.png", UriKind.Relative);
            bulb.Source = new BitmapImage(uriSource);
        }

        /******************************************************
         CALL: runProgram(TextBox textBoxMK, TextBox textBoxAssembler)
         TASK: Runs through the entered instructions if syntax is ok. 
         *****************************************************/

        
        bool assemblyTextToModel(TextBox textBoxAssembler) {

            if (!checkSyntaxAssemblyTextBox(textBoxAssembler))
            {
                return false;
            }

            // Adds users text input to the model
            for (byte i = 0; i < textBoxAssembler.LineCount; i++)
            {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBoxAssembler.GetLineText(i).TrimEnd(trimChars);
                Bit12 bits = new Bit12(0);

                // Empty lines to create space are fine
                if (str == "\r\n" || str == "\r" || str == "\n" || string.IsNullOrWhiteSpace(str))
                {
                    _assemblerModel.setAddr(i, new Bit12(0));
                    continue;
                }

                bool success = _assemblerModel.stringToMachine(str, out bits);
                Debug.Assert(success);

                _assemblerModel.setAddr(i, bits);
            }
            return true;
        }

        bool InitProgramStart() {
            if (_runTimer.IsEnabled) {
                return false;
            }

            if (!assemblyTextToModel(TextBox_Assembler)) {
                return false;
            }

            TextBox_Assembler.IsReadOnly = true;
            TextBox_MK.IsReadOnly = true;

            clearUserMsg();

            return true;
        }

        /******************************************************
         CALL: When clicking the run button.
         TASK: Runs through the entered instructions. 
         *****************************************************/
        private void Button_Run_Click(object sender, RoutedEventArgs e) 
        {
            if (!InitProgramStart()) {
                return;
            }
            _runTimer.Start();          
        }

        private void OnInputTimerRunElapsed(object source, EventArgs e) {
            programTick();
        }

        
        /******************************************************
         CALL: Clicking the step forward button.
         TASK: Progresses the execution of the program one step.
        *****************************************************/
        private void Button_StepForward_Click(object sender, RoutedEventArgs e)
        {
            if (!InitProgramStart()) {
                return;
            }
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

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));
            clearUserMsg();
            lightBulb();
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
        
        private MemoryRow getStackRowOfPosition(int pos) {
            return theStack.Children[(theStack.Children.Count - 1) - pos] as MemoryRow;
        }

        /******************************************************
         CALL: errorCode("I want to display this to the user");
         TASK: displays msg on screen in TextBoxError
         *****************************************************/
        void errorCode(String errorMsg)
        {
            SolidColorBrush br = new SolidColorBrush(Colors.Red);
            textBoxError.Foreground = br;
            textBoxError.Text += errorMsg;
        }

        /******************************************************
         CALL: userMsg("I want to display this to the user");
         TASK: displays msg on screen in TextBoxMsg
         *****************************************************/
        void userMsg(String userMsg)
        {
            SolidColorBrush br = new SolidColorBrush(Colors.Blue);
            textBoxMsg.Foreground = br;
            textBoxMsg.Text += userMsg;
        }

        /******************************************************
         CALL: clearUserMsg()
         TASK: Empty user message screen
         *****************************************************/
        void clearUserMsg()
        {
            textBoxError.Text = "";
            textBoxMsg.Text = "";
        }

        /******************************************************
         CALL: When clicking the Open button in the Menu
         TASK: Open a txt file from the directory.
         *****************************************************/
        private void Open_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".txt";
            ofd.Filter = "Text Document (.txt)|*.txt";

            if(ofd.ShowDialog() == true) {
                string filename = ofd.FileName;
                TextBox_Assembler.Focus();
                TextBox_Assembler.Text = File.ReadAllText(filename);
                userMsg("Open file " + filename + "\n");                
            }
            else
            {
                string filename = ofd.FileName;
                errorCode("Could not open file " + filename + "\n");
            }
        }

        /******************************************************
         CALL: When clicking the Save button in the menu.
         TASK: Saves the inputted assembler code as a .txt file.
         *****************************************************/
        private void Save_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = ".txt";
            sfd.AddExtension = true;      
     
            if(sfd.ShowDialog() == true) {
                File.WriteAllText(sfd.FileName, TextBox_Assembler.Text);
                String time = DateTime.Now.ToString();
                userMsg("Saved successfully " + time + "\n");
            }
            else
            {
                errorCode("Could not save the file\n");
            }
        }

        /******************************************************
         CALL: When clicking the Exit button in the Menu
         TASK: Gives the user a messageBox Yes/No to Exit.
         *****************************************************/
        private void Exit_Click(object sender, RoutedEventArgs e) {
            MessageBoxResult result = MessageBox.Show("Exit the application without saving?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) {
                Application.Current.Shutdown();
            }
        }
               

        /******************************************************
         CALL: When clicking the About button in the menu.
         TASK: Displays info about the devolopment.
         *****************************************************/
        private void About_Click(object sender, RoutedEventArgs e) {
            About aboutWin = new About();

            double mainLeft = Application.Current.MainWindow.Left;
            double mainTop = Application.Current.MainWindow.Top;

            aboutWin.Left = mainLeft + 60;
            aboutWin.Top = mainTop + 60;

            aboutWin.Show();
        }
       
        /******************************************************
         CALL: When clicking the pause button in the menu.
         TASK: Pauses the run through of the program and enables 
               input in the textboxes again.
         *****************************************************/
        private void Button_Pause_Click(object sender, RoutedEventArgs e) {
            TextBox textBoxMK = TextBox_MK;
            TextBox textBoxAssembler = TextBox_Assembler;
            _runTimer.Stop();
            textBoxMK.IsReadOnly = false;
            textBoxAssembler.IsReadOnly = false;
        }

        
        /******************************************************
         CALL: Clicking the step back button.
         TASK: Rolls back the program one step i.e. undo the 
               previous operation.
        *****************************************************/
        private void Button_StepBack_Click(object sender, RoutedEventArgs e) {
            if (_runTimer.IsEnabled) {
               
                errorCode("Error cannot do this while running the application\n");
                return;
            }

            if (_assemblerModel.undoStack().Count == 0) {
                return;
            }

            UndoStorage undoValues = _assemblerModel.undo();
            Bit12 currentAddr = _assemblerModel.getAddr(_assemblerModel.instructionPtr());
            Operations opr = Operations.LOAD;
            _assemblerModel.extractOperation(currentAddr.value(), out opr);

            // Mark current row
            markRow(getMMRowOfPosition(255 - _assemblerModel.instructionPtr()));

            // Uppdatera grafiskt minnet som ändrats
            byte index;
            if (_assemblerModel.addrIdxToUpdate(currentAddr, out index)) {
                if (opr == Operations.RETURN) {
                    index += 2;
                }

                MemoryRow row = getMMRowOfPosition(255 - index);
                row.ShowMemoryAdress(_assemblerModel.getAddr(index));

                if (index > 250) {
                    MemoryRow stackRow = getStackRowOfPosition(255 - index);                   
                    stackRow.ShowMemoryAdress(_assemblerModel.getAddr(index));
                }
            }

            ValueRow_WorkingRegister.ShowMemoryAdress(_assemblerModel.workingRegister());
            ValueRow_Output.ShowMemoryAdress(_assemblerModel.output());
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));           
        }


        /******************************************************
         CALL: Clicking one of the tabs.
         TASK: Toggles machine code textbox and assembler 
               textbox visibility. 
        *****************************************************/
        // Ganska ful kod, men vågar inte röra XAML-filen så att den inte förstör text alignment
        private void TabsToggle_event(object sender, RoutedEventArgs e) {
            if (Convert.ToBoolean(AssemblyTab.IsChecked)) {
                TextBox_Assembler.Visibility = Visibility.Visible;
                TextBox_MK.Visibility = Visibility.Hidden;
                Grid.SetColumn(TextBox_Assembler, 1);
                Grid.SetColumnSpan(TextBox_Assembler, 2);
                TextBox_Assembler.MinWidth = 400;

                AssemblyTab.FontWeight = FontWeights.Bold;
                MKTab.FontWeight = FontWeights.Normal;
                SplitTab.FontWeight = FontWeights.Normal;
            }

            else if(Convert.ToBoolean(MKTab.IsChecked)) {
                TextBox_Assembler.Visibility = Visibility.Hidden;
                TextBox_MK.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(TextBox_MK, 2);
                TextBox_MK.MinWidth = 400;

                MKTab.FontWeight = FontWeights.Bold;
                AssemblyTab.FontWeight = FontWeights.Normal;
                SplitTab.FontWeight = FontWeights.Normal;
            }

            else if (Convert.ToBoolean(SplitTab.IsChecked)) {
                TextBox_Assembler.Visibility = Visibility.Visible;
                TextBox_MK.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(TextBox_MK, 1);
                Grid.SetColumn(TextBox_Assembler, 2);
                Grid.SetColumnSpan(TextBox_Assembler, 1);
                TextBox_MK.MinWidth = 200;
                TextBox_Assembler.MinWidth = 200;

                SplitTab.FontWeight = FontWeights.Bold;
                AssemblyTab.FontWeight = FontWeights.Normal;
                MKTab.FontWeight = FontWeights.Normal;
            }
        }


        /******************************************************
         CALL: Clicking the drop down list to change skin.
         TASK: Changes the skin color (theme).
        *****************************************************/
        private void changeSkinEvent(object sender, RoutedEventArgs e) {
            ComboBoxItem item = sender as ComboBoxItem;

            if (!item.IsFocused)
                return;

            Skins selected;
            Enum.TryParse(item.Content.ToString(), out selected);
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
         CALL: Clicking the minus button.
         TASK: Decreases the value on input.
        *****************************************************/
        private void Button_Minus_Click(object sender, RoutedEventArgs e) {
            Bit12 input = _assemblerModel.input();
            input -= new Bit12(1);

            if (input == new Bit12(-1)) {
                _assemblerModel.setInput(new Bit12(4095));
            }

            else {
                _assemblerModel.setInput(input);
            }

            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
        }

        /******************************************************
         CALL: Clicking the plus button.
         TASK: Increases the value on input by one.
        *****************************************************/
        private void Button_Plus_Click(object sender, RoutedEventArgs e) {
            Bit12 input = _assemblerModel.input();
            input += new Bit12(1);

            if (input == new Bit12(4095)) {
                _assemblerModel.setInput(new Bit12(4095));
            }

            else {
                _assemblerModel.setInput(input);
            }

            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
        }

        
        private AssemblerModel _assemblerModel;
        private byte _previousLineCount;
        private int _previousInstructionPtr = -1; // TODO: Remove this. Temporary until we have stack for step back.

        private System.Windows.Threading.DispatcherTimer _runTimer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _inputTimerMK = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _inputTimerAssembly = new System.Windows.Threading.DispatcherTimer();
    }
}
