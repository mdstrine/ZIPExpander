using System.Collections.Generic;
using System.IO;

namespace ZIPExpander
{
    public class FileFinder
    //make a list of all files for the return
    {
        private List<string> _AllFoundCompressedFiles = new();
        private List<string> _AllFoundUncompressedFiles = new();

        public List<string> AllFoundCompressedFiles
        {
            get { return _AllFoundCompressedFiles; }
        }

        public List<string> AllFoundUncompressedFiles
        {
            get { return _AllFoundUncompressedFiles; }
        }

        public void GetListofItems(string StartFolder)
        //returns a list of strings which is all files found in the start folder.
        {
            //make a list of all files in the directory (This cannot be run with .zip files as the startfolder)
            List<string> AllFilesInCurrentDirectory = new(Directory.EnumerateFiles(StartFolder));

            //add each zip or gz file to list of all found files
            foreach (string file in AllFilesInCurrentDirectory)
            {
                if ((Path.GetExtension(file) == ".zip") || (Path.GetExtension(file) == ".gz"))
                {
                    _AllFoundCompressedFiles.Add(file);
                }
                else
                {
                    _AllFoundUncompressedFiles.Add(file);
                }
            }

            //make a list of all directories in the current directory
            List<string> allDirsInfilepath = new(Directory.EnumerateDirectories(StartFolder));

            //Recurse the zip file finder in each directory, it will add its findings to the lists.
            foreach (string dir in allDirsInfilepath)
            {
                GetListofItems(dir);
            }

        }
    }
}
