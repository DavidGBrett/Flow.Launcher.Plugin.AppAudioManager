using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class UWPResourceResolver
    {
        public static List<string> FindAllVariants(string manifestPath)
        {
            string directory = Path.GetDirectoryName(manifestPath);
            if (!Directory.Exists(directory)) return new List<string>();
            
            string baseName = Path.GetFileNameWithoutExtension(manifestPath);
            string extension = Path.GetExtension(manifestPath);
            
            // Pattern: /qualifier-value/qualifier-value/basename.qualifier-value.qualifier-value.extension
            var matches = Directory.GetFiles(directory, baseName + ".*" + extension, SearchOption.AllDirectories);

            return matches.ToList();
        }

        public static List<string> GetQualifiersFromFilePath(string basePath, string filePath)
        {
            var qualifiedPath = filePath.Substring(basePath.Length);
            
            // get qualifiers from the dir names before the file, eg  /qualifier-value/fileName.extension
            IEnumerable<string> dirQualifiers = Path
                .GetDirectoryName(qualifiedPath)
                .TrimEnd(Path.DirectorySeparatorChar)
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany((s)=>s.Split("_"));

            // get qualifiers from the file name eg basename.qualifier-value_qualifier-value.extension
            IEnumerable<string> fileNameQualifiers = Path
                .GetFileNameWithoutExtension(qualifiedPath)
                .Split(".")
                .ElementAtOrDefault(1)
                ?.Split("_")
                ?? Array.Empty<string>();

            return fileNameQualifiers.Concat(dirQualifiers).ToList();

        }

    }
}