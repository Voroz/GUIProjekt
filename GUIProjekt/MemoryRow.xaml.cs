using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using GUIProjekt;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace GUIProjekt
{
    /// <summary>
    /// Interaction logic for MemoryRow.xaml
    /// </summary>
    /// 

    public partial class MemoryRow : UserControl
    {
        public MemoryRow() {
            InitializeComponent();
        }

        public void ChangeSkin(ResourceDictionary selectedDictionary) {
            this.Resources.MergedDictionaries.Add(selectedDictionary);
        }

        /******************************************************
         CALL: ShowMemoryAdress();
         TASK: Prints a row of ones and zeros in the memory.
         *****************************************************/
        public void ShowMemoryAdress(Bit12 val) {
            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            string str = Convert.ToString(val.value(), 2).PadLeft(12, '0');

            if (str.Length > 12) {
                str = str.Substring(str.Length - 12);
            }

            for (int i = 0; i < str.Length && i < memoryGrid.Children.Count; i++) {
                Label lab = memoryGrid.Children[i] as Label;
                if (str[i] == '0' || str[i] == '1') {
                    lab.Content = str[i];
                }
            }
        }

        /******************************************************
         CALL: ShowMemoryRowNumber(byte val);
         TASK: Creates this row's numbering.
         *****************************************************/
        public void ShowMemoryRowNumber(byte val) {
            UniformGrid rowNumbers = this.AdressNumber as UniformGrid;
            Label lab = rowNumbers.Children[0] as Label;
            lab.Content = val.ToString();
        }

        /******************************************************
         CALL: RemoveChildElements();
         TASK: Removes certain elements of the Memory Row.
         NOTE: Only used by the instruction pointer to reduce 
               the number of bits to 8. Made to avoid having to
               make an entirely new control class just for this.
         *****************************************************/
        public void HideChildElements() {
            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            for (int i = 0; i < 4; i++) {
                memoryGrid.Children[i].Visibility = Visibility.Hidden;
            }
        }
    }
}
