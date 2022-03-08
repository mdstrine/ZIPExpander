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
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;


namespace ZIPExpander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
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
                Title = "Select a .zip file or folder to extract from (Double clicking a .zip or folder will browse that folder)",
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
            //open a windows API file picker in folder mode, not using validation here.
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select a target folder",
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

        //This is the main exectuion of the program
        private async void ExtractBtn_Click(object sender, RoutedEventArgs e)
        {
            //set inputs to local variables
            string SourceTextPath = SourceTextBx.Text;
            string TargetTextPath = TargetTextBx.Text;
            bool OpenFldrBoxChkd = Convert.ToBoolean(ShowFilesBx.IsChecked);

            //validate the source and target before extraction, throw errors if needed, etc.
            bool valid = Utils.ValidateSourceAndTarget(SourceTextPath, TargetTextPath);

            if (valid)
            {
                //disable the extract button so multiple processes arent started
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
                    bool decompressResult = false;
                    bool copyItems = false;

                    //if the source file is a .zip, extract it to the target first.
                    //the target is now the new source and don't need to copy any uncompressed items
                    //also need to delete compressed files extracted from the new source in this case
                    if (Path.GetExtension(SourceTextPath) == ".zip")
                    {
                        progressWindow.OverallTextBlk.Text = "Unzipping source file...";
                        //run decompressor once
                        await Task.Run(() => Decompressor.RunDecompressor(progressCur, progressWorking, progressFileName, progressWorkingFileName, SourceTextPath, TargetTextPath));
                        //extracted files are now the source to expaand the rest
                        SourceTextPath = TargetTextPath;
                        copyItems = false;
                    }

                    else
                    {
                        //if it wasnt a zip we need to copy uncompressed files to the target
                        copyItems = true;
                    }

                    //make an instance of the file finder class to locate all compressed files and uncompressed files in the source
                    FileFinder fileFinder = new();
                    //fill lists
                    fileFinder.GetListofItems(SourceTextPath);
                    //access the lists in the class and copy to local variables
                    List<string> CompressedItemList = fileFinder.AllFoundCompressedFiles;
                    List<string> UncompressedItemList = fileFinder.AllFoundUncompressedFiles;
                    //remove any compressed times that happen to be in the uncompressed items list
                    List<string> UncompressedItemListScrubbed = UncompressedItemList.Except(CompressedItemList).ToList();

                    //get file counts for progress reports
                    int compressedItems = CompressedItemList.Count;
                    int unCompressedItems = UncompressedItemListScrubbed.Count;
                    int totalItems = compressedItems;

                    //prevent odd behavior if nothing to copy, probably need to use something
                    //other than an exception but don't want exectuion to continue if this is hit.
                    if (compressedItems == 0)
                    {
                        throw new Exception("Source has nothing to decompress!");
                    }

                    //if entered source and target folders are the same, don't copy files as we cant make a copy of an existing file
                    if (string.Equals(SourceTextBx.Text, TargetTextBx.Text, StringComparison.CurrentCultureIgnoreCase))
                    {
                        copyItems = false;
                    }

                    //if copying files, add them to the total item count for progress reporting
                    if (copyItems)
                    {
                        totalItems += UncompressedItemListScrubbed.Count;
                    }

                    int compressedItemsProcessed = 0;
                    int totalItemsProcessed = 0;
                    progressWindow.OverallTextBlk.Text = String.Format("0 of {0} compressed items processed...", compressedItems);

                    //itterate over every compressed item found
                    foreach (string CompressedItem in CompressedItemList)
                    {
                        //create a path for the target item
                        string ItemTargetTextPath = Utils.GetTargetTextPathPerItem(CompressedItem, SourceTextPath, TargetTextPath, true);
                        //create the target directory if it doesn't exist
                        if (!Directory.Exists(ItemTargetTextPath))
                        {
                            Directory.CreateDirectory(ItemTargetTextPath);
                        }
                        //run decompression on this item
                        decompressResult = await Task.Run(() => Decompressor.RunDecompressor(progressCur, progressWorking, progressFileName, progressWorkingFileName, CompressedItem, ItemTargetTextPath));
                        //increment progress counters
                        compressedItemsProcessed++;
                        totalItemsProcessed++;
                        //report progress
                        progressWindow.AllItemsProgBar.Value = (1.0d / totalItems * totalItemsProcessed * 100.0d);
                        progressWindow.OverallTextBlk.Text = String.Format("{0} of {1} compressed items processed...", compressedItemsProcessed, compressedItems);


                    }
                    
                    //at this point all compressed files are decompressed
                    //I set the progress text to reflect this as the bars/text won't indicate progress otherwise.
                    progressWindow.CurProgTxt.Text = "Done!";
                    progressWindow.WorkingProgTxt.Text = "Waiting for copy to finish...";

                    //if the last item decompressed was successful do these final tasks
                    if (decompressResult)
                    {
                        //if the source is a folder we might need to copy uncompressed items to the target,
                        //since an extraction would normally do so.
                        if (copyItems)
                        {
                            int unCompressedItemsProcessed = 0;
                            //itterate thru and copy every item that wasn't compressed
                            foreach (string uncompressedItem in UncompressedItemListScrubbed)
                            {
                                //build a path for the target dir
                                string ItemTargetTextPath = Utils.GetTargetTextPathPerItem(uncompressedItem, SourceTextPath, TargetTextPath, false);

                                //skip this itteration if the uncompressed item happens to be a directory
                                FileAttributes attr = File.GetAttributes(uncompressedItem);
                                if (attr.HasFlag(FileAttributes.Directory))
                                {
                                    continue;
                                }

                                //this will create the target item's directory before creating the item as it seems file.copy will not.
                                string? ItemTargetDir = Path.GetDirectoryName(ItemTargetTextPath);
                                if (ItemTargetDir != null)
                                {
                                    Directory.CreateDirectory(ItemTargetDir);
                                }

                                //TO DO: check if the file already exists on the target and append the name if so

                                //run the copy async so we get progress updates
                                await Task.Run(() => File.Copy(uncompressedItem, ItemTargetTextPath, true));
                                //increment progress counter
                                totalItemsProcessed++;
                                unCompressedItemsProcessed++;
                                //set progress values
                                progressWindow.AllItemsProgBar.Value = (1.0d / totalItems * totalItemsProcessed * 100.0d);
                                progressWindow.OverallTextBlk.Text = String.Format("{0} of {1} uncompressed files copied...", unCompressedItemsProcessed, unCompressedItems);
                            }
                        }

                        //if items were not copied because the source was a .zip or source and target were the same folder
                        //then delete any compressed items since they've been decompressed already.
                        if (!copyItems)
                        {
                            foreach (string item in CompressedItemList)
                            {
                                File.Delete(item);
                            }
                        }
                        
                        //finally if the box was checked to open the target folder when complete, open it up.
                        if (OpenFldrBoxChkd)
                        {
                            Utils.OpenDirectory(TargetTextBx.Text);
                        }

                        //and close the application since its not needed
                        Close();
                    }


                }
                // this will catch any exceptions as they bubble up from other threads and report them to this ui thread
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("An error occured during extraction: \r\n \r\n {0}", ex), "Extraction Problem",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //if decompressResult was false we don't close the app, instead re-enable the button
                //and close the progress window so the extraction can be attempted again.
                ExtractBtn.IsEnabled = true;
                progressWindow.Close();
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

        //used for debugging the fileFinder, makes the list of items that are
        //compressed and uncompressed then outputs to text files in the target directory.
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



    }
}
