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
        public MemoryRow()
        {
            InitializeComponent();
        }

        public void ShowMemoryAdress(string str)
        {
            Brush[] br = new Brush[2] {Brushes.Aqua,Brushes.Orange};
            UniformGrid memoryGrid = this.BinaryMemoryAdress as UniformGrid;

            for(int ix=0;ix<str.Length;ix++)
            {
                if(ix<memoryGrid.Children.Count)
                {
                    Grid cell = memoryGrid.Children[ix] as Grid;
                    Rectangle rect = cell.Children[0] as Rectangle;
                    Label lab = cell.Children[1] as Label;

                    int value;
                    bool ok = int.TryParse(str[ix].ToString(), out value);
                    if(ok && value >=0 && value <=1)
                    {
                        lab.Content = value.ToString();
                        rect.Fill = br[value];
                    }
                }
            }
        }

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
    }
}
