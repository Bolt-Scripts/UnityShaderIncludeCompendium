using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityShaderCompeniumCompiler {


    class CgincParser {
        //----------------------------------------------------------------------------
        //I swear i usually write better less nonsense code than this
        //I did this quickly in like a day just to get something done that works
        //----------------------------------------------------------------------------


        static readonly Regex extraSpaces = new Regex(@"\s\s+");

        static string GetComment(string[] lines, int startLine) {
            if (startLine <= 0) return null;

            string line = lines[startLine].Trim();
            string comment = "";

            //for detecting comments that might exist at the end of the actual line of code
            if (line.Contains("//")) {
                comment = line.Substring(line.IndexOf("//"));
            }

            checkComment:
            if (startLine <= 0) return comment;
            line = lines[startLine - 1].Trim();

            //most, if not all, block comments in the cginc files use * at the start of each line
            //so im taking advantage of that to detect those
            if (line.StartsWith("//") || line.StartsWith("/*") || line.StartsWith("*") || line.StartsWith("*/")) {
                comment = lines[--startLine] + (string.IsNullOrEmpty(comment) ? "" : "\n") + comment;
                goto checkComment;
            }

            if (!string.IsNullOrEmpty(line)) {
                startLine--;
                goto checkComment;
            }

            return comment;
        }

        static JObject InitObj(string[] lines, int lineNum) {
            JObject jobj = new JObject();
            jobj["comment"] = GetComment(lines, lineNum);
            jobj["lineNum"] = lineNum;

            return jobj;
        }


        static void ProcessMetadata(string input, JObject receptacle) {
            string[] lines = input.Split('\n');
            for(int i = 0; i < lines.Length; i++) {
                lines[i] = lines[i].StripComments();
            }
            input = string.Join("", lines);

            //might be missing some but eh
            string stripped = input.Replace("volatile", "").Replace("inline", "").Replace("const", "").Replace("static", "");
            stripped = stripped.Replace("uniform", "");
            stripped = extraSpaces.Replace(stripped.Trim(), " ");

            if (stripped.StartsWith("struct")) {
                receptacle["type"] = "struct";
                string name = stripped.Replace("struct", "").Replace("{", "").Trim();
                receptacle["name"] = name;
                receptacle["modifiers"] = input.Replace("struct", "").Replace(name, "").Replace("{", "").Trim();
            } else {
                string[] items = stripped.Split(' ');
                string type = items[0].Trim();
                string name = items[1].Trim();

                int leftP = input.IndexOf('(');
                int eqIdx = input.IndexOf('=');

                if(eqIdx > 0) {
                    if (leftP > 0 && leftP > eqIdx) {
                        //handles situations like:
                        //static float4x4 unity_MatrixMVP = mul(unity_MatrixVP, unity_ObjectToWorld);

                        receptacle["assignment"] = input.Substring(eqIdx);
                        input = input.Substring(0, eqIdx);
                        leftP = -1;
                    }
                }

                if (leftP > 0) {
                    if (name.Contains('(')) name = name.Substring(0, name.IndexOf('(')).Trim();

                    int rightP = input.LastIndexOf(')');
                    receptacle["modifiers"] = input.Substring(0, leftP).Replace(type, "").Replace(name, "").Trim();
                    receptacle["parameters"] = input.Substring(leftP + 1, rightP - leftP - 1).Trim();
                } else {
                    //var
                    receptacle["modifiers"] = input.Replace(type, "").Replace(name, "").Replace(";", "").Trim();
                }

                receptacle["type"] = type;
                receptacle["name"] = name;
            }
        }

        static JObject HandleVariable(string[] lines, string line, int i) {
            JObject varObj = InitObj(lines, i);
            ProcessMetadata(line, varObj);

            varObj["code"] = line.Trim();
            return varObj;
        }


        public static JObject Parse(string path) {

            if (!File.Exists(path)) return null;

            string fileName = Path.GetFileName(path);
            JObject fileData = new JObject();
            fileData["file"] = fileName;

            JArray definesArray = new JArray();
            fileData["defines"] = definesArray;

            JArray functionsArray = new JArray();
            fileData["functions"] = functionsArray;

            JArray structsArray = new JArray();
            fileData["structs"] = structsArray;

            JArray varsArray = new JArray();
            fileData["variables"] = varsArray;

            string[] lines = (new string[] { "" }).Concat(File.ReadAllLines(path)).Concat((new string[] { "" })).ToArray();

            for (int i = 0; i < lines.Length - 1; i++) {

                string line = lines[i];
                string trimmedLine = lines[i].StripComments().Trim();
                string fullCommand = line;
                int startLineNum = i;

                //if (i == 621)
                //    Console.WriteLine();

                try {

                    //justttt ignore these
                    if (trimmedLine.StartsWith("/*") || trimmedLine.StartsWith("*/") || trimmedLine.StartsWith("*")
                        || trimmedLine.StartsWith("CBUFFER_END") || trimmedLine.StartsWith("GLOBAL_CBUFFER_END") 
                        || trimmedLine.StartsWith("UNITY_INSTANCING_CBUFFER_SCOPE_END")) {
                        continue;
                    }

                    if (trimmedLine.StartsWith("#define")) {
                        JObject defineObj = InitObj(lines, i);

                        //should probably be a bit more verbose with parsing macro definitions
                        //at least to be able to pic out the actual name of the macro rather than just
                        //using the whole thing, so it could be searched more easily later
                        //but eh, lazy ykno

                        lineCheck:
                        if (trimmedLine.EndsWith("\\")) {
                            //multiline macro
                            line = lines[++i];
                            trimmedLine = line.StripComments().Trim();
                            fullCommand += "\n" + line;
                            goto lineCheck;
                        }

                        defineObj["code"] = fullCommand;
                        definesArray.Add(defineObj);
                        continue;


                    } else if (trimmedLine.StartsWith("#")) {
                        //some kind of preprocessor conditional thing prob
                        //for now dont really care to gather this info
                        continue;

                    } else if (trimmedLine.Contains('(') || trimmedLine.Contains("struct")) {
                        //some kind of function or struct declaration
                        JObject funcObj = InitObj(lines, i);
                        string funcDeclare = line;

                        if (!trimmedLine.Contains("struct")) {
                            int opIdx = trimmedLine.IndexOf('(');
                            string beforePart = trimmedLine.Substring(0, opIdx).Trim();
                            string[] parts = beforePart.Split(' ');

                            if (parts.Length <= 1) {
                                //in this situation its most likely a shader macro being used
                                //not a definition of anything, so we skip it
                                continue;
                            }

                            if (trimmedLine.Contains('=')) {
                                int eqIdx = trimmedLine.IndexOf('=');
                                if(eqIdx > 0 && eqIdx < opIdx) {
                                    //equals sign is before the open parenthesis of the function
                                    //so its actually a variable assignment

                                    varsArray.Add(HandleVariable(lines, line, i));
                                    continue;
                                }
                            }

                            if (trimmedLine.EndsWith(";")) {
                                //it wouldnt make sense for a semicolon to be here
                                //so its prob a situation like this:
                                //half    UnitySampleShadowmap_PCF7x7(float4 coord, float3 receiverPlaneDepthBias);

                                varsArray.Add(HandleVariable(lines, line, i));
                                continue;
                            }
                        }

                        int openBracketCount = trimmedLine.Where(x => x == '{').Count();
                        if (openBracketCount > 0) {
                            ProcessMetadata(line, funcObj);
                        }

                        //need to check close first just in case the function is open/close on same line
                        goto checkClose;

                        lineCheck:

                        line = lines[++i];
                        trimmedLine = line.StripComments().Trim();
                        fullCommand += "\n" + line;
                        int openBrackets = trimmedLine.Where(x => x == '{').Count();
                        if (openBrackets > 0 && openBracketCount == 0) {
                            ProcessMetadata(fullCommand, funcObj);
                        }
                        openBracketCount += openBrackets;


                        checkClose:
                        if (trimmedLine.Contains('}')) {

                            int closeBrackets = trimmedLine.Where(x => x == '}').Count();
                            if (closeBrackets > 0) {
                                openBracketCount -= closeBrackets;

                                if (openBracketCount <= 0) {
                                    funcObj["code"] = fullCommand;
                                    if (funcObj["type"].ToString() == "struct") {
                                        structsArray.Add(funcObj);
                                    } else {
                                        functionsArray.Add(funcObj);
                                    }

                                    continue;
                                }
                            }
                        }

                        goto lineCheck;


                    } else if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#")) {
                        //something else, most likely a variable declaration
                        varsArray.Add(HandleVariable(lines, line, i));
                    }

                } catch (Exception ex) {
                    Console.WriteLine("------------------------------");
                    Console.WriteLine("-----------ERROR------------");
                    Console.WriteLine("------------------------------");
                    Console.WriteLine(ex);
                    Console.WriteLine();
                    Console.WriteLine(fileName + " - " + i + ": " + fullCommand);

                    i = startLineNum + 1;
                }
            }

            //Console.WriteLine("------------------------------");
            //Console.WriteLine("-----------RESULTS------------");
            //Console.WriteLine("------------------------------");

            //Console.WriteLine(fileData);


            return fileData;
        }


    }//end cgincparser


    public static class Extensions {

        public static string StripComments(this string str) {
            string result = str;

            commentBlockCheck:
            int dSlashIdx = result.IndexOf("/*");
            if (dSlashIdx >= 0) {
                int endIdx = result.IndexOf("*/");
                result = result.Remove(dSlashIdx, endIdx - dSlashIdx + 2);
                goto commentBlockCheck;
            }

            dSlashIdx = result.IndexOf("//");
            if (dSlashIdx >= 0) {
                result = result.Substring(0, dSlashIdx);
            }

            return result;
        }
    }
}
