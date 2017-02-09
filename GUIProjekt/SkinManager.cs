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
        Visual,
        Dark,
        Secret
    }

    class SkinManager
    {

        /******************************************************
         CALL: ResourceDictionary selectedDictionary = GetSkin(Skins);
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
                case Skins.Visual:
                     return "Skins/VisualSkin.xaml";
                case Skins.Dark:
                     return "Skins/DarkSkin.xaml";
                case Skins.Secret:
                     return "Skins/SecretSkin.xaml";
            }
        }        
    }
}
