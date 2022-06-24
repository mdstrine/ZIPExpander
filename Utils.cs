using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace ZIPExpander
{
    internal class Utils
    {
        //Opens a windows explorer window at a given directory
        public static void OpenDirectory(string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                ProcessStartInfo startInfo = new()
                {
                    Arguments = targetPath,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }

            else
            {
                MessageBox.Show(string.Format("Could not open target path: {0} Directory does not exist!", targetPath));
            }

        }

        //validates that the source and target inputs from UI are usable, also creates target path if it doesnt exist
        public static bool ValidateSourceAndTarget(string sourcePath, string targetPath)
        {
            bool valid = true;
            //Validate source exists and isn't the default.
            if ((!File.Exists(sourcePath)) || (sourcePath == "Click Browse..."))
            {
                if (!Directory.Exists(sourcePath))
                {
                    MessageBox.Show("Please enter a valid source path", "Cannot Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                    valid = false;
                    return valid;
                }
            }

            //Validate target
            //try to create target path if it does not exist
            try
            {
                if (targetPath == "Click Browse...")
                {
                    MessageBox.Show("Enter a target path", "Cannot Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    valid = false;
                    return valid;
                }
                else if (targetPath == "")
                {
                    MessageBox.Show("The target path is empty", "Cannot Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    valid = false;
                    return valid;
                }
                else if (!Path.IsPathRooted(targetPath))
                {
                    MessageBox.Show("Target path root is not valid, check the root (EG: C:\\)", "Cannot Extract",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    valid = false;
                    return valid;
                }
                else if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

            }

            catch (Exception ex)
            {
                string message = "Please enter a valid target path \r\n"+ ex.Message;
                MessageBox.Show(message, "Cannot Extract",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

                valid = false;
            }

            if ((sourcePath == "Click Browse...") || (targetPath == "Click Browse..."))
            {
                valid = false;
            }

            return valid;

        }

        public static string GetTargetTextPathPerItem(string sourceItemIn, string sourcePathIn, string targetPathIn, bool isCompressed)
        {
            //returns a target item path based on the inputs source item path, source path, and target path
            //IsCompressed determines if the item should have the extension removed or not
            //(dont want .zip in target folder names, do want extension when copying uncompresseditems.)

            //EG 
            //SourceItemIn = C:\Templogs\4855_before v11\PWPMGT101P.payworks.lan\Backup\2021-09-09_Svc.VeeamBackup.zip
            //SourcePathIn = C:\Templogs\4855_before v11\
            //TargetPathIn = D:\mylogs\4855_before v11_expanded\

            //SourceItemIn - SourcePathIn = PWPMGT101P.payworks.lan\Backup\2021-09-09_Svc.VeeamBackup.zip
            // then targetpathIn is added before this

            //Return = D:\mylogs\4855_before v11_expanded\PWPMGT101P.payworks.lan\Backup\2021-09-09_Svc.VeeamBackup
            // It does not create the directory

            //The sourceitempath must start with the sourcepath for this to work properly
            //I'm sure this will break in some situation - If the source is extracted to a different target path,
            //and the item extracted is another compressed item, that compressed item has a different source path than what was given during job start.

            //I can see a problem if we have two compressed files with the same name, eg MyLogs.zip and MyLogs.gz - these would both extract to a folder named "MyLogs_Extracted" and if those have files with the same name within, those files will be overwritten
            //consider checking the list of compressed items in the for loop for any items with the same name without extension. If thats the result somehow add a number to and increment the _Extracted postfix so those get put into different folders

            string sourcePath = sourcePathIn;
            string sourceItemPath = sourceItemIn;
            string targetPath = targetPathIn;
            string fullSourcePath = Path.GetFullPath(sourcePath);
            string fullSourceItemPath = Path.GetFullPath(sourceItemPath);

            if (fullSourceItemPath.StartsWith(fullSourcePath, StringComparison.CurrentCultureIgnoreCase))
            {
                string targetEndOnly = fullSourceItemPath[fullSourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                if (isCompressed)
                {
                    //used to replace the periods in the path when decompressing a compressed item, I'm not sure why, maybe to prevent a folder with an extension? shouldnt matter if I'm adding _extracted?
                    //It causes problems if there are periods in the path such as a user profile michael.strine so I reverted to not replacing periods.
                    //old code:
                    //string targetPathReplacePeriods = targetPath.Replace(".", "_");
                    //string targetEndOnlyWithoutExt = Path.ChangeExtension(targetEndOnly, null);
                    //return targetPathReplacePeriods + "\\" + targetEndOnlyWithoutExt + "_Extracted";

                    string targetEndOnlyWithoutExt = Path.ChangeExtension(targetEndOnly, null);
                    return targetPath + "\\" + targetEndOnlyWithoutExt + "_Extracted";


                }
                else
                {
                    return targetPath + "\\" + targetEndOnly;
                }

            }

            else
            {
                throw new Exception(string.Format("Problem Making Target Text Path! \r\n \r\n SourceItemIn:{0} \r\n SourcePathIn: {1} \r\n TargetPathIn: {2} \r\n", sourceItemIn, sourcePathIn, targetPathIn));
            }

        }

        public static bool IsCompressedExtAny(string file)
        {
            if ((Path.GetExtension(file) == ".zip") || (Path.GetExtension(file) == ".gz") || (Path.GetExtension(file) == ".tar") || (Path.GetExtension(file) == ".tgz") || (Path.GetExtension(file) == ".7z") || (Path.GetExtension(file) == ".rar") || (Path.GetExtension(file) == ".gzip"))
            { 
                return true; 
            }
            else
            {
                return false;
            }
        }
    }

}
