using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Dualsoft
{

    public static class FileOpenSave
    {
        public static string[] OpenFiles()
        {
            string[] files;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory =
                    DSRegistry.GetValue(K.LastPath) == null ?
                    Global.DefaultFolder : DSRegistry.GetValue(K.LastPath).ToString();

                openFileDialog.Filter =
                "PPTX files (*.pptx)|*.pptx|" +
                "All files (*.*)|*.*";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return null;

                var directory = Path.GetDirectoryName(openFileDialog.FileNames.First());
                DSRegistry.SetValue(K.LastPath, directory);

                files = openFileDialog.FileNames;
            };

            return files;
        }
    }
}
