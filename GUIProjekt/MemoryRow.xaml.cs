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
    /// 

    public class RowColor {
        public RowColor(Color backgroundZero
            , Color backgroundOne) {

            _backgroundZero = backgroundZero;
            _backgroundOne = backgroundOne;
        }

        public Color _backgroundZero;
        public Color _backgroundOne;
    }
    public partial class MemoryRow : UserControl
    {
        public MemoryRow() {
            InitializeComponent();
            _rowColor = new RowColor(Color.FromArgb(255, 2, 132, 130)
                , Color.FromArgb(255, 128, 255, 0)
                );
        }

        public void setColor(RowColor newColor)
        {
            _rowColor = newColor;
            updateColor();
        }
        private void updateColor() {
            SolidColorBrush[] br = new SolidColorBrush[2];
            br[0] = new SolidColorBrush();
            br[1] = new SolidColorBrush();
            br[0].Color = _rowColor._backgroundZero;
            br[1].Color = _rowColor._backgroundOne;

            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            for (int i = 0; i < memoryGrid.Children.Count; i++) {
                Grid cell = memoryGrid.Children[i] as Grid;
                Rectangle rect = cell.Children[0] as Rectangle;
                Label lab = cell.Children[1] as Label;

                if (lab.Content.ToString() == "0" || lab.Content.ToString() == "1") {
                    int val = int.Parse(lab.Content.ToString());
                    rect.Fill = br[val];
                }
            }
        }
        public RowColor color() {
            return _rowColor;
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
            br[0].Color = _rowColor._backgroundZero;
            br[1].Color = _rowColor._backgroundOne;

            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            if (string.IsNullOrWhiteSpace(str))
            {
                for (int i = 0; i < memoryGrid.Children.Count; i++)
                {
                    Grid cell = memoryGrid.Children[i] as Grid;
                    Rectangle rect = cell.Children[0] as Rectangle;
                    Label lab = cell.Children[1] as Label;

                    lab.Content = "0";
                    rect.Fill = br[0];
                }
            }

            for (int i = 0; i < str.Length && i < memoryGrid.Children.Count; i++)
            {
                Grid cell = memoryGrid.Children[i] as Grid;
                Rectangle rect = cell.Children[0] as Rectangle;
                Label lab = cell.Children[1] as Label;

                    

                if (str[i] == '0' || str[i] == '1')
                {
                    lab.Content = str[i];
                    rect.Fill = br[int.Parse(str[i].ToString())];
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
            Label lab = cell.Children[0] as Label;
            
            lab.Content = val.ToString();
        }

        private RowColor _rowColor;
    }
}
