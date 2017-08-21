using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResizePicZip
{
    class Program
    {
        private static readonly long quality = 100;
        private static EncoderParameters eps;
        private static EncoderParameter ep;
        private static ImageCodecInfo ici;

        static void Main(string[] args)
        {
            var currDir = System.Environment.CurrentDirectory;
            var list = Directory.EnumerateFiles(currDir, "*.zip").ToList();
            if (list == null || list.Count <= 0)
            {
                Console.WriteLine("Not found Zip file");
                return;
            }
            JpegQualitySetting(quality);
            list.ForEach(file => changeSizeImageInZipFile(file));
        }

        private static void JpegQualitySetting(long quality)
        {
            //EncoderParameterオブジェクトを1つ格納できる
            //EncoderParametersクラスの新しいインスタンスを初期化
            //ここでは品質のみ指定するため1つだけ用意する
            eps = new EncoderParameters(1);
            //品質を指定
            ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
            //EncoderParametersにセットする
            eps.Param[0] = ep;
            //イメージエンコーダに関する情報を取得する
            ici = GetEncoderInfo("image/jpeg");
        }

        //MimeTypeで指定されたImageCodecInfoを探して返す
        private static ImageCodecInfo GetEncoderInfo(string mineType)
        {
            //GDI+ に組み込まれたイメージ エンコーダに関する情報をすべて取得
            return ImageCodecInfo.GetImageEncoders().ToList().Find(enc => enc.MimeType == mineType);
        }

        //ImageFormatで指定されたImageCodecInfoを探して返す
        private static ImageCodecInfo GetEncoderInfo(System.Drawing.Imaging.ImageFormat f)
        {
            return ImageCodecInfo.GetImageEncoders().ToList().Find(enc => enc.FormatID == f.Guid);
        }

        static string[] ext = { ".jpg", ".jpeg", ".jpe" };

        private static void changeSizeImageInZipFile(string FileName)
        {
            Console.WriteLine("***** " + Path.GetFileName(FileName) + " *****");
            using (var zto = new FileStream(FileName, FileMode.Open))
            using (var zipArc = new ZipArchive(zto, ZipArchiveMode.Update))
            {
                zipArc.Entries.Where(file => ext.Contains(Path.GetExtension(file.Name).ToLower())).ToList().ForEach(zipEntity =>
                {
                    var imageFileName = zipEntity.FullName;
                    Console.Write(zipEntity.Name);
                    using (var stream = zipEntity.Open())
                    {
                        Image img = Image.FromStream(stream);

                        //描画先とするImageオブジェクトを作成する
                        Bitmap canvas = new Bitmap((1920 - (138 * 2)), 1200);
                        //ImageオブジェクトのGraphicsオブジェクトを作成する
                        Graphics g = Graphics.FromImage(canvas);
                        var srcRect = new Rectangle(138, 0, (1920 - (138 * 2)), 1200);
                        var destRect = new Rectangle(0, 0, (1920 - (138 * 2)), 1200);
                        g.DrawImage(img, destRect, srcRect, GraphicsUnit.Pixel);
                        g.Dispose();

                        var memStream = new MemoryStream();
                        canvas.Save(memStream, ici, eps);
                        long len = memStream.Length;
                        int baseSize = int.MaxValue;
                        int offset = 0;
                        var buf = memStream.ToArray();
                        var entry = zipArc.CreateEntry(imageFileName);
                        using (var writer = entry.Open())
                        {
                            while (len > 0)
                            {
                                int wlen = len > baseSize ? baseSize : (int)len;
                                writer.Write(buf, offset, wlen);
                                len -= wlen;
                                offset += wlen;
                            }
                        }
                    }

                    zipEntity.Delete();
                    Console.WriteLine(" ... Complete!");
                });
            }
            Console.WriteLine(Path.GetFileName(FileName) + " ... All Complete!");
        }
    }
}
