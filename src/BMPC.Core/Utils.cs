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

        /// <summary>
        /// Best-effort recursive delete. Entries that cannot be deleted (e.g. files locked by another process)
        /// are skipped instead of throwing.
        /// </summary>
        /// <returns><c>true</c> if the directory no longer exists; <c>false</c> if anything was left behind.</returns>
        public static bool TryDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return true;
                }

                // Never follow links (symlinks/junctions) into their targets; only remove the link itself.
                if (new DirectoryInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    Directory.Delete(path);
                    return true;
                }

                var success = true;
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        success = false;
                    }
                }

                foreach (var directory in Directory.GetDirectories(path))
                {
                    success &= TryDeleteDirectory(directory);
                }

                if (success)
                {
                    Directory.Delete(path);
                }

                return success;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
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

        /// <summary>
        /// Returns the first non-ASCII character (or surrogate pair) in <paramref name="value"/>, or <c>null</c> if there is none.
        /// </summary>
        public static string? FindNonAsciiCharacter(string? value)
        {
            if (value == null) return null;

            foreach (var rune in value.EnumerateRunes())
            {
                if (!rune.IsAscii)
                {
                    return rune.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// BEE2 can't read non-ASCII text in info.txt (it shows <see cref="EscapeString"/>'s <c>\uXXXX</c> escapes as-is),
        /// so text written there must be ASCII.
        /// </summary>
        /// <returns>A user-facing error message, or <c>null</c> if <paramref name="value"/> is ASCII only.</returns>
        public static string? GetNonAsciiTextError(string fieldName, string? value)
        {
            var character = FindNonAsciiCharacter(value);
            if (character == null) return null;

            return $"{fieldName} contains \"{character}\", which BEEmod can't display. Use only English letters, numbers and basic symbols (no accented letters, emoji or other special characters).";
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
