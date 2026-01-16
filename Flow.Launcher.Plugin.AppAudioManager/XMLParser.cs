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

        public bool TryGetValueByPath(out string? value,params string[] pathSegments)
        {
            value =  GetElementByPath(pathSegments)?.Value;
            return value != null;
        }
        public bool TryGetElementByPath(out XElement? element,params string[] pathSegments)
        {
            element =  GetElementByPath(pathSegments);
            return element != null;
        }

        public bool TryGetAttributeValue(out string? value, XElement element, string attributeName)
        {
            value =  element.Attribute(attributeName)?.Value;
            return value != null;
        }

        public XElement? GetElementByPath(params string[] pathSegments)
        {
            XElement root = XMLDoc.Root;
            if (root == null) return null;
            
            XElement current = root;
            foreach (var segment in pathSegments)
            {
                var segmentParts = segment.Split(":",2);

                if (segmentParts.Length == 1)
                {
                    current = current?.Element(DefaultNamespace + segment);
                }
                else 
                {
                    var prefix = segmentParts[0];
                    var localName = segmentParts[1];
                    var ns = XMLDoc.Root.GetNamespaceOfPrefix(prefix);
                    current = current?.Element(ns + localName);
                }
                
                if (current == null) break;
            }
            
            if (current != null)
                return current;
        
            
            return current;
        }
    }
}
