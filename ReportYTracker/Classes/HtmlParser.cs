using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportYTracker.Helpers
{
    internal class HtmlParser
    {
        public static Dictionary<string, string> ParseHtmlInputs(string html)
        {
            var inputs = new Dictionary<string, string>();

            var regex = new Regex("<input[^>]+>", RegexOptions.IgnoreCase);
            var matches = regex.Matches(html);

            foreach (Match match in matches)
            {
                var input = match.Value;

                var nameMatch = Regex.Match(input, "name\\s*=\\s*\"([^<\"]*)\"", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    var name = nameMatch.Groups[1].Value;

                    var valueMatch = Regex.Match(input, "value\\s*=\\s*\"([^<\"]*)\"", RegexOptions.IgnoreCase);
                    var value = valueMatch.Success ? valueMatch.Groups[1].Value : "";

                    inputs[name] = value;
                }
            }

            return inputs;
        }
        public static string GetFormAction(string html)
        {
            var regex = new Regex("<form[^>]+>", RegexOptions.IgnoreCase);
            var match = regex.Match(html);

            if (match.Success)
            {
                var formTag = match.Value;

                var actionMatch = Regex.Match(formTag, "action\\s*=\\s*\"([^<\"]*)\"", RegexOptions.IgnoreCase);
                if (actionMatch.Success)
                {
                    return actionMatch.Groups[1].Value;
                }
            }

            return null;
        }
    }
}
