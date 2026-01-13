using System.Xml.Linq;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class XMLParser
    {
        public XDocument XMLDoc;

        public XNamespace DefaultNamespace;

        public XMLParser(string filePath)
        {
            XMLDoc = XDocument.Load(filePath);

            DefaultNamespace = XMLDoc.Root.GetDefaultNamespace();
        }

        public bool TryGetValueByUnqualifiedPath(out string? value,params string[] pathSegments)
        {
            value =  GetElementByUnqualifiedPath(pathSegments)?.Value;
            return value != null;
        }

        public XElement? GetElementByUnqualifiedPath(params string[] pathSegments)
        {
            XElement root = XMLDoc.Root;
            if (root == null) return null;

            // Try with namespace first
            XElement current = root;
            foreach (var segment in pathSegments)
            {
                current = current?.Element(DefaultNamespace + segment);
                if (current == null) break;
            }
            
            if (current != null)
                return current;
            
            // Fallback: try without namespace
            current = root;
            foreach (var segment in pathSegments)
            {
                current = current?.Element(segment);
                if (current == null) break;
            }
            
            return current;
        }
    }
}
