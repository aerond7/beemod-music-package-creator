using System.Reflection;
using System.Text;

namespace BMPC.Core
{
    public static class Utils
    {
        public static void CreateDirectoryIfMissing(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static string ConvertToSafeFileName(string val)
        {
            string result = val;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(c.ToString(), string.Empty);
            }

            result = result.Replace(" ", string.Empty)
                           .Replace(",", string.Empty)
                           .Replace(";", string.Empty)
                           .Replace("'", string.Empty)
                           .Replace(".", string.Empty)
                           .ToLower()
                           .Trim();

            return result;
        }

        public static string EscapeString(string input)
        {
            if (input == null) return string.Empty;

            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        // Escape everything outside normal printable ASCII
                        if (c < 0x20 || c > 0x7E)
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static string GetAppVersion()
        {
            return "v" + Assembly.GetExecutingAssembly().GetName().Version!.ToString(3);
        }
    }
}
