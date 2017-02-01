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

// TODO: Fixa problem efter step back att programmet ej läser in minnet från modellen
// utan istället från textbox (updateGUIMemory() omskrivning).
// Se även till att uppdatera grafiska minnet efter step back (med updateGUIMemory() funktionen).
// Gör också så att när man startar / steppar programmet igen så läser programmet om raden vi är på för tillfället
// från textrutan och in i modellen.

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
            _inputTimerAssembly.Tick += OnInputTimerAssemblyElapsed;
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, Constants.SlowExecutionDelay);
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

            for (int i = from; i <= to; i++) {
                string assStr = "";
                if (i < TextBox_Assembler.LineCount) {
                    assStr = TextBox_Assembler.GetLineText(i);
                }

                char[] trimChars = new char[2] { '\r', '\n' };
                assStr = assStr.TrimEnd(trimChars);

                Bit12 val = new Bit12(0);
                if (!string.IsNullOrWhiteSpace(assStr)) {
                    _assemblerModel.assemblyToMachine(assStr, out val);
                }

                if (i > 250) {
                    MemoryRow stackRow = getStackRowOfPosition(255 - i);
                    stackRow.ShowMemoryAdress(val);
                }

                MemoryRow rad = getMMRowOfPosition(255 - i);
                rad.ShowMemoryAdress(val);
            }
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

                Bit12 val = new Bit12(0);
                if (!_assemblerModel.assemblyToMachine(str, out val)) {
                    errorCode("Syntax error row "+ i +" " + str +" not a valid command");
                    return false;
                }             
            }
            
            return true;
        }

        /******************************************************
        CALL: When writing in the machine code section.
        TASK: Updates the assembler section.
       *****************************************************/
        private void TextBox_MK_TextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Intellisens stuff
            // (use struct from checkSyntax functions with error code and line number to create highlighting and error information for user)

            //TextBox mkBox = sender as TextBox;
            //TextBox assemblerBox = TextBox_Assembler;

            //if (!mkBox.IsFocused || mkBox.IsReadOnly)
            //{
            //    return;
            //}

            //_inputTimerMK.Stop();
            //_inputTimerMK.Start();
            //assemblerBox.IsReadOnly = true;
        }

        /******************************************************
         CALL: When writing in the assembler section.
         TASK: Updates the machine code section and the memory.
        *****************************************************/ 
        private void TextBox_Assembler_TextChanged(object sender, TextChangedEventArgs e) {
            TextBox assemblerBox = sender as TextBox;
            
            if (!assemblerBox.IsFocused || assemblerBox.IsReadOnly) {
                return;
            }
            
            _inputTimerAssembly.Stop();
            _inputTimerAssembly.Start();
        }

        private void OnInputTimerAssemblyElapsed(object source, EventArgs e) {
            TextBox assemblerBox = TextBox_Assembler;
            
            _inputTimerAssembly.Stop();            

            if (assemblerBox.LineCount > 256) {
                errorCode("Error: Exceeded maximum lines in assembler editor.");
                return;
            }

            storeLabels();

            for (int i = 0; i < assemblerBox.LineCount; i++) {
                string assemblyStr = assemblerBox.GetLineText(i);
                Bit12 bits = new Bit12(0);

                if (!string.IsNullOrWhiteSpace(assemblyStr)) {
                    char[] trimChars = new char[2] { '\r', '\n' };
                    _assemblerModel.assemblyToMachine(assemblyStr.TrimEnd(trimChars), out bits);
                }

                MemoryRow row = getMMRowOfPosition(255 - i);

                row.ShowMemoryAdress(bits);
            }

            // Update deleted lines memory aswell
            int nrOfDeletedLines = _previousLineCount - assemblerBox.LineCount;
            if (nrOfDeletedLines >= 0) {
                updateGUIMemory((byte)(assemblerBox.LineCount - 1), (byte)(assemblerBox.LineCount - 1 + nrOfDeletedLines));
            }
            updateGUIMemory((byte)0, (byte)(assemblerBox.LineCount - 1));

            _previousLineCount = (byte)assemblerBox.LineCount;
        }


        void storeLabels() 
        {
            _assemblerModel.clearLabels();
            for (byte i = 0; i < TextBox_Assembler.LineCount; i++)
            {
                string label;
                if (_assemblerModel.containsLabel(TextBox_Assembler.GetLineText(i), out label) == LabelStatus.Success)
                {
                    _assemblerModel.addLabel(label, i);
                }
            }
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

            if (opr == Operations.RETURN && _assemblerModel.stack().size() == 0) {
                _runTimer.Stop();
                TextBox_Assembler.IsReadOnly = false;
                errorCode("Error: Attempted Return on an empty stack");
                return;
            }

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
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));


            // TODO: Lamptest
           
                short lightup = _assemblerModel.extractValFromBits((byte)(0), (byte)(0), _assemblerModel.output().value());
                if (lightup > 0)
                    lightOn();
                else
                    lightOff();
            
            ////////////////////////////////////////////////////            
        }

        //TODO Test för lampan
        void lightOn()
        {
            
            var uriSource = new Uri(@"/GUIProjekt;component/images/bulbon.png", UriKind.Relative);
            
            bulb.Source = new BitmapImage(uriSource);
        }
        //TODO Test för lampan
        void lightOff()
        {
            
            var uriSource = new Uri(@"/GUIProjekt;component/images/bulboff.png", UriKind.Relative);
           
            bulb.Source = new BitmapImage(uriSource);
        }
        
        bool assemblyTextToModel(TextBox textBoxAssembler) {

            storeLabels();

            if (!checkSyntaxAssemblyTextBox(textBoxAssembler))
            {
                return false;
            }
            
            for (byte i = 0; i < textBoxAssembler.LineCount; i++)
            {
                char[] trimChars = new char[2] { '\r', '\n' };
                string str = textBoxAssembler.GetLineText(i).TrimEnd(trimChars);
                Bit12 bits = new Bit12(0);

                bool success = _assemblerModel.stringToMachine(str, out bits);
                Debug.Assert(success);
                
                _assemblerModel.setAddr(i, bits);
            }
            return true;
        }

        bool InitProgramStart() {
            if (_runTimer.IsEnabled || _inputTimerAssembly.IsEnabled) {
                return false;
            }

            if (!assemblyTextToModel(TextBox_Assembler)) {
                return false;
            }

            TextBox_Assembler.IsReadOnly = true;

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
            clearUserMsg();
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
            clearUserMsg();
            programTick();

            TextBox_Assembler.IsReadOnly = false;
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


                lightOff();
            

            TextBox_Assembler.IsReadOnly = false;

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
            TextBlock_MessageBox.Inlines.Add(new Run(errorMsg +"\n") { Foreground = Brushes.Red });
            ScrollViewer_MessageBox.ScrollToEnd();
        }

        /******************************************************
         CALL: userMsg("I want to display this to the user");
         TASK: displays msg on screen in TextBoxMsg
         *****************************************************/
        void userMsg(String userMsg)
        {            
            TextBlock_MessageBox.Inlines.Add(new Run(userMsg + "\n") { Foreground = Brushes.Blue });
            ScrollViewer_MessageBox.ScrollToEnd();
        }

        /******************************************************
         CALL: clearUserMsg()
         TASK: Empty user message screen
         *****************************************************/
        void clearUserMsg()
        {
            TextBlock_MessageBox.Text = "";
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
                userMsg("Open file " + filename);                
            }
            else
            {
                string filename = ofd.FileName;
                errorCode("Could not open file " + filename);
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
                userMsg("Saved successfully " + time);
            }
            else
            {
                errorCode("Could not save the file");
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
            _runTimer.Stop();
            TextBox_Assembler.IsReadOnly = false;
        }

        
        /******************************************************
         CALL: Clicking the step back button.
         TASK: Rolls back the program one step i.e. undo the 
               previous operation.
        *****************************************************/
        private void Button_StepBack_Click(object sender, RoutedEventArgs e) {
            if (_runTimer.IsEnabled) {
               
                errorCode("Error cannot do this while running the application");
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
            //TODO testade att tända lampan om output är över 0
           
                short lightup = _assemblerModel.extractValFromBits((byte)(0), (byte)(0), _assemblerModel.output().value());
                if (lightup > 0)
                    lightOn();
                else
                    lightOff();
            
            ////////////////////////////////////////////////////
            ValueRow_InstructionPointer.ShowMemoryAdress(new Bit12(_assemblerModel.instructionPtr()));           
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

        private void Slider_Input_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            _assemblerModel.setInput(new Bit12((short)slider.Value));
            ValueRow_Input.ShowMemoryAdress(_assemblerModel.input());
        }

        private void Button_FastForward_Checked(object sender, RoutedEventArgs e)
        {
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, Constants.FastExecutionDelay);
        }

        private void Button_FastForward_Unchecked(object sender, RoutedEventArgs e)
        {
            _runTimer.Interval = new TimeSpan(0, 0, 0, 0, Constants.SlowExecutionDelay);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;

           
                Default.IsChecked = false;
                Orange.IsChecked = false;
                Visual.IsChecked = false;
                item.IsChecked = true;
                
            

            Skins selected;
            Enum.TryParse(item.Header.ToString(), out selected);
            
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
        private byte _previousLineCount;
        private int _previousInstructionPtr = -1; // TODO: Remove this. Temporary until we have stack for step back.

        private System.Windows.Threading.DispatcherTimer _runTimer = new System.Windows.Threading.DispatcherTimer();
        private System.Windows.Threading.DispatcherTimer _inputTimerAssembly = new System.Windows.Threading.DispatcherTimer();

        private void Assembler_Click(object sender, RoutedEventArgs e)
        {
            if (Assembler.IsChecked)
                return;
            Assembler.IsChecked = true;
            MachineCode.IsChecked = false;
        }

        private void MachineCode_Click(object sender, RoutedEventArgs e)
        {
            if (MachineCode.IsChecked)
                return;
            MachineCode.IsChecked = true;
            Assembler.IsChecked = false;
        }
    }
}
