using System.Text;
using System.Xml.Linq;

namespace Python_Interpretation.PythonScript
{
    internal class Decoder
    {
        public static void DecodeErrorDataAndSaveToFile(string encodedXml)
        {
            byte[] decodedBytes = Convert.FromBase64String(encodedXml);

            string decodedXmlStr = Encoding.UTF8.GetString(decodedBytes);

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FailureData");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = Path.Combine(folderPath, $"DecodedError_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            try
            {
                XDocument xmlDoc = XDocument.Parse(decodedXmlStr);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Decoded XML:");

                bool categoriesFound = false;

                foreach (var category in xmlDoc.Descendants())
                {
                    if (category.Name == "CME" || category.Name == "FLR" || category.Name == "GST")
                    {
                        categoriesFound = true;

                        sb.AppendLine($"Category: {category.Name}");

                        foreach (var record in category.Descendants("record"))
                        {
                            string? date = record.Element("date")?.Value;
                            string? value = record.Element("value")?.Value;

                            sb.AppendLine($"Date: {date}, Value: {value}");
                        }
                    }
                }

                if (!categoriesFound)
                {
                    sb.AppendLine("Категории не найдены в XML.");
                }

                File.WriteAllText(fileName, sb.ToString());
            }
            catch (System.Xml.XmlException)
            {
                File.WriteAllText(fileName, $"Decoded Error Message: {decodedXmlStr}");
            }
        }
    }
}
