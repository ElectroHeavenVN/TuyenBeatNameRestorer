using dnlib.DotNet;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace TuyenBeatNameRestorer
{
    internal class Program
    {
        [DllImport("msvcrt.dll")]
        static extern int system(string cmd);

        static XmlDocument obfuscatedXmlMapping;

        static ModuleDef obfuscatedModule; 

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Yellow;
            for (int i = 0; i < (args.Length > 2 ? 2 : args.Length); i++)
            {
                string str = args[i];
                if (str.EndsWith(".xml"))
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(str.Replace("\"", ""));
                    obfuscatedXmlMapping = xml;
                }
                if (str.EndsWith(".exe") || str.EndsWith(".dll"))
                {
                    obfuscatedModule = AssemblyDef.Load(str.Replace("\"", "")).ManifestModule;
                }
            }
            if (obfuscatedModule == null)
            {
                Console.Write("Đường dẫn tệp bị mã hóa: ");
                obfuscatedModule = AssemblyDef.Load(Console.ReadLine().Replace("\"", "")).ManifestModule;
            }
            if (obfuscatedXmlMapping == null)
            {
                Console.Write("Đường dẫn tệp XML: ");
                XmlDocument xml = new XmlDocument();
                xml.Load(Console.ReadLine().Replace("\"", ""));
                obfuscatedXmlMapping = xml;
            }
            if (obfuscatedXmlMapping == null || obfuscatedModule == null)
            {
                Console.WriteLine("Tệp XML hoặc tệp bị mã hóa không được chỉ định!");
                system("pause");
                return;
            }
            XmlNode mappings = obfuscatedXmlMapping.DocumentElement.SelectSingleNode("/mappings");
            if (mappings == null)
            {
                Console.WriteLine("Tệp XML không đúng định dạng!");
                system("pause");
                return;
            }
            Console.WriteLine("Đang giải mã " + obfuscatedModule.FullName + "...");
            TypeDef lastType = obfuscatedModule.GlobalType;
            Dictionary<string, string> namespaceMapping = new Dictionary<string, string>();
            foreach (XmlNode xmlNode in mappings.ChildNodes.Cast<XmlNode>().Where(node => node.Attributes["Type"].Value == "Namespace"))
            {
                namespaceMapping.Add(xmlNode.Attributes["ObfuscatedValue"].Value, xmlNode.Attributes["InitialValue"].Value);
            }
            foreach (XmlNode node in mappings.ChildNodes)
            {
                string obfuscatedName = node.Attributes["ObfuscatedValue"].Value;
                string originalName = node.Attributes["InitialValue"].Value;
                string type = node.Attributes["Type"].Value;
                if (type == "Type")
                {
                    try
                    {
                        lastType = obfuscatedModule.Find(obfuscatedName, false);
                        lastType.Name = originalName;
                    }
                    catch (Exception)
                    {
                        foreach (TypeDef typeDef in obfuscatedModule.GetTypes())
                        {
                            if (typeDef.Name == obfuscatedName)
                            {
                                typeDef.Name = originalName;
                                if (!string.IsNullOrEmpty(typeDef.Namespace))
                                {
                                    string originalNamespace = "";
                                    foreach (string str in ((string)typeDef.Namespace).Split('.'))
                                    {
                                        originalNamespace += namespaceMapping[str] + ".";
                                    }
                                    originalNamespace = originalNamespace.Substring(0, originalNamespace.LastIndexOf('.'));
                                    typeDef.Namespace = originalNamespace;
                                }
                                lastType = typeDef;
                                break;
                            }
                        }
                    }
                }
                if (type == "Method")
                {
                    lastType.FindMethod(obfuscatedName).Name = originalName;
                }
                if (type == "Field")
                {
                    lastType.FindField(obfuscatedName).Name = originalName;
                }

            }
            Console.WriteLine("Đang lưu tệp...");
            string outputName = Path.GetFileNameWithoutExtension(obfuscatedModule.FullName) + "-Beated" + Path.GetExtension(obfuscatedModule.FullName);
            obfuscatedModule.Write(outputName);
            Console.WriteLine("Tệp được lưu tại: " + Environment.CurrentDirectory + "\\" + outputName);
            system("pause");
        }
    }
}
