using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;


namespace ZIPExpander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ZIPExpanderWindow : Window
    {
        public ZIPExpanderWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TargetTextBx_TextChanged(object sender, TextChangedEventArgs e)
        {
            //changes the text style from default gray/italic to regular once the path has changed
            TargetTextBx.Foreground = new SolidColorBrush(Colors.Black);
            TargetTextBx.FontStyle = new FontStyle();
        }

        private void SourceTextBx_TextChanged(object sender, TextChangedEventArgs e)
        {
            //changes the text style from default gray/italic to regular once the path has changed
            SourceTextBx.Foreground = new SolidColorBrush(Colors.Black);
            SourceTextBx.FontStyle = new FontStyle();
        }

        private void SourceBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            //open a Windows API file dialog in folder picker mode for source file selection. This lets us select zip files also when using AllowNonFileSystemItems, thankfully.
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select a .zip file or folder to extract from (Double clicking a .zip or folder will browse into that folder, click \"Select Folder\" to choose it)",
                InitialDirectory = SourceTextBx.Text,
                IsFolderPicker = true,
                AllowNonFileSystemItems = true
            };

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                SourceTextBx.Text = dialog.FileName;
                TargetTextBx.Text = Path.ChangeExtension(dialog.FileName, null); //set target text box path 
                if (!Path.HasExtension(SourceTextBx.Text)) //if it's not a zip file add _expanded to the target file name so we don't extract to the source folder by default
                {
                    TargetTextBx.Text += "_expanded";
                }

            }

        }

        private void TargetBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            //open a windows API file picker in folder mode, not using validation here, will validate later.
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select a target folder for extraction. If you wish to use a new folder from this view you must first create it from the right-click menu",
                IsFolderPicker = true,
                EnsurePathExists = false,
                EnsureFileExists = false,
                EnsureValidNames = false
            };

            //Try to open the path entered in the box if the box isnt blank
            if (!(TargetTextBx.Text == ""))
            {
                System.IO.DirectoryInfo? directoryInfo = System.IO.Directory.GetParent(TargetTextBx.Text);
                if (directoryInfo != null)
                {
                    dialog.InitialDirectory = directoryInfo.ToString();
                }
            }

            //use the entered filename for the dialog
            if (TargetTextBx.Text != "Click Browse...")
            {
                dialog.DefaultFileName = Path.GetFileName(TargetTextBx.Text);
            }

            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                TargetTextBx.Text = dialog.FileName;
            }
        }


        //!!!!! This is the main exectuion of the program
        private async void ExtractBtn_Click(object sender, RoutedEventArgs e)
        {

            //set inputs to local variables
            string sourceTextPath = SourceTextBx.Text;
            string targetTextPath = TargetTextBx.Text;
            bool openFldrBoxChkd = Convert.ToBoolean(ShowFilesBx.IsChecked);

            //validate the source and target before extraction, throw errors if needed, etc.
            bool valid = Utils.ValidateSourceAndTarget(sourceTextPath, targetTextPath);

            if (valid)
            {
                //disable the extract button so multiple processes aren't started
                ExtractBtn.IsEnabled = false;

                //open a new window with progress bars
                ProgressWindow progressWindow = new()
                {
                    Owner = this
                };
                progressWindow.CurrentItemProgBar.Maximum = 100;
                progressWindow.AllItemsProgBar.Maximum = 100;               
                progressWindow.Show();

                //handlers for progress reports
                var progressCur = new Progress<int>(v => progressWindow.CurrentItemProgBar.Value = v);
                var progressWorking = new Progress<int>(v => progressWindow.WorkingProgBar.Value = v);
                var progressFileName = new Progress<string>(v => progressWindow.CurProgTxt.Text = v);
                var progressWorkingFileName = new Progress<string>(v => progressWindow.WorkingProgTxt.Text = v);


                try
                {
                    string decompressResult = "";
                    string decompressLoopResult = "";
                    int numCompressedItemsProcessed = 0;
                    int numUnCompressedItemsProcessed = 0;
                    int numTotalItemsProcessed = 0;
                    int numItemsLeft = 0;
                    bool copyItems = false;
                    bool deleteFromDecompressedItems = false;
                    List<string> decompressedCompressedItemList = new();

                    //set the task bar icon to report progress also
                    this.taskBarItemInfo1.ProgressState = TaskbarItemProgressState.Normal;

                    //if the source file is a .zip, extract it to the target first.
                    //the target is now the new source and don't need to copy any uncompressed items
                    //also need to delete compressed files extracted from the new source in this case
                    //(could I open a stream to this file and decompress items from that stream, rather than decompressing this whole item initally?)
                    if (Path.GetExtension(sourceTextPath) == ".zip")
                    {
                        progressWindow.OverallTxt.Text = "Unzipping source file...";

                        //run decompressor once to get our inital .zip extracted 
                        try
                        {
                            await Task.Run(() => Decompressor.RunDecompressor(progressCur, progressWorking, progressFileName, progressWorkingFileName, sourceTextPath, targetTextPath));
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.ToString());
                        }

                        //extracted files are now the source to expaand the rest
                        sourceTextPath = targetTextPath;
                        copyItems = false;
                    }

                    else
                    {
                        //if it wasnt a zip we need to copy uncompressed files to the target so we set this flag true
                        copyItems = true;
                    }

                    //make an instance of the filefinder class to locate all compressed files and uncompressed files in the source
                    FileFinder fileFinder = new();

                    //fill lists
                    fileFinder.GetListofItems(sourceTextPath);

                    //access the lists in the class and copy to local variables
                    List<string> compressedItemList = fileFinder.AllFoundCompressedFiles;
                    List<string> uncompressedItemList = fileFinder.AllFoundUncompressedFiles;

                    //make a list for all the compressed items found and copy the files found initally
                    //remove any compressed items that happen to be in the uncompressed items list
                    List<string> uncompressedItemListScrubbed = uncompressedItemList.Except(compressedItemList).ToList();

                    //get file counts for progress reports
                    int numCompressedItems = compressedItemList.Count;
                    int numUnCompressedItems = uncompressedItemListScrubbed.Count;
                    int numTotalItems = numCompressedItems;

                    //prevent odd behavior if nothing to decompress is found in the given .zip/folder
                    if (numCompressedItems == 0)
                    {

                        MessageBox.Show(string.Format("No compressed items were found within in the source \r\n\r\nIf the source was a .zip, it was extracted to the target, however no further compressed items were found."), "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        progressWindow.Close();

                    }

                    //if entered source and target folders are exactly the same, don't copy files as we cant make a copy of an existing file and place it in the same folder
                    if (string.Equals(SourceTextBx.Text, TargetTextBx.Text, StringComparison.CurrentCultureIgnoreCase))
                    {
                        copyItems = false;
                    }

                    //if copying files, add them to the total item count for progress reporting
                    if (copyItems)
                    {
                        numTotalItems += uncompressedItemListScrubbed.Count;
                    }

                    //Set text progress manually, before it gets reported
                    progressWindow.OverallTxt.Text = String.Format("0 of {0} compressed items processed... \r\n0 of {1} total items processed...", numCompressedItems, numTotalItems);

                    //make copies of global variables that may be changed within the following do loop
                    //idea being first time thru the do loop, the values are as if the do loop didnt exist
                    //these variables are modified when compressed items are found within the decompressed items
                    //however the original values are kept as they are still needed for later operations
                    string doLoopSourceTextPath = sourceTextPath;
                    List<string> doLoopCompressedItemList = new(compressedItemList);

                    //itterate over every compressed item found
                    //do while loop ensures if any embedded compressed files are decompressed, that those files will also be decompressed
                    do
                    {

                        List<string> stillCompressedItemList = new();

                        for (int i = 0; i < doLoopCompressedItemList.Count; i++)
                        {
                            string compressedItem = doLoopCompressedItemList[i];
                            FileFinder fileFinderDecompressor = new();
                            List<string> stillCompressedItemsThisLoop = new();

                            //create a path for the target item
                            string itemTargetTextPath = Utils.GetTargetTextPathPerItem(compressedItem, doLoopSourceTextPath, targetTextPath, true);

                            //create the target directory if it doesn't exist, allow retrying/skipping
                            if (!Directory.Exists(itemTargetTextPath))
                            {
                                DialogResult result1 = System.Windows.Forms.DialogResult.Retry;
                                while (result1 == System.Windows.Forms.DialogResult.Retry)
                                {
                                    try
                                    {
                                        Directory.CreateDirectory(itemTargetTextPath);
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        result1 = MessageBox.Show(string.Format("Error Creatuing folder {0} \r\n \r\n Use \"Ignore\" to skip this file and continue \r\n \r\n", itemTargetTextPath) + ex.ToString(), "Decompression Error", MessageBoxButtons.AbortRetryIgnore);
                                        if (result1 == System.Windows.Forms.DialogResult.Abort) throw;
                                    }
                                }
                            }

                            //run decompression on this item, allow retrying/skipping
                            DialogResult result = System.Windows.Forms.DialogResult.Retry;
                            while (result == System.Windows.Forms.DialogResult.Retry)
                            {
                                try
                                {
                                    decompressResult = await Task.Run(() => Decompressor.RunDecompressor(progressCur, progressWorking, progressFileName, progressWorkingFileName, compressedItem, itemTargetTextPath));
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    result = MessageBox.Show(string.Format("Error Decompressing file: {0} \r\n \r\n Use \"Ignore\" to skip this file and continue \r\n \r\n", compressedItem) + ex.ToString(), "Decompression Error", MessageBoxButtons.AbortRetryIgnore);
                                    if (result == System.Windows.Forms.DialogResult.Abort) throw;
                                }
                            }

                            //if the returned folder is not empty, look for zips in the output folder
                            //NOTE the return from the decompressor task IS NOT the file which was decompressed, just the path
                            //assign the string to this flag for finishing processes
                            decompressLoopResult = decompressResult;

                            //if the returned item is a folder (ie a compressed file which was decompressed into a folder)
                            if (Directory.Exists(decompressResult))
                            {
                                //use filefinder to get a list of compressed items in this decompressed folder
                                fileFinderDecompressor.GetListofItems(decompressResult);

                                //assign filefinder result to a list containg items found during this foreach loop
                                stillCompressedItemsThisLoop = fileFinderDecompressor.AllFoundCompressedFiles;

                                //add the list of items found during this foreach loop to another list in the do loop                       
                                stillCompressedItemList.AddRange(stillCompressedItemsThisLoop);

                                //also add the items to a decompressed item list outside both loops, for deleting the source compressed files later
                                decompressedCompressedItemList.AddRange(stillCompressedItemsThisLoop);

                                //add counts of found items to total progress counters
                                numTotalItems += stillCompressedItemsThisLoop.Count;
                                numCompressedItems += stillCompressedItemsThisLoop.Count;

                            }
                            //increment progress counters
                            numCompressedItemsProcessed++;
                            numTotalItemsProcessed++;

                            //report overall progress based on number of items found and number of items processed already
                            progressWindow.AllItemsProgBar.Value = (1.0d / numTotalItems * numTotalItemsProcessed * 100.0d);
                            this.taskBarItemInfo1.ProgressValue = (1.0d / numTotalItems * numTotalItemsProcessed * 100.0d) / 100;
                            progressWindow.OverallTxt.Text = String.Format("{0} of {1} compressed items processed... \r\n{2} of {3} total items processed...", numCompressedItemsProcessed, numCompressedItems, numTotalItemsProcessed, numTotalItems);


                        }
                        //once the list is thru, assign the returned items to the compressed items list to be decompressed in the next loop
                        doLoopCompressedItemList = stillCompressedItemList;

                        //count the items still compressed for the while loop
                        numItemsLeft = stillCompressedItemList.Count;

                        //set the source path to the target path as we'll be decompressing from the target now?
                        doLoopSourceTextPath = targetTextPath;

                        //set delete items true if there's anything to remove
                        if (numItemsLeft > 0)
                        {
                            deleteFromDecompressedItems = true;
                        }

                    }
                    //while any compressed items are returned we must keep decompressing and start the do loop over again
                    while (numItemsLeft != 0);

                    //!!!!!! At this point all compressed files should be decompressed
                    //I set the progress text to reflect this as the bars/text won't indicate progress otherwise.
                    progressWindow.CurProgTxt.Text = "Decompression done!";
                    progressWindow.WorkingProgTxt.Text = String.Format("{0} of {1} compressed items processed", numCompressedItemsProcessed, numCompressedItems);

                    //if the last item decompressed was successful, do these final tasks
                    if (decompressLoopResult != "")
                    {
                        //if the source is a folder we might need to copy uncompressed items to the target,
                        //since an extraction would normally do so. We finally use the copyItems flag here
                        if (copyItems)
                        {
                            progressWindow.CurProgTxt.Text = "Decompression done! Copying uncompressed files...";
                            //itterate thru and copy every item that wasn't compressed
                            foreach (string uncompressedItem in uncompressedItemListScrubbed)
                            {
                                //build a path for the target dir
                                string itemTargetTextPath = Utils.GetTargetTextPathPerItem(uncompressedItem, sourceTextPath, targetTextPath, false);

                                //skip this itteration if the uncompressed item happens to be a directory
                                FileAttributes attr = File.GetAttributes(uncompressedItem);
                                if (attr.HasFlag(FileAttributes.Directory))
                                {
                                    continue;
                                }

                                //TO DO: check if the file already exists on the target and append the name if so?

                                //run the copy async so we get progress updates
                                DialogResult result = System.Windows.Forms.DialogResult.Retry;
                                while (result == System.Windows.Forms.DialogResult.Retry)
                                {
                                    try
                                    {
                                        //this will create the target item's directory before creating the item as it seems file.copy will not. Allow retry/ignore if issues arise
                                        string? itemTargetDir = Path.GetDirectoryName(itemTargetTextPath);
                                        if (itemTargetDir != null)
                                        {
                                            Directory.CreateDirectory(itemTargetDir);
                                        }
                                        await Task.Run(() => File.Copy(uncompressedItem, itemTargetTextPath, true));
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        result = MessageBox.Show(string.Format("Error Copying file: {0} \r\n \r\n Use \"Ignore\" to skip this file and continue \r\n \r\n", uncompressedItem) + ex.ToString(), "Copy Error", MessageBoxButtons.AbortRetryIgnore);
                                        if (result == System.Windows.Forms.DialogResult.Abort) throw;
                                    }
                                }
                                //increment progress counter during copy
                                numTotalItemsProcessed++;
                                numUnCompressedItemsProcessed++;

                                //set overall progress values for copy
                                progressWindow.AllItemsProgBar.Value = (1.0d / numTotalItems * numTotalItemsProcessed * 100.0d);
                                this.taskBarItemInfo1.ProgressValue = (1.0d / numTotalItems * numTotalItemsProcessed * 100.0d) / 100;
                                progressWindow.OverallTxt.Text = String.Format("{0} of {1} uncompressed files copied\r\n{2} of {3} total items processed", numUnCompressedItemsProcessed, numUnCompressedItems, numTotalItemsProcessed, numTotalItems);
                                progressWindow.WorkingProgTxt.Text = String.Format("{0} of {1} compressed items processed...", numCompressedItemsProcessed, numCompressedItems);

                            }
                            progressWindow.WorkingProgTxt.Text = String.Format("{0} of {1} compressed items processed \r\n{2} of {3} uncomcompressed items copied", numCompressedItemsProcessed, numCompressedItems, numUnCompressedItemsProcessed, numUnCompressedItems);
                        }

                        //if items were not copied because the source was a .zip or source and target were the same folder
                        //then delete any compressed items since they've been decompressed already.
                        if (!copyItems)
                        {
                            foreach (string item in compressedItemList)
                            {
                                DialogResult result = System.Windows.Forms.DialogResult.Retry;
                                while (result == System.Windows.Forms.DialogResult.Retry)
                                {
                                    try
                                    {
                                        File.Delete(item);
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        result = MessageBox.Show(string.Format("Error Deleting file: {0} \r\n \r\n Use \"Ignore\" to skip this file and continue \r\n \r\n", item) + ex.ToString(), "Cleanup Error", MessageBoxButtons.AbortRetryIgnore);
                                        if (result == System.Windows.Forms.DialogResult.Abort) throw;
                                    }
                                }
                            }
                        }

                        //delete compressed files which were extracted from during the do while loop
                        if (deleteFromDecompressedItems)
                        {
                            foreach (string item in decompressedCompressedItemList)
                            {
                                DialogResult result = System.Windows.Forms.DialogResult.Retry;
                                while (result == System.Windows.Forms.DialogResult.Retry)
                                {
                                    try
                                    {
                                        File.Delete(item);
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        result = MessageBox.Show(string.Format("Error Deleting file: {0} \r\n \r\n Use \"Ignore\" to skip this file and continue \r\n \r\n", item) + ex.ToString(), "Cleanup Error", MessageBoxButtons.AbortRetryIgnore);
                                        if (result == System.Windows.Forms.DialogResult.Abort) throw;
                                    }
                                }
                            }
                        }

                        //finally if the box was checked to open the target folder when complete, open it up.
                        if (openFldrBoxChkd)
                        {
                            Utils.OpenDirectory(TargetTextBx.Text);
                        }

                        //report completeion of the decompression
                        progressWindow.HeaderTxt.Text = "Expansion complete";
                        progressWindow.CurProgTxt.Text = "All done!";
                        progressWindow.OverallTxt.Text = String.Format("{0} of {1} total items processed", numTotalItemsProcessed, numTotalItems);
                        progressWindow.CancelBtn.Content = "Exit App";

                        //set progress window to foreground to capture user's attention
                        progressWindow.Activate();

                    }


                }
                // this should catch any exceptions as they bubble up from other threads and report them to this ui thread
                catch (Exception ex)
                {
                    progressWindow.CloseBtn.IsEnabled = true;
                    progressWindow.CancelBtn.Content = "Exit App";
                    progressWindow.OverallTxt.Text = ("Error occured during expansion");
                    MessageBox.Show(string.Format("An error occured during expansion: \r\n \r\n {0}", ex), "Extraction Problem",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //if decompressResult was false because of some problem this code will be exectued 
                progressWindow.CloseBtn.IsEnabled = true;
                this.taskBarItemInfo1.ProgressState = TaskbarItemProgressState.None;
                ExtractBtn.IsEnabled = true;
                this.Activate();

            }

        }

        //close app if cancel clicked
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //show the about page if clicked
        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            AboutBox1 aboutBox = new();
            aboutBox.ShowDialog();
        }


        //below is used for debugging the fileFinder, makes the list of items that are
        //compressed and uncompressed, then outputs to text files in the target directory.
        //Also must uncomment the xaml control.

        //private void DbugMakeListBtn_Click(object sender, RoutedEventArgs e)
        //{

        //    FileFinder fileFinder = new();
        //    fileFinder.GetListofItems(SourceTextBx.Text);
        //    List<string> CompressedItemList = fileFinder.AllFoundCompressedFiles;
        //    List<string> UncompressedItemList = fileFinder.AllFoundUncompressedFiles;

        //    if (!Directory.Exists(TargetTextBx.Text))
        //    {
        //        Directory.CreateDirectory(TargetTextBx.Text);
        //    }

        //    string FullTargetPath1 = TargetTextBx.Text + "\\" + "CompressedItemList.txt";
        //    string FullTargetPath2 = TargetTextBx.Text + "\\" + "UncompressedItemList.txt";
        //    System.IO.File.WriteAllLines(FullTargetPath1, CompressedItemList);
        //    System.IO.File.WriteAllLines(FullTargetPath2, UncompressedItemList);

        //}

        //


    }
}
