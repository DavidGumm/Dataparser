using System;
using System.Text.Json;
using System.Data;
using System.Collections;
using Microsoft.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using System.Xml;
using System.Text;

namespace Dataparser
{
    class Program
    {
        public static bool Verbose { get; set; }
        public static string Path { get; set; }
        public static List<string> Output { get; set; }
        public static Newtonsoft.Json.Formatting CompressJson { get; set; }
        public static Newtonsoft.Json.Formatting CompressXlm { get; set; }

        static void Main(string[] args)
        {
            //string path, bool verbose, bool json, bool xml, bool compress
            var app = new CommandLineApplication();
            app.Name = "ConsoleArgs";
            app.Description = ".NET Core console app with argument parsing.";

            app.HelpOption("-?|-h|--help");

            var pathOption = app.Option("-p|--path <optionvalue>",
                "Path to the folders to parse.",
                CommandOptionType.SingleValue);

            var outputOption = app.Option("-o|--output <optionvalue>",
                "Output file location(s)/name(s).",
                CommandOptionType.MultipleValue);

            var parseOption = app.Option("-a|--parse <optionvalue>",
                "Parse options are json or xml.",
                CommandOptionType.SingleValue);

            //var verboseOption = app.Option("-v|--verbose",
            //        "Enable verbose output.",
            //        CommandOptionType.NoValue);

            //var compressOption = app.Option("-c|--compress",
            //        "Crompress the output.",
            //        CommandOptionType.NoValue);

            app.OnExecute(() => {
                if (pathOption.HasValue())
                {
                    Path = pathOption.Value();
                }
                else
                {
                    Console.WriteLine("-p|--path has no value.");
                    app.ShowHint();
                    return 0;
                }

                if (outputOption.HasValue())
                {
                    Output = outputOption.Value().Split((Char)',').ToList();
                }
                else
                {
                    Console.WriteLine("-o|--output has no value.");
                    app.ShowHint();
                    return 0;
                }

                if (!parseOption.HasValue())
                {
                    Console.WriteLine("-a|parse has no value. Please input either xml or json");
                    app.ShowHint();
                    return 0;
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                Console.WriteLine("");
                Console.WriteLine("Starting parser.");

                //Verbose = verboseOption.value
                //if (compressOption.HasValue())
                //{
                //    CompressJson = Newtonsoft.Json.Formatting.None;
                //    CompressXlm = Newtonsoft.Json.Formatting.None;
                //}

                if (parseOption.Value() == "xml")
                {
                    ParseXml();
                }

                if (parseOption.Value() == "json")
                {
                    ParseJson();
                }

                stopwatch.Stop();
                var runTime = Math.Round(((double)stopwatch.ElapsedMilliseconds / (double)1000), 3);

                Console.WriteLine();
                Console.WriteLine("Finished.");
                Console.WriteLine($"Runtime: {runTime} seconds.");

                return 0;
            });

            app.Command("Dataparser", (command) =>
            {
                command.Description = "This is the description for Dataparser.";
                command.HelpOption("-?|-h|--help");

                command.OnExecute(() =>
                {
                    Console.WriteLine("Dataparser has finished.");
                    return 0;
                });
            });
            app.Execute(args);
        }

        static void ParseXml()
        {
            var dataPath = $"{Directory.GetCurrentDirectory()}\\..\\Data\\db.xml";
            XmlDocument document = new XmlDocument();
            document.Load(dataPath);
            string data = JsonConvert.SerializeXmlNode(document);

            System.IO.File.WriteAllText($"{dataPath}\\..\\db.json", data);
        }

        static void ParseJson()
        {
            var files = DirSearch(Path);
            Console.WriteLine($"Files located: {files.Count}");

            JToken Database = JToken.Parse("{}");
            Console.WriteLine($"Reading files.");
            var fileCount = files.Count();

            for (int i = 0; i < fileCount; i++)
            {
                string f = files[i];
                var count = i + 1;

                JToken table = null;
                var path = f.Replace(Path, "").Replace(".JSON", "").Split("\\").ToList();
                var fileName = path[path.Count() - 1];
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                ClearCurrentConsoleLine();
                Console.WriteLine($"Reading file {count} / {fileCount} - {fileName}                                                                                ");

                table = JToken.Parse(System.IO.File.ReadAllText(f));

                var tableName = table.Children<JProperty>().Select(P => P.Name).FirstOrDefault();
                if (tableName == fileName)
                {
                    table = JToken.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(table[tableName]));
                }

                var propertyNames = table.Children<JProperty>().Select(P => P.Name);
                if (propertyNames.Contains("Description"))
                {
                    table["Description"] = table["Description"] != null ? table["Description"] : "n/a";
                }

                table = RemoveEmptyChildren(table);

                System.IO.File.WriteAllText(f, table.ToString(CompressJson));

                Database = SetDatabase(Database, path, table, 0, 64);
            }
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            ClearCurrentConsoleLine();
            Console.WriteLine("                                                                                ");

            var output = Newtonsoft.Json.JsonConvert.SerializeObject(Database);
            foreach (var location in Output)
            {
                Console.WriteLine();
                System.IO.File.WriteAllText(location, output);
                Console.WriteLine($"File Location: {location}");
                Console.WriteLine($"File payload: {output.Count()} bytes");
            }
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        static void WriteError(String[] args, Exception error)
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }
            Console.WriteLine("");
            Console.WriteLine($"{error.Message}");
            Console.WriteLine($"{error.StackTrace}");
            Console.ResetColor();
            Console.WriteLine("");

        }
        static JToken SetDatabase(JToken data, List<string> Path, JToken table, int depth, int maxDepth)
        {
            if (depth > maxDepth) return data;

            var path = Path.Skip(depth).FirstOrDefault();
            var properties = data.Children<JProperty>().Select(P => P.Name).ToList();

            if (!properties.Contains(path))
            {
                data[path] = JObject.Parse("{}");
            }

            if (Path.Count() > 1 && depth < Path.Count() - 1)
            {
                try
                {
                    data[path] = SetDatabase(data[path], Path, table, ++depth, maxDepth);
                }
                catch (Exception error)
                {
                    string[] args = new String[]{
                        "",
                        "setDatabase",
                        "",
                        $"at Depth:{(depth + 1)}",
                        $"at Path:{String.Join(" -> ", Path)}"};
                    WriteError(args, error);
                }
            }
            else
            {
                try
                {
                    data[path] = table;
                }
                catch (Exception error)
                {
                    string[] args = new String[]{"",
                        data.ToString(),
                        "",
                        "else",
                        "",
                        $"at Depth:{(depth + 1)}",
                        $"at Path:{String.Join(" -> ", Path)}"};
                    WriteError(args, error);
                }
            }
            return data;
        }

        static List<string> DirSearch(string sDir)
        {
            return Directory.GetFiles(sDir, "*.JSON", SearchOption.AllDirectories).ToList();
        }

        public static JToken RemoveEmptyChildren(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                JObject copy = new JObject();
                foreach (JProperty prop in token.Children<JProperty>())
                {
                    JToken child = prop.Value;
                    if (child.HasValues)
                    {
                        child = RemoveEmptyChildren(child);
                    }
                    if (!IsEmpty(child))
                    {
                        copy.Add(prop.Name, child);
                    }
                }
                return copy;
            }
            else if (token.Type == JTokenType.Array)
            {
                JArray copy = new JArray();
                foreach (JToken item in token.Children())
                {
                    JToken child = item;
                    if (child.HasValues)
                    {
                        child = RemoveEmptyChildren(child);
                    }
                    if (!IsEmpty(child))
                    {
                        copy.Add(child);
                    }
                }
                return copy;
            }
            return token;
        }

        public static bool IsEmpty(JToken token)
        {
            return (token.Type == JTokenType.Null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues);
        }
    }
}
