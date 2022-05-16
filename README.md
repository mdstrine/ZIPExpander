ZIPExpander: Total Decompression v0.9.5

Requires .net 6.0.2 (you will be prompted to install on first run if not already installed)


Do you have a zip or a folder which has a lot of embedded compressed items within? 

Do you wish you could decompress all of those compressed items with just one click?

This program may help you. 


It will decompress all .zip, .gz, .tar, .tgz, .7z, and .rar files within the given source. 

It will also check for and decompress any compressed items embeded within items it has already decompressed.


Meaning anything which is compressed in the given source should be completely decompressed. 

The given source should be completely "expanded" when the program is finished.



Operation:

Double click the .exe to run

Feed it a .zip file or a folder.

Give it a target where you want it to decompress everything to. 

Hit Expand and wait! 



Caution: 
This program does not check expected output file size or your free disk space before decompressing. 
While it is running it should let you know if there is not enough free space to continue the decompression.



Known issues: 

- The entered source must be a .zip or an uncompressed folder. This is due to me not yet making a custom file browser for the source which is limited to finding only .zip, .gz, .tar, .tgz, .7z, and .rar files.

- Number of compressed items may increase during extraction, this is normal as the program can only determine if a compressed item had other embeded compressed items within after that item has been decompressed.

- After the decopmression is started, if a file in the source is changed or removed the decompression loop will throw an error (E.G. file does not exist). Same that adding a file to a folder already being decompressed, it wont be considered for decompression.
This is really only a problem for huge, long running decompressions.

- It's possible to start another extraction without closing the previous progress window. Not a huge issue but looks odd. I added a "close window" button to the progress window which is only active once the extraction is complete. I haven't discovered how to close the existing progress window programatically when a new one has been opened.

- Detection of .7z and .gz files are based on the file extension. If for some reason a 7z compressed file has the extension .zip, it will not process correctly. Same that if a source .zip is not actually a .zip, it won't know. 

- Likely unoptimized, speed is largely dependent on CPU power and disk speed (SSDs are much faster)



Future plans: 

- Add an "Are you sure?" prompt to closing the progress window (which stops the current extraction).

- It should be possible to open a source .zip file as a stream and decompress items from within, rather than decompressing the source .zip first then decompressing other items as I do now.

- Add an "active log" of files being decompressed 




Uses Libraries:
SharpCompress by Adam Hathcock (modified by me to be aware of Windows file name restrictions)
WindowsAPICodePack by rpastric, contre, dahall


Please report any issues with a screenshot to michael.strine@veeam.com
