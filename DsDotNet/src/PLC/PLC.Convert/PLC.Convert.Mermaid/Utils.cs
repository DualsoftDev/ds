using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PLC.Convert.Mermaid
{
    
    public static class FileOpenSave
    {
        /// <summary>
        /// Opens a file dialog to select files.
        /// </summary>
        /// <returns>An array of file paths of the selected files or null if no file is selected.</returns>
        public static string[] OpenFiles()
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter =
            "xg50000 file (*.xgwx)|*.xgwx|" +
            "All files (*.*)|*.*";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            string file = openFileDialog.FileNames.First();

            return openFileDialog.FileNames;
        }

        /// <summary>
        /// Sets the last opened file paths in the registry.
        /// </summary>
        /// <param name="filePath">The file paths to set as the last opened files.</param>


        /// <summary>
        /// Generates a new file path based on the given path and the current timestamp.
        /// </summary>
        /// <param name="path">The original file path.</param>
        /// <returns>A new file path with a timestamp.</returns>
        public static string GetNewPath(string path)
        {
            string newPath = Path.Combine(Path.GetDirectoryName(path)
                        , string.Join("_", Path.GetFileNameWithoutExtension(path)));

            string extension = Path.GetExtension(path);
            string excelName = Path.GetFileNameWithoutExtension(newPath) + $"_{DateTime.Now:yyMMdd(HH-mm-ss)}.{extension}";
            string excelDirectory = Path.Combine(Path.GetDirectoryName(newPath), Path.GetFileNameWithoutExtension(excelName));
            _ = Directory.CreateDirectory(excelDirectory);

            return Path.Combine(excelDirectory, excelName);
        }
    }
  
    
}
