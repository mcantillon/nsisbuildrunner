using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace makensisbuildrunner
{
    // Orginally from http://www.tandisoft.com/2011/11/built-teamcity-makensisexe-output.html

    public class Program
    {
        private static Regex rxMessage = new Regex("!system: returned 1", RegexOptions.Compiled);
        private static Regex rxMessage2 = new Regex(" -> no files found.", RegexOptions.Compiled);
        private static Regex rxMessage3 = new Regex("aborting creation process", RegexOptions.Compiled);
        private static Regex rxMessage4 = new Regex("Can't open script ", RegexOptions.Compiled);
        private static Regex rxMessage5 = new Regex("Invalid command: ", RegexOptions.Compiled);

        private static Regex rxFirstLine = new Regex("Processing script file:", RegexOptions.Compiled);

        private static Dictionary<int, List<string>> flows = new Dictionary<int, List<string>>();

        private static string nsisCommandLineAppName = "makensis.exe";

        static void Main(string[] args)
        {

            string nsisCommandLineAppPath = ConfigurationManager.AppSettings["NSISCommandLineAppPath"];
            if (string.IsNullOrEmpty(nsisCommandLineAppPath))
            {
                throw new ArgumentNullException("NSISCommandLineAppPath", "The config setting that tells me where to find makensis.exe is not set");
            }

            string nsisCommandLineAppExecutionPath = Path.Combine(nsisCommandLineAppPath, nsisCommandLineAppName);

            if (!File.Exists(nsisCommandLineAppExecutionPath))
            {
                throw new FileNotFoundException("Unable to file " + nsisCommandLineAppName + " in " + nsisCommandLineAppPath, nsisCommandLineAppName);
            }

            string pattern = string.Format("^[\"]?({0})[\"]?\\s*", Environment.GetCommandLineArgs()[0].Replace("\\", "\\\\"));
            Regex rgx = new Regex(pattern);
            string argumentsOnly = rgx.Replace(Environment.CommandLine, string.Empty);

            if (string.IsNullOrEmpty(argumentsOnly))
            {
                throw new ArgumentNullException("NSISScript", "You didn't give me a NSIS script to use");
            }
            string fullPathToNSISScript = string.Empty;

            argumentsOnly = argumentsOnly.Replace("'", string.Empty);
            argumentsOnly = argumentsOnly.Replace(@"""", string.Empty);
            //argumentsOnly = argumentsOnly.Replace(@"\\", @"\");

            //if (argumentsOnly.IndexOfAny(Path.GetInvalidPathChars()) > 0 || argumentsOnly.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
            //{
            //    throw new ArgumentOutOfRangeException("NSISScript", "Invalid vlaues in Path and/or File name for NSISScript to complie");
            //}

            fullPathToNSISScript = string.IsNullOrEmpty(Path.GetDirectoryName(argumentsOnly)) ? Path.Combine(Environment.CurrentDirectory, argumentsOnly) : Path.GetFullPath(argumentsOnly);

            if (!File.Exists(fullPathToNSISScript))
            {
                throw new FileNotFoundException("Unable to find NSISScript to complie. Looking for " + fullPathToNSISScript, fullPathToNSISScript);
            }

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = nsisCommandLineAppExecutionPath,
                WorkingDirectory = Path.GetDirectoryName(fullPathToNSISScript),
                Arguments = "\"" + Path.GetFileName(fullPathToNSISScript) + "\"",
                CreateNoWindow = true,
                ErrorDialog = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            Process nsisProcess = Process.Start(psi);

            Thread StdOReader = new Thread(new ParameterizedThreadStart(ReadStd));
            StdOReader.Start(nsisProcess.StandardOutput);
            Thread StdEReader = new Thread(new ParameterizedThreadStart(ReadStd));
            StdEReader.Start(nsisProcess.StandardError);

            nsisProcess.WaitForExit();

            StdOReader.Join();
            StdEReader.Join();
            Console.Out.Flush();
            Environment.ExitCode = nsisProcess.ExitCode;
        }

        private static void ReadStd(object obj)
        {
            TextReader R = (TextReader)obj;
            string line = R.ReadLine();
            while ((line != null))
            {
                ProcessLine(line);
                line = R.ReadLine();
            }
        }
        
        private static void ProcessLine(string line)
        {
            int flowID = 0;
            Match M = default(Match);
            string status = "NORMAL";

            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }
            M = rxMessage.Match(line);
            if (!object.ReferenceEquals(M, Match.Empty))
            {
                status = "ERROR";
            }
            M = rxMessage2.Match(line);
            if (!object.ReferenceEquals(M, Match.Empty))
            {
                status = "ERROR";
            }
            M = rxMessage3.Match(line);
            if (!object.ReferenceEquals(M, Match.Empty))
            {
                status = "FAILURE";
            }
            M = rxMessage4.Match(line);
            if (!object.ReferenceEquals(M, Match.Empty))
            {
                status = "FAILURE";
            }

            M = rxMessage5.Match(line);
            if (!object.ReferenceEquals(M, Match.Empty))
            {
                status = "ERROR";
            }
            M = rxFirstLine.Match(line);
            if (!object.ReferenceEquals(M, Match.Empty))
            {
                Console.Out.WriteLine("##teamcity[progressMessage '{0}']", QuoteLine(line));
            }

            line = QuoteLine(line);

            if ((!flows.ContainsKey(flowID)))
            {
                flows[flowID] = new List<string>();
            }

            string fmtLine = string.Format("##teamcity[message status='{2}' errorDetails='' text='[{0:000}|] {1}']", flowID, line, status);

            Console.Out.WriteLine(fmtLine);
            Console.Out.Flush();
            flows[flowID].Add(fmtLine);
        }

        private static string QuoteLine(string line)
        {
            var sb = new StringBuilder();

            foreach (char c in line)
            {
                if (("'|]" + Environment.NewLine).IndexOf(c) != -1)
                {
                    sb.Append('|');
                }
                sb.Append(c);
            }

            line = sb.ToString();
            return line;
        }

    }
}
