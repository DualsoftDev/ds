using Dual.Common.Base.CS;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace Dual.Common.Core
{
    /*
     * Use System.IO.Path for
     * Path.GetDirectoryName(), Path.GetExtension(), ...
     */


    /// <summary>
    /// File 관련 helper class
    /// <para/> - Use System.IO.Path for Path.GetDirectoryName(), Path.GetExtension(), ...
    /// <para/> - Use new FileInfo(path).Exist for existence check.
    /// </summary>
    public class FileSpec
    {
        public static string NormalizePath(string path)
        {
            var splits = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Join(Path.DirectorySeparatorChar.ToString(), splits);
        }
        public static bool FileContentsAreEqual(string path1, string path2)
        {
            var f1 = new FileInfo(path1);
            var f2 = new FileInfo(path2);
            if (!f1.Exists || !f2.Exists || f1.Length != f2.Length)
                return false;

            return System.IO.File.ReadAllBytes(path1).SequenceEqual(System.IO.File.ReadAllBytes(path2));
        }


        static public string Absolute2Relative(string path, string baseDir) { return Absolute2Relative(path, baseDir, false, false); }
        static public string Absolute2Relative(string path, string baseDir, bool bUseBackslash/*=false*/, bool urlEncode/*=false*/)
        {
            Contract.Requires(!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(baseDir));
            Contract.Requires(Path.IsPathRooted(path) && Path.IsPathRooted(baseDir));

            if (!baseDir.EndsWith("\\") && !baseDir.EndsWith("/"))
                baseDir += "\\";

            Uri uriBase = new Uri(baseDir);
            Uri uriPath = new Uri(path);

            string strRelative = uriBase.MakeRelativeUri(uriPath).ToString();

            var strRelativeEncoded = bUseBackslash ? strRelative.Replace('/', '\\') : strRelative;
            return urlEncode ? strRelativeEncoded : Uri.UnescapeDataString(strRelativeEncoded);
        }

        static public string Relative2Absolute(string path, string baseDir)
        {
            if (Path.IsPathRooted(path))
                return path;

            if (baseDir.IsNullOrEmpty() && (path.IsNullOrEmpty() || path == "."))
                baseDir = ".";

            return Path.GetFullPath(baseDir + '\\' + path);
        }

        static public string[] SplitPathString(string path)
        {
            return path.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
        }

        static public string FindFirstExistingFileOnPaths(string cwd, string file, IEnumerable<string> paths)
        {
            var pr = FindFirstExistingFileOnPaths2(cwd, file, paths);
            return pr.HasValue ? pr.Value.Value : null;
        }

        /// <summary>
        /// cwd 기준으로 paths 들을 상대 path 로 결합하여, file 이 존재하는 지 검사한다.
        /// 존재하면 file 이 존재하는 dir 와 full path 를 pair 로 반환한다.
        /// </summary>
        /// <param name="cwd">base dir</param>
        /// <param name="file">찾을 파일 명. Not full path.  just file name</param>
        /// <param name="paths">cwd 에서 상대경로로 지정하는 검색 directories</param>
        /// <returns></returns>
        public static Nullable<KeyValuePair<string, string>> FindFirstExistingFileOnPaths2(string cwd, string file, IEnumerable<string> paths)
        {
            using (new CwdChanger(cwd))
            {
                if (File.Exists(file))
                    return new KeyValuePair<string, string>(cwd, Path.GetFullPath(Path.Combine(cwd, file)));
                foreach (var path in paths)
                {
                    var fullpath = Path.GetFullPath(Path.Combine(path, file));
                    if (File.Exists(fullpath))
                        return new KeyValuePair<string, string>(path, fullpath);
                }
            }

            return null;
        }

        /// <summary>
        /// targetDir 을 baseDir 에서 본 상대 path 로 반환한다.
        /// <para/> - ChangeReferenceFolder("./abc", "./ABC") => ../abc
        /// <para/> - ChangeReferenceFolder("./abc", "./ABC/DEF") => ../../abc
        /// <para/> - ChangeReferenceFolder("./abc/def", "./ABC") => ../abc/def
        /// </summary>
        /// <param name="targetDir"></param>
        /// <param name="baseDir"></param>
        /// <returns></returns>
        public static string ChangeReferenceFolder(string targetDir, string baseDir)
        {
            if (Path.IsPathRooted(targetDir))
                return targetDir;

            if ( Path.IsPathRooted(baseDir))
                throw new InvalidOperationException();

            var pseudoPrefix = "x:\\";
            var pseudoResult = Absolute2Relative(Path.Combine(pseudoPrefix, targetDir), Path.Combine(pseudoPrefix, baseDir));
            return pseudoResult;
        }

        /// <summary>
        /// Path.ChangeExtension() 는 extension 만 바꾸는 것이 아니라, 경로에 포함된 space 를 %20 등으로 encoding 한다.  이를 원복하기 위한 함수.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newExtension"></param>
        /// <returns></returns>
        public static string ChangeExtension(string path, string newExtension)
        {
            return Uri.UnescapeDataString(Path.ChangeExtension(path, newExtension));
        }



        public static string[] CollctFileNames(string path, string searchPattern, bool recursive=true)
        {
            return Directory.GetFiles(path, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// sourceDirectory 하부의 folder 구조를 targetDirectory 하부에 복사한다.
        public static void CopyFolderStructureRecursively(string sourceDirectory, string targetDirectory)
        {
            // https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
            Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories)
                .Iter(sd =>
                {
                    var td = sd.Replace(sourceDirectory, targetDirectory);
                    Directory.CreateDirectory(td);
                });
        }

        /// sourceDirectory 하부 전체를 targetDirectory 하부에 복사한다.
        public static void CopyRecursively(string sourceDirectory, string targetDirectory)
        {
            CopyFolderStructureRecursively(sourceDirectory, targetDirectory);

            Directory.GetFiles(sourceDirectory)
                .Iter(sf =>
                {
                    var tf = sf.Replace(sourceDirectory, targetDirectory);
                    Directory.CreateDirectory(Path.GetDirectoryName(tf));
                    File.Delete(tf);
                    File.Copy(sf, tf);
                });
        }
    }

    public class CwdChanger : IDisposable
    {
        protected string m_strBackupDirectory = null;

        public CwdChanger() : this(null) { }
        public CwdChanger(string strTargetDirectory/*=null*/)
        {
            m_strBackupDirectory = Directory.GetCurrentDirectory();
            if (String.IsNullOrEmpty(strTargetDirectory))
                throw new Exception($"Directory {strTargetDirectory} not found.");
            else
                Directory.SetCurrentDirectory(strTargetDirectory);
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(m_strBackupDirectory);
        }
    }


    public class TextFileParser
    {
        // http://schotime.net/blog/index.php/2008/03/18/importing-data-files-with-linq/
        public static IEnumerable<string> ReadLinesFromFile(string filename)
        {
            if (!new FileInfo(filename).Exists)
                yield break;

            using (StreamReader reader = new StreamReader(filename))
            {
                while (true)
                {
                    string s = reader.ReadLine();
                    if (s == null)
                        break;
                    yield return s;
                }
            }
        }
    }
}
