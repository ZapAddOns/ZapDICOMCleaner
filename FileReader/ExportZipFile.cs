using FellowOakDicom;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ZapDICOMCleaner.FileReader
{
    static class ExportZipFile
    {
        public static bool IsZapExportFile(string zipFilePath)
        {
            if (!File.Exists(zipFilePath))
            {
                return false;
            }

            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                if (archive == null)
                {
                    return false;
                }

                var dicomFile = archive.Entries.Select(e => e.Name).FirstOrDefault(n => n.EndsWith("_RTSTRUCT000000.dcm"));

                if (dicomFile == null)
                {
                    return false;
                }

                dicomFile = dicomFile.Trim().Replace(".dcm", "");

                // Check for all other files

                return true;
            }
        }

        public static DicomFile ReadDICOMRTStructFile(string zipFilePath)
        {
            if (!IsZapExportFile(zipFilePath))
            {
                return null;
            }

            using (var archive = ZipFile.OpenRead(zipFilePath))
            {
                var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith("_RTSTRUCT000000.dcm"));

                using (var stream = entry.Open())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        // Deflate compressed data
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        var dicom = DicomFile.OpenAsync(memoryStream);

                        dicom.Wait();

                        return dicom.Result;
                    }
                }
            }
        }

        public static void WriteDICOMRTStructFile(string zipFilePath, DicomFile file)
        {
            if (!IsZapExportFile(zipFilePath))
            {
                return;
            }

            using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
            {
                foreach (var entry in archive.Entries.Where(e => e.Name.EndsWith("_RTSTRUCT000000.dcm")))
                {
                    using (var stream = entry.Open())
                    {
                        stream.SetLength(0);

                        file.Save(stream);
                    }
                }
            }
        }
    }
}
