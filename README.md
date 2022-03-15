Welcome to ZIPExpander: Total Decompression!

This application will take a .zip file or a folder as an input.
Press expand and it will decompress the .zip file (if given) then proceed to decompress every .zip and .gz file found within the source directory tree.
If a file which is compressed contains addional compressed files, those will be decompressed too.
I call this process "Expansion" as all compressed files will be expanded to their full size. This can make it easier to search a pack of files for strings.

Files will be decompressed to the target folder of your choosing. 
If using a folder as a source, the program will also copy any uncompressed items from the source folder to the target.

Requires .net 6.0.2, will be prompted to install on startup.


![image](https://user-images.githubusercontent.com/101219572/158442909-9ba8b037-1c6d-4b95-adbd-fbffb903d2d4.png)




Known issues: 

-Number of compressed items may increase during extraction, this is normal as the program will detect if an extracted item contains any compressed items within and will add those to the total item count

-Progress bars do not move when extracting .gz files

-After the decopmression is started, if a file in the source is locked/changed/removed the decompression loop will throw an error, EG file does not exist. 
Same that adding a .zip file to a folder which is already being decompressed, it wont be considered for decompression after the job has been started.
This is really only a problem for expanding big folders where a user may forget the expansion is running.

-after expansion is complete the progress window stays open as intended. Another expansion can be started in this state as the main window button returns to enabled. However the progress window from the previous expansion is not closed and a new progress window will appear for the next expansion.

-filename.tar.gz files, I am unsure of the current behavior for these files. Likely the tar will be extracted from the .gz but will not be further extracted. Need better example files to test with.
