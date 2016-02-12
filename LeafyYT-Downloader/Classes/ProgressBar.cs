// -----------------------------------------------------------
// This program is private software, based on C# source code.
// To sell or change credits of this software is forbidden,
// except if someone approves it from the LeafyCoding INC. team.
// -----------------------------------------------------------
// Copyrights (c) 2016 LeafyYT-Downloader INC. All rights reserved.
// -----------------------------------------------------------

#region

using System;
using System.Text;

#endregion

namespace LeafyYT_Downloader.Classes
{
    internal static class ProgressBar
    {
        private static readonly StringBuilder progressBar = new StringBuilder();

        internal static string Update(double percent)
        {
            var progress = Math.Abs(percent) > 0 ? (int) percent : 0;

            progressBar.Length = 0;
            progressBar.Append("[");

            for (double i = 0; i < 33; i++)
            {
                progressBar.Append(i*3.1 < progress ? "#" : "--");
            }

            progressBar.Append($"] {progress}%");

            return progressBar.ToString();
        }
    }
}