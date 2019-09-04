using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;

namespace UnityShaderCompeniumCompiler {
    class Program {

        static void Main(string[] args) {

            string savePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            savePath = Path.GetDirectoryName(savePath);

            JObject filesData = new JObject();
            JArray filesArray = new JArray();
            filesData["files"] = filesArray;

            Console.WriteLine($"Parsing {args.Length} Files...");

            foreach (string arg in args) {
                JObject fileData = CgincParser.Parse(arg);

                filesArray.Add(fileData);
            }

            Console.WriteLine("Donezo.");

            File.WriteAllText($"{savePath}\\Output.json", "cgincData = " + filesData.ToString());



            Console.Read();
        }



    }//END CLASS
}
