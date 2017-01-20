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
        Blue
    }

    class SkinManager
    {
        public static ResourceDictionary GetSkin(Skins skin)
        {
            return Application.LoadComponent(new Uri(getPath(skin), UriKind.Relative)) as ResourceDictionary;
        }

        private static string getPath(Skins skin)
        {
            switch (skin)
            {
                default:
                case Skins.Default:
                    return "Skins/DefaultSkin.xaml";
                case Skins.Orange:
                    return "Skins/OrangeSkin.xaml";
                case Skins.Blue:
                     return "Skins/BlueSkin.xaml";
            }
        }
    }
}
