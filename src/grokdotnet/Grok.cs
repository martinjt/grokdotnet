using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace GrokDotNet
{
    public class Grok
    {
        private readonly string _patternsDirectory;
        private readonly string _grokString;
        private string _newGrokString;
        private readonly Regex _regexFull = new Regex(@"%{(\w+):(\w+)(?::\w+)?}", RegexOptions.Compiled);
        private readonly Regex _regexWithType = new Regex(@"%{(\w+):(\w+):(\w+)?}", RegexOptions.Compiled);
        private readonly Regex _regexWithoutName = new Regex(@"%{(\w+)}", RegexOptions.Compiled);

        private Dictionary<string, string> _patterns = new Dictionary<string, string>();
        private bool _patternsLoaded;
        private Dictionary<string, string> _typeMaps = new Dictionary<string, string>();
        private Regex _compiledRegex;
        private List<string> groupNames;

        public string ParsedRegexString {
            get {
                return _newGrokString;
            }
        }

        public Grok(string grokString)
        {
            _grokString = grokString;
        }

        public Grok(string grokString, string patternsDirectory)
        {
            _grokString = grokString;
            _patternsDirectory = patternsDirectory;
        }

        private void ParseGrokString()
        {
            if (!_patternsLoaded)
            {
                LoadPatterns();
                _patternsLoaded = true;
            }

            var returnString = string.Empty;
            while (true)
            {
                var shouldFinish = false;
                foreach (Match match in _regexWithType.Matches(string.IsNullOrEmpty(returnString) ? _grokString : returnString))
                {
                    _typeMaps.Add(match.Groups[2].Value, match.Groups[3].Value);
                }

                var newString = _regexFull.Replace(string.IsNullOrEmpty(returnString) ? _grokString : returnString, new MatchEvaluator(ReplaceWithName));
                newString = _regexWithoutName.Replace(newString, new MatchEvaluator(ReplaceWithoutName));
                if (newString.Equals(returnString, StringComparison.CurrentCultureIgnoreCase))
                    shouldFinish = true;

                returnString = newString;

                if (shouldFinish)
                    break;
            }
            _compiledRegex = new Regex(returnString, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
            groupNames = _compiledRegex.GetGroupNames().ToList();
        }

        private void LoadPatterns()
        {
            var assembly = typeof(Grok).GetTypeInfo().Assembly;
            var resources = assembly.GetManifestResourceNames();
            foreach (var resourceName in resources)
            {
                if (!resourceName.EndsWith(".pattern"))
                    return;

                var stream = assembly.GetManifestResourceStream(resourceName);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while(!reader.EndOfStream)
                    {
                        ProcessPatternLine(reader.ReadLine());
                    }
                }
            }
            if (string.IsNullOrEmpty(_patternsDirectory))
                return;

            foreach (var file in Directory.GetFiles(_patternsDirectory, "*.pattern"))
            {
                foreach (var line in File.ReadAllLines(file))
                {
                    ProcessPatternLine(line);
                }
            }
        }
        private void ProcessPatternLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            var split = line.Split(new[] { ' ' }, 2);

            if (split[0].Equals("#", StringComparison.OrdinalIgnoreCase))
                return;

            _patterns.Add(split[0], split[1]);
        }

        private string ReplaceWithName(Match match)
        {
            var patternName = match.Groups[2];
            var name = match.Groups[1];
            var pattern = string.Empty;
            if (_patterns.ContainsKey(name.Value))
                pattern = _patterns[name.Value];

            return $"(?<{patternName}>{pattern})";
        }

        private string ReplaceWithoutName(Match match)
        {
            var patternName = match.Groups[1];
            var pattern = string.Empty;
            if (_patterns.ContainsKey(patternName.Value))
                pattern = _patterns[patternName.Value];

            return $"({pattern})";
        }

        public GrokParseResponse ParseLine(string line)
        {
            if (_compiledRegex == null)
                ParseGrokString();

            var response = new GrokParseResponse();

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (Match match in _compiledRegex.Matches(line))
            {
                foreach (var name in groupNames)
                {
                    if (name == "0")
                        continue;
                        
                    if (_typeMaps.ContainsKey(name))
                        response.Captures.Add(new Tuple<string, object>(name, MapType(_typeMaps[name], match.Groups[name].Value)));
                    else
                        response.Captures.Add(new Tuple<string, object>(name, match.Groups[name].Value));
                }
            }
            stopWatch.Stop();
            response.TimeTaken = stopWatch.Elapsed;

            return response;
        }

        private object MapType(string type, string data)
        {
            var typeCompare = type.ToLowerInvariant();
            try {
                if (typeCompare == "int")
                    return Convert.ToInt32(data);
                if (typeCompare == "float")
                    return Convert.ToDouble(data);
                if (typeCompare == "datetime")
                    return DateTime.Parse(data);
            }
            catch (Exception)
            {
            }
            return data;
        }
    }
}