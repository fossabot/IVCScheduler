using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ARIIVC.Accelerator
{
    public class AcceleratorXmlParser
    {
        public string WorkflowName;
        public string Error { get; set; }
        public List<string> Functions
        {
            get { return _functionsHashSet.ToList(); }
        }
        private readonly HashSet<string> _functionsHashSet = new HashSet<string>();

        public AcceleratorXmlParser(string workflowName)
        {
            WorkflowName = workflowName;
        }
        public AcceleratorXmlParser()
        {
        }

        public int ProcessXmlFiles(List<string> xmlFiles)
        {            
            foreach (var xmlFile in xmlFiles)
            {
                FileInfo fi = new FileInfo(xmlFile);
                if (fi.Length > 0)
                {
                    ProcessXml(xmlFile);
                    Console.WriteLine("Parsed : {0} and added {1} records as functions traced", xmlFile, _functionsHashSet.Count);                    
                }
                else
                {
                    Console.WriteLine("Ignored the file {0} as it's size is 0", fi.Name);
                }
            }
            return _functionsHashSet.Count;
        }

        public void ProcessXml(string xmlFile)
        {
            try
            {
                XDocument doc = XDocument.Load(xmlFile);
                if (doc.Root != null)
                {
                    var elements = (from function in doc.Root.Descendants("function")
                        let xAttribute = function.Attribute("functname")
                        where xAttribute != null //&& (!xAttribute.Value.Contains("@ D0001_")) //Added D0001 in exclusion list as these are generated code methods
                        select xAttribute.Value.Split('-')[0]).ToList();
                    _functionsHashSet.UnionWith(elements);
                }
            }
            catch (Exception excp)
            {
                Error = "Error occurred during parsing of " + xmlFile + "\n" + excp;
            }
        }
    }
}
