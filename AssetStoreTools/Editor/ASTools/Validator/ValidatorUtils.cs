using System.IO;
using System.Text;

namespace ASTools.Validator
{
    public class ValidatorUtils
    {
        public static bool IsMixamoFbx(string fbxPath)
        {
            FileStream fileStream = new FileStream(fbxPath, FileMode.Open);
            if (fileStream.Length < 1218L)
            {
                return false;
            }
            byte[] array = new byte[10];
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                binaryReader.BaseStream.Seek(1218L, SeekOrigin.Begin);
                binaryReader.Read(array, 0, 10);
            }
            string @string = Encoding.ASCII.GetString(array);
            return @string.Contains("mixamo");
        }
    }
}
