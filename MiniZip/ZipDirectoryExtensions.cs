using System.Text;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Extension methods for <see cref="ZipDirectory"/>.
    /// </summary>
    public static class ZipDirectoryExtensions
    {
        /// <summary>
        /// Determines the comment of the <see cref="ZipDirectory"/> by decoding the comment bytes. Uses the default code page.
        /// </summary>
        /// <param name="directory">The ZIP driectory.</param>
        public static string GetComment(this ZipDirectory directory)
        {
            return directory.GetComment(encoding: null);
        }

        /// <summary>
        /// Determines the full path name of the <see cref="CentralDirectoryHeader"/> by decoding the name bytes with the provided
        /// encoding.
        /// </summary>
        /// <param name="directory">The ZIP driectory.</param>
        /// <param name="encoding">The encoding to decode the bytes with.</param>
        public static string GetComment(this ZipDirectory directory, Encoding encoding)
        {
            if (encoding == null)
            {
                encoding = Encoding.GetEncoding(codepage: 0);
            }

            return encoding.GetString(directory.Comment);
        }
    }
}
