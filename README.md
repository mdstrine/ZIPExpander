ZIPExpander: Total Decompression v0.9.6

Requires .net 6.0.2 (you will be prompted to install on first run if not already installed)

 
 

 
ZipExpander will decompress the contents of all .zip, .gz, .tar, .tgz, .gzip, .7z, and .rar files within the given source folder or .zip file. 
It will also check the contents which were decompressed for any embeded compressed files and decompress those as well.
The given source file tree should be completely "expanded" when the program is finished.

 
 
Operation:

Double click the .exe to run.

Install .net 6.0.2 if promped 

Select a .zip file or a folder.

Pick a target where you want it to decompress everything to. 

Hit Expand and wait.

 
 
Caution: 
This program does not check expected output file size or your free disk space before decompressing. 

 
 
Known issues: 

- The entered source must be a .zip or an uncompressed folder. This is due to me not yet making a custom file browser for the source which is limited to finding only .zip, .gz, .tar, .tgz, .gzip, .7z, and .rar files.

- Number of compressed items may increase during extraction, this is normal as the program can only determine if a compressed item had other embeded compressed items within after that item has been decompressed.

- After the decopmression is started, if a file in the source is changed or removed the decompression loop will throw an error (E.G. file does not exist). Same that adding a file to a folder already being decompressed, it wont be considered for decompression.
This is really only a problem for huge, long running decompressions.

- It's possible to start another extraction without closing the previous progress window. Not a huge issue but looks odd. I added a "close window" button to the progress window which is only active once the extraction is complete. I haven't discovered how to close the existing progress window programatically when a new one has been opened.

- Detection of .7z and .gz files are based on the file extension. If for some reason a 7z compressed file has the extension .zip, it will not process correctly. Same that if a source .zip is not actually a .zip, it won't know. 

- Likely unoptimized, speed is largely dependent on CPU power and disk speed (SSDs are much faster)

 
 
Future plans: 

- Add an "Are you sure?" prompt to closing the progress window (which stops the current extraction).

- It should be possible to open a source .zip file as a stream and decompress items from within, rather than decompressing the source .zip first then decompressing other items as it does now.

- Add an "active log" of files being decompressed 

 
 
Building this code in VS 2022:

run: dotnet publish -p:PublishSingleFile=true  -r win-x64 -c Release --self-contained false

 
 
Uses Libraries:
SharpCompress by Adam Hathcock (modified by me to be aware of Windows file name restrictions)
WindowsAPICodePack by rpastric, contre, dahall


 
 
Please report any issues with a screenshot to michael.strine@veeam.com or create a github issue






Change log:

0.9.3
-Added missing logic to decompress zips which were extracted from zips. IE if I have layer1.zip and that has a zip named later2.zip and this zip has a zip named layer3.zip, layer 1 and 2 would be extracted to folders but layer3.zip and beyond would not.

-If an error happens during decompression, file copy, or file deletion the file being operated on can be aborted/retried/ignored rather than the whole decompression needing to be restarted

-added progress to the task bar

-progress window can be moved

-cleaned up code naming convention and added comments


0.9.4
-Changed exiting, rather than app auto closing it will bring itself to the foreground and wait for the user to click exit after decompression is complete. Folder still auto opens when decompression is done.

-Changed extraction progress stats a little.

-fixed a bug where trying to decompress a 0kb .zip file would throw an error (0kb zip files are skipped now)

    known issue, after extraction is complete the progress window stays open as intended. Another extraction can be started in this state as the main window button returns to enabled. If another extraction is started another progress window appears and now there are two...


0.9.5

-v0.9.5 now uses sharpcompress libraries instead of dotnetzip

-The program can now decompress all .zip, .gz, .tar, .tgz, .7z, and .rar files within a folder or .zip file. (It was limited to .gz and .zip files before)

-Progress is now reported for .gz files

-Can close the progress window once extraction is complete to start another extraction without closing the program


0.9.6

-added gzip support