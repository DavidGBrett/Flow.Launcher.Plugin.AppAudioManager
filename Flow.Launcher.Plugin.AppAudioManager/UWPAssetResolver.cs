using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    }
}