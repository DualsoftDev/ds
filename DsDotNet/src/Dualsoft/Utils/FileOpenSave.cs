using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace DSModeler;

[SupportedOSPlatform("windows")]
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
            openFileDialog.Multiselect = false;  //단일 파일로 동일 폴더에 경로에 있는것을 Active로 자동 해석
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return null;

            var directory = Path.GetDirectoryName(openFileDialog.FileNames.First());
            DSRegistry.SetValue(K.LastPath, directory);

            files = openFileDialog.FileNames;
        };

        return files;
    }
}
