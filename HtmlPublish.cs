using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace UMC.Subs
{

    public class HtmlPublish
    {

        class TextTpl
        {
            public bool IsBegin
            {
                get; set;
            }

            public String Text
            {
                get;
                set;
            }
            public String Key
            {
                get; set;
            }
            public List<TextTpl> Children
            {
                get; set;
            }
        }
        static readonly Regex resourceName = new Regex("<!--%\\s*(?<resourceName>[^%]+)\\s*%-->", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        static String AppendTitle(String body, String meta)
        {
            var sb = new StringBuilder(body);
            var index = body.IndexOf("<title", StringComparison.CurrentCultureIgnoreCase);
            if (index > 0)
            {
                var endindex = body.IndexOf("</title>", StringComparison.CurrentCultureIgnoreCase);

                endindex = endindex + 7;


                while (endindex != -1)
                {
                    var bindex = body.IndexOf('<', endindex);
                    if (bindex > -1)
                    {
                        if (body.Substring(bindex + 1, 4).StartsWith("meta", StringComparison.CurrentCultureIgnoreCase))
                        {
                            endindex = body.IndexOf('>', bindex);
                        }
                        else
                        {
                            break;
                        }

                    }
                }
                sb.Remove(index, endindex - index + 1);
                sb.Insert(index, meta.TrimEnd());
            }
            return sb.ToString();
        }
        public static StringBuilder Release(String publishContent, String title, String keyword, String description, String tplContent)
        {
            var log = new StringBuilder();



            var publish = ResourceLoader(publishContent);




            var tpl = ResourceLoader(tplContent);
            if (publish.Children.Count >= tpl.Children.Count)
            {
                int lastIndex = 0;
                for (var i = 0; i < tpl.Children.Count; i++)
                {
                    if (tpl.Children[i].IsBegin)
                    {
                        tpl.Children[i].Text = publish.Children[i].Text;
                        lastIndex = i;

                    }
                }
                if (String.IsNullOrEmpty(title) == false)
                {
                    var titles = new StringBuilder();
                    titles.AppendFormat("<title>{0}</title>", title);
                    titles.AppendFormat("<meta name=\"keywords\" content=\"{0}\">", keyword.Replace("\"", "&quot;"));//
                    titles.AppendFormat("<meta name=\"description\" content=\"{0}\">", description.Replace("\"", "&quot;"));

                    tpl.Children[0].Text = AppendTitle(tpl.Children[0].Text, titles.ToString());
                }
                var tts = UMC.Data.Utility.TimeSpan();

                var sb = new System.IO.StringWriter(log);
                int index = 0;
                var regex = new System.Text.RegularExpressions.Regex("(?<key>\\shref|\\ssrc)=\"(?<src>[^\"]+)\"");
                foreach (var t in tpl.Children)
                {
                    if (t.IsBegin)
                        sb.WriteLine("<!--%publish {0}%-->", tts, t.Key);
                    sb.WriteLine(regex.Replace(t.Text.Trim(), g =>
                    {
                        var src = g.Groups["src"].Value;
                        if (src.IndexOf(':') == -1)
                        {
                            switch (src[0])
                            {
                                case '{':
                                case '/':
                                case '#':
                                    break;
                                default:
                                    src = "/" + src;
                                    break;

                            }
                        }
                        return String.Format("{0}=\"{1}\"", g.Groups["key"], src);
                    }));
                    if (t.IsBegin)
                    {
                        if (lastIndex == index && publish.Children.Count > tpl.Children.Count)
                        {
                            var pt = publish.Children[publish.Children.Count - 1];
                            if (String.Equals(pt.Key, "js", StringComparison.CurrentCulture))
                            {
                                sb.WriteLine(pt.Text.Trim());
                            }
                        }

                        sb.WriteLine("<!--%end%-->");
                    }
                    index++;
                }
                sb.Flush();

            }
            return log;
        }

        static TextTpl ResourceLoader(string input)
        {
            var tp = new TextTpl();
            tp.Children = new List<TextTpl>();
            var stack = new System.Collections.Generic.Stack<TextTpl>();

            stack.Push(tp);
            MatchCollection matchs = resourceName.Matches(input);
            int startIndex = 0;

            foreach (Match match in matchs)
            {

                Group group = match.Groups["resourceName"];
                if (group != null)
                {
                    string resourceName = group.Value.Trim();

                    if (resourceName.StartsWith("publish", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var p = stack.Peek();
                        var pt = new TextTpl();
                        pt.Text = input.Substring(startIndex, match.Index - startIndex);
                        pt.Children = new List<TextTpl>();
                        p.Children.Add(pt);
                        var tpl = new TextTpl();
                        tpl.IsBegin = true;
                        tpl.Key = resourceName.Substring(7).Trim();
                        tpl.Children = new List<TextTpl>();
                        p.Children.Add(tpl);

                        stack.Push(tpl);
                    }
                    else if (resourceName.StartsWith("end", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var resText = input.Substring(startIndex, match.Index - startIndex);
                        var p = stack.Pop();
                        p.Text = resText;
                    }
                    startIndex = match.Index + match.Length;
                }
            }

            if (startIndex < input.Length)
            {
                var pt = new TextTpl();
                pt.Text = input.Substring(startIndex);
                pt.Children = new List<TextTpl>();
                tp.Children.Add(pt);
            }
            if (stack.Count > 1)
            {
                tp.Text = "未配置正确请求标签,请检查";
            }
            return tp;
        }


    }
}
