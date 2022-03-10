using System.Collections.Generic;
using System.IO;

namespace ZIPExpander
{
    //File finder finds files
    //make an instance of the class then call GetListofItems on a starting path
    //it will recursively go thru all files and folders in the start path and fill two lists
    //one list with compressed files and the other with uncompressed files
    //the lists can then be accessed thru the object
    public class FileFinder
    {
        //declare the lists outside the loop so they can be filled recursively
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

        //fills two lists of strings with all files found in the start folder.
        public void GetListofItems(string startFolder)      
        {
            //make a new list of all files in the startfolder directory (This cannot be run with .zip files as the startfolder)
            List<string> allFilesInCurrentDirectory = new(Directory.EnumerateFiles(startFolder));

            //itterate through each file, add each zip or gz file to a list
            foreach (string file in allFilesInCurrentDirectory)
            {
                if ((Path.GetExtension(file) == ".zip") || (Path.GetExtension(file) == ".gz"))
                {
                    _AllFoundCompressedFiles.Add(file);
                }
                //or else add the current file to the list of uncompressed files if its not those types
                else
                {
                    _AllFoundUncompressedFiles.Add(file);
                }
            }

            //make a list of all directories in the current directory
            List<string> allDirsInfilepath = new(Directory.EnumerateDirectories(startFolder));

            //Recurse this file finder in each found directory, it will add its findings to the lists.
            foreach (string dir in allDirsInfilepath)
            {
                GetListofItems(dir);
            }

        }
    }
}
