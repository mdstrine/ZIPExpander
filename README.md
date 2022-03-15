ZIPExpander

Requires .net 6.0.2

Known issues: 

-Number of compressed items may increase during extraction, this is normal as the program will detect if an extracted item contains any compressed items within and will add those to the list to also be extracted

-Progress bars do not move when extracting .gz files

-After the decopmression is started, if a file in the source is changed/removed the decompression loop will throw an error, EG file does not exist. 
Same that adding a file to a folder being decompressed, it wont be considered for decompression.
This is really only a problem for huge, long running decompressions.
