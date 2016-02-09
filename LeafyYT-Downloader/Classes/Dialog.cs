// -----------------------------------------------------------
// This program is private software, based on C# source code.
// To sell or change credits of this software is forbidden,
// except if someone approves it from the LeafyCoding INC. team.
// -----------------------------------------------------------
// Copyrights (c) 2016 LeafyYT-Downloader INC. All rights reserved.
// -----------------------------------------------------------

#region

using System;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

#endregion

namespace LeafyYT_Downloader.Classes
{
    internal class Dialog
    {
        internal static async void ShowMessageDialog(string messageTitle, string message)
        {
            var m = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                AnimateShow = true,
                AnimateHide = true,
                SuppressDefaultResources = true,
                CustomResourceDictionary =
                    new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Resources/Themes/Dialogs.xaml")
                    }
            };
            await
                ((MainWindow) Application.Current.MainWindow).ShowMessageAsync(messageTitle,
                    message,
                    MessageDialogStyle.Affirmative, m);
        }
    }
}