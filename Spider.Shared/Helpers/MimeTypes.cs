using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spider.Shared.Helpers
{
    /// <summary>
    /// FT: In Angular there is an built in class MimeType
    /// </summary>
    public class MimeTypes
    {
        private MimeTypes(string value) { Value = value; }

        public string Value { get; private set; }

        public static MimeTypes Pdf { get { return new MimeTypes("application/pdf"); } }
        public static MimeTypes Zip { get { return new MimeTypes("application/zip"); } }

        public static MimeTypes Jpeg { get { return new MimeTypes("image/jpeg"); } }
        public static MimeTypes Png { get { return new MimeTypes("image/png"); } }
        public static MimeTypes Svg { get { return new MimeTypes("image/svg"); } }
        public static MimeTypes Webp { get { return new MimeTypes("image/webp"); } }

        public override string ToString()
        {
            return Value;
        }

        public MimeTypes GetMimeTypeForTheFileName(string fileName)
        {
            string fileExtension = fileName.Split('.').LastOrDefault();

            switch (fileExtension)
            {
                case ".pdf":
                    return Pdf;
                case ".zip":
                    return Zip;
                case ".jpeg":
                    return Jpeg;
                case ".jpg":
                    return Jpeg;
                case ".png":
                    return Png;
                case ".svg":
                    return Svg;
                case ".webp":
                    return Webp;
                default:
                    throw new NotSupportedException($"We don't support this file extension ({fileExtension}), for the file ({fileName}).");
            }
        }

    }
}
