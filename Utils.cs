using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace ZIPExpander
{
    internal class Utils
    {

        public static void OpenDirectory(string TargetPath)
        {
            if (Directory.Exists(TargetPath))
            {
                ProcessStartInfo startInfo = new()
                {
                    Arguments = TargetPath,
                    FileName = "explorer.exe"
                };

                Process.Start(startInfo);
            }

            else
            {
                MessageBox.Show(string.Format("Could not open target path: {0} Directory does not exist!", TargetPath));
            }

        }

        public static bool ValidateSourceAndTarget(string SourcePath, string TargetPath)
        {
            bool valid = true;
            //Validate source
            if ((!File.Exists(SourcePath)) || (SourcePath == "Click Browse..."))
            {
                if (!Directory.Exists(SourcePath))
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
                if (TargetPath == "Click Browse...")
                {
                    throw new Exception("Enter a target path");
                }
                else if (TargetPath == "")
                {
                    throw new Exception("The target path is empty");
                }
                else if (!Path.IsPathRooted(TargetPath))
                {
                    throw new Exception("Please enter a complete and valid path, path must be rooted");
                }
                else if (!Directory.Exists(TargetPath))
                {
                    Directory.CreateDirectory(TargetPath);
                }
            }

            catch (Exception ex)
            {
                string message = "Please enter a valid target path" + System.Environment.NewLine + ex.Message;
                MessageBox.Show(message, "Cannot Extract",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

                valid = false;

            }

            if ((SourcePath == "Click Browse...") || (TargetPath == "Click Browse..."))
            {
                valid = false;
            }

            return valid;


        }

        public static string GetTargetTextPathPerItem(string SourceItemIn, string SourcePathIn, string TargetPathIn, bool IsCompressed)
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
        //I'm sure this will break in some situation

        {
            string SourcePath = SourcePathIn;
            string SourceItemPath = SourceItemIn;
            string TargetPath = TargetPathIn;
            string FullSourcePath = Path.GetFullPath(SourcePath);
            string FullSourceItemPath = Path.GetFullPath(SourceItemPath);

            if (FullSourceItemPath.StartsWith(FullSourcePath, StringComparison.CurrentCultureIgnoreCase))
            {
                string TargetEndOnly = FullSourceItemPath[FullSourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                if (IsCompressed)
                {
                    string TargetEndOnlyWithoutExt = Path.ChangeExtension(TargetEndOnly, null);
                    return TargetPath + "\\" + TargetEndOnlyWithoutExt + "_Extracted";
                }
                else
                {
                    return TargetPath + "\\" + TargetEndOnly;
                }
            }
            else
            {
                throw new Exception(string.Format("Problem Making Target Text Path! \r\n \r\n SourcePath:{0} \r\n CompressedItemPath: {1}", FullSourcePath, FullSourceItemPath));
            }

  
        }

    }
}
