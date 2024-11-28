using System.Text;

namespace CardLib
{
    public static class Extensions
    {
        internal static int[] CSVIndexes(this IEnumerable<char> chars)
        {
            int i = 0;
            char value = ',';
            bool IsQuoted = false;
            List<int> list = new List<int>();
            foreach (char c in chars)
            {
                if (c == value && !IsQuoted)
                {
                    list.Add(i);
                }
                if (c == '"')
                {
                    IsQuoted = !IsQuoted;
                }
                i++;
            }
            return list.ToArray();
        }

        public static string[] SplitCSV(this string str)
        {
            int[] splits = str.CSVIndexes();
            var begin = 0;
            var list = new List<string>();
            foreach (var i in splits)
            {
                if (begin == i)
                {
                    list.Add(string.Empty);
                    begin++;
                }
                else
                {
                    list.Add(str.Substring(begin, i - begin).Replace("\"", string.Empty));
                    begin = i + 1;
                }
            }
            if (begin < str.Length - 1)
            {
                list.Add(str.Substring(begin, str.Length - begin).Replace("\"", string.Empty));
            }
            else list.Add(string.Empty);
            return list.ToArray();
        }

        public static string Readln(this Stream stream)
        {
            if (stream == null || !stream.CanRead)
            {
                throw new InvalidOperationException("Stream not readable.");
            }

            StringBuilder sb = new StringBuilder();
            int read;
            while ((read = stream.ReadByte()) != -1)
            {
                var rune = (char)read;
                if (rune == '\n')
                    break;
                if (rune != '\r')
                    sb.Append(rune);
            }
            return sb.ToString();
        }

        public static Dictionary<int, int> ToDict(this IEnumerable<(int x, int y)> items)
        {
            Dictionary<int, int> dict = new(items.Count());
            foreach (var item in items)
            {
                dict.Add(item.x, item.y);
            }
            return dict;
        }
    }
}
