namespace DSModeler.Utils;

[SupportedOSPlatform("windows")]
public static class FileOpenSave
{
    public static string[] OpenFiles()
    {
        string[] files;
        using (OpenFileDialog openFileDialog = new())
        {
            openFileDialog.InitialDirectory =
                DSRegistry.GetValue(RegKey.LastPath) == null ?
                Global.DefaultFolder : DSRegistry.GetValue(RegKey.LastPath).ToString();

            openFileDialog.Filter =
            "PPTX files (*.pptx)|*.pptx|" +
            "All files (*.*)|*.*";
            openFileDialog.Multiselect = false;  //단일 파일로 동일 폴더에 경로에 있는것을 Active로 자동 해석
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            string directory = Path.GetDirectoryName(openFileDialog.FileNames.First());
            DSRegistry.SetValue(RegKey.LastPath, directory);

            files = openFileDialog.FileNames;
        };

        return files;
    }
}
