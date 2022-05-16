using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.IO;
using System.Threading.Tasks;



namespace ZIPExpander
{
    //This code runs the decompression of a single file and reports progress through interfaces. 
    //theres a potential file naming problem, if the target already has a file with the name it will be overwitten.
    //This doesn't matter in many cases I think as I decompress into a new folder with "_Extracted" as a postfix and items within a compressed file should not have the same name.
    internal class Decompressor
    {
        public static async Task<string> RunDecompressor(IProgress<int> progress, IProgress<int> progressWorking, IProgress<string> progressFile, IProgress<string> progressWorkingFile, string sourcePath, string targetPath)
        {
            //report the name of the compressed folder we are working on now.
            progressFile.Report(sourcePath);

            //if the file has 0 length, stop and return nothing to prevent errors
            if (new FileInfo(sourcePath).Length == 0)
            {
                return "";
            }


            if (Path.GetExtension(sourcePath) == ".7z")
            //When handling .7z files I found I needed to specifically tell the library to open them as a .7z
            {
                try
                {
                    using var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(sourcePath);
                    using (var reader = archive.ExtractAllEntries())
                    {
                        //set up entry progress reporting
                        reader.EntryExtractionProgress += (sender, e) =>
                        {
                            if (e.ReaderProgress != null)
                            {
                                progressWorking.Report(e.ReaderProgress.PercentageRead);
                            }
                        };

                        //report which entry is being worked on
                        progressWorkingFile.Report(sourcePath);
                        try
                        {
                            await Task.Run(() => reader.WriteAllToDirectory(targetPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true }));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }

            else
            {
                if (Path.GetExtension(sourcePath) == ".gz")
                //when handling GZ files with this library the files must have the name in the gz file's header to use the standard "writeAllToDirectory" method. Many linux created GZ files do not have this information and fail to extract
                //since a GZ only has one entry per GZ file, I just open the gz, move to the next entry and use "writeentrytofile" while specifying the output file name to be the same as the input file name
                {
                    try
                    {
                        using var archive = SharpCompress.Archives.GZip.GZipArchive.Open(sourcePath);
                        using (var reader = archive.ExtractAllEntries())
                        {
                            //set up entry progress reporting
                            reader.EntryExtractionProgress += (sender, e) =>
                            {
                                if (e.ReaderProgress != null)
                                {
                                    progressWorking.Report(e.ReaderProgress.PercentageRead);
                                }
                            };

                            while (reader.MoveToNextEntry())
                            {
                                //since a GZ only has one file we can just report the sourcePath as what's being worked on
                                progressWorkingFile.Report(sourcePath);
                                try
                                {
                                    string fullTargetPath = targetPath + "\\" + Path.GetFileNameWithoutExtension(sourcePath);
                                    await Task.Run(() => reader.WriteEntryToFile(fullTargetPath, new ExtractionOptions() { Overwrite = true }));
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.ToString());
                                }

                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.ToString());
                    }
                }

                else if (Path.GetExtension(sourcePath) != ".7z" && Path.GetExtension(sourcePath) != ".gz")
                //if its any other type of compressed file the library seems to figure it out reliably 
                {
                    try
                    {
                        using (Stream stream = File.OpenRead(sourcePath))
                        using (var reader = ReaderFactory.Open(stream))
                        {
                            //set up entry progress reporting
                            reader.EntryExtractionProgress += (sender, e) =>
                            {
                                if (e.ReaderProgress != null)
                                {
                                    progressWorking.Report(e.ReaderProgress.PercentageRead);
                                }
                            };

                            while (reader.MoveToNextEntry())
                            {

                                if (!reader.Entry.IsDirectory)
                                {
                                    //report which entry is being worked on
                                    progressWorkingFile.Report(reader.Entry.Key);
                                    try
                                    {
                                        await Task.Run(() => reader.WriteEntryToDirectory(targetPath, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true }));
                                        //calculate progress of entire compressed file and report
                                        progress.Report((int)(1.0d / stream.Length * stream.Position * 100.0d));
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception(ex.ToString());
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.ToString());
                    }
                }

            }
            //I return the target path of the decompressed item to be used later
            return targetPath;
        }
    }
}


