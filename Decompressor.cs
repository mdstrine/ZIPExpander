using Ionic.Zip;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ZipFile = Ionic.Zip.ZipFile;


namespace ZIPExpander
{
    //This code runs the decompression of a single file, .zip or .gz async to allow the UI thread to update.
    //progress is reported to the UI through interfaces.
    //dotnetzip libraries are used for zip due to its easy progress reporting.
    //System.IO.Compression is used for gzip.
    //Return type is Task<bool>, bool being used to determine if a file was decompressed or not. 
    //Basically if there is a problem during extraction the return will be false and a few post
    //extraction methods won't be ran (like closing the UI)
    internal class Decompressor
    {

        public static async Task<bool> RunDecompressor(IProgress<int> progress, IProgress<int> progressWorking, IProgress<string> progressFile, IProgress<string> progressWorkingFile, string SourcePath, string TargetPath)
        {
            //If its a .zip decompress it
            if (Path.GetExtension(SourcePath) == ".zip")
            {
                try
                {
                    //instanciate a zipfile instanace and open the file for read
                    using ZipFile zip = ZipFile.Read(SourcePath);

                    //collect events from dotnetzip for progress reporting
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

                    //run the actual task
                    await Task.Run(() => zip.ExtractAll(TargetPath, ExtractExistingFileAction.OverwriteSilently));
                    return true;

                }
                //bubble exceptions up to the UI thread
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
                    //fix the target path as this library requires the file name in the target path
                    string TargetPathFixed = TargetPath + "\\" + Path.GetFileNameWithoutExtension(SourcePath);
                    //change the UI to show the current file being worked on
                    //theres no simple way to calculate a progress bar with this library
                    progressFile.Report(SourcePath);
                    progressWorkingFile.Report(SourcePath);
                    //open the input and output files then direct the stream to decompress
                    using (var input = File.OpenRead(SourcePath))
                    using (var output = File.OpenWrite(TargetPathFixed))
                    using (var gz = new GZipStream(input, CompressionMode.Decompress))
                    {
                        await gz.CopyToAsync(output);
                    }
                    return true;
                }
                //bubble exceptions up to the UI thread
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }

            //is not a .zip or .gz, do nothing.
            else
            {
                return false;
            }



        }

    }
}
