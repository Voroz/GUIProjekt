using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GUIProjekt
{
    public enum Skins {
        Default,
        Orange,
        Visual,
        DefaultAlt,
        Red,
        Blue
    }

    class SkinManager
    {
        /******************************************************
         CALL: When choosing a skin.
         TASK: Loads the XAML file for the wanted skin.
         *****************************************************/
        public static ResourceDictionary GetSkin(Skins skin) 
        {
            return Application.LoadComponent(new Uri(getPath(skin), UriKind.Relative)) as ResourceDictionary;
        }

        /******************************************************
         CALL: string path = getPath(Skins);
         TASK: Returns the path to the parameter skin.
         *****************************************************/
        private static string getPath(Skins skin) 
        {
            switch (skin) {
                default:
                case Skins.Default:
                    return "Skins/DefaultSkin.xaml";
                case Skins.Orange:
                    return "Skins/OrangeSkin.xaml";
                case Skins.Visual:
                     return "Skins/BlueSkin.xaml";
                case Skins.DefaultAlt:
                     return "Skins/DefaultAlt.xaml";
                case Skins.Red:
                     return "Skins/RedSkin.xaml";
                case Skins.Blue:
                     return "Skins/FBSkin.xaml";
            }
        }
    }
}
