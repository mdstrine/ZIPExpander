using Ionic.Zip;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ZipFile = Ionic.Zip.ZipFile;


namespace ZIPExpander
{
    internal class Decompressor
    {

        public static async Task<bool> RunDecompressor(IProgress<int> progress, IProgress<int> progressWorking, IProgress<string> progressFile, IProgress<string> progressWorkingFile, string SourcePath, string TargetPath)
        {
            //If its a .zip decompress zip
            if (Path.GetExtension(SourcePath) == ".zip")
            {
                try
                {

                    using ZipFile zip = ZipFile.Read(SourcePath);
                    zip.ExtractProgress += (sender, e) =>
                    {
                        if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
                        {
                            progressWorking.Report((int)(1.0d / e.TotalBytesToTransfer * e.BytesTransferred * 100.0d));
                            progressWorkingFile.Report(e.CurrentEntry.ToString());
                        }
                        if (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry)
                        {
                            progress.Report((int)(1.0d / e.EntriesTotal * e.EntriesExtracted * 100.0d));
                            progressFile.Report(e.ArchiveName.ToString());
                        }

                    };
                    await Task.Run(() => zip.ExtractAll(TargetPath, ExtractExistingFileAction.OverwriteSilently));
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }

            }

            //else if its a .gz decompress gz
            else if (Path.GetExtension(SourcePath) == ".gz")
            {
                try
                {
                    string TargetPathFixed = TargetPath + "\\" + Path.GetFileNameWithoutExtension(SourcePath);
                    progressFile.Report(SourcePath);
                    progressWorkingFile.Report(SourcePath);
                    using (var input = File.OpenRead(SourcePath))
                    using (var output = File.OpenWrite(TargetPathFixed))
                    using (var gz = new GZipStream(input, CompressionMode.Decompress))
                    {
                        await gz.CopyToAsync(output);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }

            //is a directory, do directory things
            else if (Directory.Exists(SourcePath))
            {
                //TO DO
                return false;
            }

            //is not a .zip or .gz or a folder do nothing?
            else
            {
                //TO DO
                return false;
            }



        }

    }
}
