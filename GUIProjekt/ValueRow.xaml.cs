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

// TODO: Byt ut denna klass mot vår MemoryRow klass och sätt radnummer som hidden bara.

namespace GUIProjekt
{
    /// <summary>
    /// Interaction logic for ValueRow.xaml
    /// </summary>
    public partial class ValueRow : UserControl
    {
        public ValueRow()
        {
            InitializeComponent();
        }

        /******************************************************
         CALL: ShowValue();
         TASK: Fills one of the value rows with ones and zeros.
         *****************************************************/

        public void ShowValue(string str)
        {
            SolidColorBrush[] br = new SolidColorBrush[2];
            br[0] = new SolidColorBrush(Color.FromArgb(255, 2, 132, 130));
            br[1] = new SolidColorBrush(Color.FromArgb(255, 128, 255, 0));

            UniformGrid memoryGrid = this.TwelveSquareRow as UniformGrid;

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
    }
}
