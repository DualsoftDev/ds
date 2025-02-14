using Dual.Common.Base.CS;

using System;
using System.IO;

namespace Dual.Common.Core
{
    public class TempFile : IDisposable
    {
        public string Name { get; private set; }

        /// <summary>
        /// Dispose 시 자동 삭제 되는 temp file name 을 생성한다.
        /// e.g ext = ".txt"
        /// </summary>
        /// <param name="ext"></param>
        public TempFile(string ext = null)
        {
            //try
            //{
                // https://stackoverflow.com/questions/18350699/system-io-ioexception-the-file-exists-when-using-system-io-path-gettempfilena
                Name = CreateTempFileName(ext);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Failed to create tempfile: {ex.Message}");
            //}
        }

        public static string CreateTempFileName(string ext = null)
        {
            var tmp = Guid.NewGuid().ToString();
            if (ext.NonNullAny())
                tmp += ext;
            return Path.Combine(Path.GetTempPath(), tmp);
        }

        public void Dispose()
        {
            File.Delete(Name);
        }
    }

    public class TempFolder : IDisposable
    {
        bool _removeFolderOnDispose;
        public string Dir { get; private set; }
        public TempFolder(bool removeFolderOnDispose = true)
        {
            _removeFolderOnDispose = removeFolderOnDispose;
            // https://stackoverflow.com/questions/18350699/system-io-ioexception-the-file-exists-when-using-system-io-path-gettempfilena
            Dir = CreateTempFolder();

        }
        public static string CreateTempFolder()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            return dir;
        }
        public void Dispose()
        {
            if (_removeFolderOnDispose)
                Directory.Delete(Dir, true);
        }

    }

    public class TempFolderFile : TempFolder
    {
        public string FilePath { get; private set; }
        public string FileName { get; private set; }

        /// <summary>
        /// Dispose 시 자동 삭제 되는 temp file name 을 생성한다.
        /// e.g ext = ".txt"
        /// </summary>
        /// <param name="ext"></param>
        public TempFolderFile(string fileName, bool removeFolderOnDispose=true)
            : base(removeFolderOnDispose)
        {
            FileName = fileName;
            FilePath = Path.Combine(Dir, fileName);
        }
    }

}
