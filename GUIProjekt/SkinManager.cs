using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GUIProjekt
{
    /**************************************************************
     * Anrop: SkinManager.SetSkin(AppSkin DefaultLayout);
     * Uppgift: En Klass som ska kunna byta Layout för programmet
     *************************************************************/
    ////public enum AppSkin
    ////{
    ////    Default,
    ////    Orange
    ////}
    ////public static class SkinManager
    ////{
    ////    public static AppSkin CurrentThemeType { get; private set; }

    ////    public static void SetSkin(AppSkin appSkin)
    ////    {
    ////        CurrentThemeType = appSkin;
    ////        Application.Current.Resources.MergedDictionaries[0].MergedDictionaries.Clear();
    ////        var newDictionary = new ResourceDictionary();
    ////        switch (appSkin)
    ////        {
    ////            case AppSkin.Default:
    ////                newDictionary.Source = new Uri("C://Mäki/Source/Repos/GUIProjekt/GUIProjekt/Skins/DefaultSkin.xaml");
    ////               break;
    ////            case AppSkin.Orange:
    ////                newDictionary.Source = new Uri("pack://application:,,,/GUIProjekt;component/Skins/OrangeSkin.xaml");
    ////                break;
    ////        }
    ////        Application.Current.Resources.MergedDictionaries[0].MergedDictionaries.Add(newDictionary);
    ////    }
    ////}
}
