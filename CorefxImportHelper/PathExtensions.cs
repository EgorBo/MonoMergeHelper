using System;
using System.IO;
using System.Linq;

namespace CorefxImportHelper
{
    public static class PathExtensions
    {
        public static string ToOsPath(this string path) => path
            .Replace("/", Path.DirectorySeparatorChar.ToString())
            .Replace("\\", Path.DirectorySeparatorChar.ToString());

        public static string ToUnixPath(this string path) => path
            .Replace("\\", "/");

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                folder += Path.DirectorySeparatorChar;
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string ShortUnixPathToLong(this string path, int maxLength)
        {
            try
            {
                if (path.Contains("\\"))
                    return path; //should not happen

                if (path.Length <= maxLength)
                    return path;

                var parts = path.Split('/').ToList();
                string shortPath = path;

                int indexCutFrom = path.StartsWith("../../") ? 5 : 2;

                while (shortPath.Length > maxLength && parts.Count > indexCutFrom)
                {
                    parts.RemoveAt(indexCutFrom);
                    shortPath = string.Join("/", parts);
                }
                parts.Insert(indexCutFrom, "[ .... ]");
                shortPath = string.Join("/", parts);
                return shortPath;
            }
            catch (Exception)
            {
                return path;
            }
        }

        public static string GetMonoRootPath(this string path)
        {
            while (!Directory.Exists(Path.Combine(path, "external")))
                path = Path.GetDirectoryName(path);
            return path;
        }
    }
}
