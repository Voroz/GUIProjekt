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

namespace GUIProjekt
{
    /// <summary>
    /// Interaction logic for MemoryRow.xaml
    /// </summary>
    public partial class MemoryRow : UserControl
    {
        private Color _colorZero = Color.FromArgb(255, 2, 132, 130);
        private Color _colorOne = Color.FromArgb(255, 128, 255, 0);

        public void MemoryColors(Color value, Color value2)
        {
            _colorZero = value;
            _colorOne = value2;                          
        }
        public MemoryRow()
        {
            InitializeComponent();
        }


        /******************************************************
         CALL: ShowMemoryAdress()
         TASK: Prints a row of ones and zeros in the memory.
         *****************************************************/
        public void ShowMemoryAdress(string str)
        {
            SolidColorBrush[] br = new SolidColorBrush[2];
            br[0] = new SolidColorBrush();
            br[1] = new SolidColorBrush();
            br[0].Color = _colorZero;
            br[1].Color = _colorOne;

            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            for (int ix = 0; ix < str.Length; ix++)
            {
                if (ix < memoryGrid.Children.Count)
                {
                    Grid cell = memoryGrid.Children[ix] as Grid;
                    Rectangle rect = cell.Children[0] as Rectangle;
                    Label lab = cell.Children[1] as Label;

                    int value;
                    bool ok = int.TryParse(str[ix].ToString(), out value);
                    if (ok && value >= 0 && value <= 1)
                    {
                        lab.Content = value.ToString();
                        rect.Fill = br[value];
                    }
                }
            }
        }


        /******************************************************
         CALL: ClearMemoryAdress();
         TASK: Clears a row in the memory and colors these 
               squares white.
         *****************************************************/
        public void ClearMemoryAdress()
        {
            Brush br = Brushes.White;
            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            for (int ix = 0; ix < 12; ix++)
            {
                if (ix < memoryGrid.Children.Count)
                {
                    Grid cell = memoryGrid.Children[ix] as Grid;
                    Rectangle rect = cell.Children[0] as Rectangle;
                    Label lab = cell.Children[1] as Label;

                    lab.Content = "";
                    rect.Fill = br;
                }
            }
        }


        /******************************************************
         CALL: ShowMemoryRowNumber(byte val);
         TASK: Creates this row's numbering.
         *****************************************************/
        public void ShowMemoryRowNumber(byte val)
        {
            UniformGrid rowNumbers = this.AdressNumber as UniformGrid;

            Grid cell = rowNumbers.Children[0] as Grid;
            Rectangle rect = cell.Children[0] as Rectangle;
            Label lab = cell.Children[1] as Label;
            
            lab.Content = val.ToString();
        }
    }
}
