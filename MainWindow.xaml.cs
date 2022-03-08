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
            TargetTextBx.Foreground = new SolidColorBrush(Colors.Black);
            TargetTextBx.FontStyle = new FontStyle();
        }

        private void SourceTextBx_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceTextBx.Foreground = new SolidColorBrush(Colors.Black);
            SourceTextBx.FontStyle = new FontStyle();
        }

        private void SourceBrowseBtn_Click(object sender, RoutedEventArgs e)
        {

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
                if (!Path.HasExtension(SourceTextBx.Text))
                {
                    TargetTextBx.Text += "_expanded";
                }

            }

        }

        private void TargetBrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select a target folder",
                IsFolderPicker = true
            };

            if (!(TargetTextBx.Text == ""))
            {
                System.IO.DirectoryInfo? directoryInfo = System.IO.Directory.GetParent(TargetTextBx.Text);
                if (directoryInfo != null)
                {
                    dialog.InitialDirectory = directoryInfo.ToString();
                }
            }

            dialog.EnsurePathExists = false;
            dialog.EnsureFileExists = false;
            dialog.EnsureValidNames = false;
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

        private async void ExtractBtn_Click(object sender, RoutedEventArgs e)
        {
            string SourceTextPath = SourceTextBx.Text;
            string TargetTextPath = TargetTextBx.Text;
            bool OpenFldrBoxChkd = Convert.ToBoolean(ShowFilesBx.IsChecked);

            //validate the source and target before extraction
            bool valid = Utils.ValidateSourceAndTarget(SourceTextPath, TargetTextPath);

            if (valid)
            {
                ExtractBtn.IsEnabled = false;
                ProgressWindow progressWindow = new()
                {
                    Owner = this
                };
                progressWindow.CurrentItemProgBar.Maximum = 100;
                progressWindow.AllItemsProgBar.Maximum = 100;
                progressWindow.Show();

                var progressCur = new Progress<int>(v => progressWindow.CurrentItemProgBar.Value = v);
                var progressWorking = new Progress<int>(v => progressWindow.WorkingProgBar.Value = v);
                var progressFileName = new Progress<string>(v => progressWindow.CurProgTxt.Text = v);
                var progressWorkingFileName = new Progress<string>(v => progressWindow.WorkingProgTxt.Text = v);


                try
                {
                    bool decompressResult = false;
                    bool copyItems = false;

                    //if the source file is a .zip, extract it to the target first.
                    //then the target is now the new source and don't need to copy any items
                    //also need to delete compressed files extracted from the new source
                    if (Path.GetExtension(SourceTextPath) == ".zip")
                    {
                        progressWindow.OverallTextBlk.Text = "Unzipping source file...";
                        await Task.Run(() => Decompressor.RunDecompressor(progressCur, progressWorking, progressFileName, progressWorkingFileName, SourceTextPath, TargetTextPath));
                        SourceTextPath = TargetTextPath;
                        copyItems = false;
                    }

                    else
                    {
                        copyItems = true;
                    }


                    FileFinder fileFinder = new();
                    fileFinder.GetListofItems(SourceTextPath);
                    List<string> CompressedItemList = fileFinder.AllFoundCompressedFiles;
                    List<string> UncompressedItemList = fileFinder.AllFoundUncompressedFiles;
                    List<string> UncompressedItemListScrubbed = UncompressedItemList.Except(CompressedItemList).ToList();

                    int compressedItems = CompressedItemList.Count;
                    int unCompressedItems = UncompressedItemListScrubbed.Count;
                    int totalItems = compressedItems;

                    //prevent odd behavior if nothing to copy
                    if (compressedItems == 0)
                    {
                        throw new Exception("Source has nothing to decompress!");
                    }

                    //if source and target are the same, don't copy files.
                    if (string.Equals(SourceTextBx.Text, TargetTextBx.Text, StringComparison.CurrentCultureIgnoreCase))
                    {
                        copyItems = false;
                    }

                    //if copying files, add them to the total item count
                    if (copyItems)
                    {
                        totalItems += UncompressedItemListScrubbed.Count;
                    }

                    int compressedItemsProcessed = 0;
                    int totalItemsProcessed = 0;
                    progressWindow.OverallTextBlk.Text = String.Format("0 of {0} compressed items processed...", compressedItems);

                    foreach (string CompressedItem in CompressedItemList)
                    {

                        string ItemTargetTextPath = Utils.GetTargetTextPathPerItem(CompressedItem, SourceTextPath, TargetTextPath, true);
                        if (!Directory.Exists(ItemTargetTextPath))
                        {
                            Directory.CreateDirectory(ItemTargetTextPath);
                        }

                        decompressResult = await Task.Run(() => Decompressor.RunDecompressor(progressCur, progressWorking, progressFileName, progressWorkingFileName, CompressedItem, ItemTargetTextPath));
                        compressedItemsProcessed++;
                        totalItemsProcessed++;
                        progressWindow.AllItemsProgBar.Value = (1.0d / totalItems * totalItemsProcessed * 100.0d);
                        progressWindow.OverallTextBlk.Text = String.Format("{0} of {1} compressed items processed...", compressedItemsProcessed, compressedItems);


                    }

                    progressWindow.CurProgTxt.Text = "Done!";
                    progressWindow.WorkingProgTxt.Text = "Waiting for copy to finish...";

                    if (decompressResult)
                    {
                        //if the source is a folder we probably need to copy stuff to the target since an extraction would do so.
                        if (copyItems)
                        {
                            int unCompressedItemsProcessed = 0;
                            //copy every item that wasn't compressed
                            foreach (string uncompressedItem in UncompressedItemListScrubbed)
                            {
                                string ItemTargetTextPath = Utils.GetTargetTextPathPerItem(uncompressedItem, SourceTextPath, TargetTextPath, false);

                                //skip if happens to be a directory
                                FileAttributes attr = File.GetAttributes(uncompressedItem);
                                if (attr.HasFlag(FileAttributes.Directory))
                                {
                                    continue;
                                }

                                //this will create the target item's directory before creating the item
                                string? ItemTargetDir = Path.GetDirectoryName(ItemTargetTextPath);
                                if (ItemTargetDir != null)
                                {
                                    Directory.CreateDirectory(ItemTargetDir);
                                }

                                //check if the file already exists on the target and append the name if so
                                //won't work correctly here... if the files already exist thwn with this it wouldn't overwrite them, instead it will append everything...
                                //needed though? Could we end up extracting files to a folder which already has files with the same name?
                                //if (File.Exists(ItemTargetTextPath))
                                //{
                                //    ItemTargetTextPath = Utils.AppendFileName(ItemTargetTextPath);
                                //}

                                await Task.Run(() => File.Copy(uncompressedItem, ItemTargetTextPath, true));
                                totalItemsProcessed++;
                                unCompressedItemsProcessed++;
                                progressWindow.AllItemsProgBar.Value = (1.0d / totalItems * totalItemsProcessed * 100.0d);
                                progressWindow.OverallTextBlk.Text = String.Format("{0} of {1} uncompressed files copied...", unCompressedItemsProcessed, unCompressedItems);
                            }
                        }

                        //if items were not copied because the source was a .zip or a folder with zips then delete any compressed items since they've been decompressed already.
                        if (!copyItems)
                        {
                            foreach (string item in CompressedItemList)
                            {
                                File.Delete(item);
                            }
                        }

                        if (OpenFldrBoxChkd)
                        {
                            Utils.OpenDirectory(TargetTextBx.Text);
                        }

                        Close();
                    }


                }

                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("An error occured during extraction: \r\n \r\n {0}", ex), "Extraction Problem",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                ExtractBtn.IsEnabled = true;
                progressWindow.Close();
                this.Activate();

            }

        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            AboutBox1 aboutBox = new();
            aboutBox.ShowDialog();
        }

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
