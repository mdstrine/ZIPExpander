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
        //theres a file naming problem rn, if the target already has a file with the name it will be overwitten, even if it was just decompressed
        //appending is difficult becuase the file name isn't created until during the unzip. 
        //could compare the directory to see if any files with the name appear and append, BUT if we try to overwrite a target which has 
        //already been written to before its going to rename every single previously decompressed file...
        public static async Task<string> RunDecompressor(IProgress<int> progress, IProgress<int> progressWorking, IProgress<string> progressFile, IProgress<string> progressWorkingFile, string sourcePath, string targetPath)
        {
            //If its a .zip decompress it
            if (Path.GetExtension(sourcePath) == ".zip")
            {
                try
                {
                    //need to check the size of the file or make sure it's actually a zip?
                    if(new FileInfo(sourcePath).Length == 0)
                    {
                        return "";
                    }

                    //instanciate a zipfile instanace and open the file for read
                    using ZipFile zip = ZipFile.Read(sourcePath);
                   
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

                    try
                    {
                        //run the actual task
                        await Task.Run(() => zip.ExtractAll(targetPath, ExtractExistingFileAction.OverwriteSilently));
                        return targetPath;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.ToString());
                    }

                }
                //bubble exceptions up to the UI thread
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }

            }

            //else if its a .gz decompress gz
            else if (Path.GetExtension(sourcePath) == ".gz")
            {
                try
                {
                    //fix the target path as this library requires the file name in the target path
                    string targetPathFixed = targetPath + "\\" + Path.GetFileNameWithoutExtension(sourcePath);
                    //change the UI to show the current file being worked on
                    //theres no simple way to calculate a progress bar with this library
                    progressFile.Report(sourcePath);
                    progressWorkingFile.Report(sourcePath);
                    //open the input and output files then direct the stream to decompress
                    using (var input = File.OpenRead(sourcePath))
                    using (var output = File.OpenWrite(targetPathFixed))
                    using (var gz = new GZipStream(input, CompressionMode.Decompress))
                    {
                        await gz.CopyToAsync(output);
                    }
                    return targetPathFixed;
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
                return "";
            }



        }

    }
}
