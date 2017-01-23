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
            br[0] = new SolidColorBrush(Colors.Aqua);
            br[1] = new SolidColorBrush(Colors.Red);
            /*br[0].Color = _colorZero;
            br[1].Color = _colorOne;*/

            UniformGrid valueRow = this.TwelveSquareRow as UniformGrid;

            for (int ix = 0; ix < str.Length; ix++)
            {
                if (ix < valueRow.Children.Count)
                {
                    Grid cell = valueRow.Children[ix] as Grid;
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
    }
}
