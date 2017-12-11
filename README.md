# MiniZip

Read the file listing of a .zip archive without downloading the whole thing.

## Notice

This project is very much in the experimental phase. Very clearly the product of a one day effort...

The idea is that .zip files have their central directory data end the end of the file. This central directory is a
listing of all files in the .zip archive. Using an HTTP `HEAD` request to get the file size and HTTP `GET` with a range
request header, you can determine all of the files in a .zip archive without downloading the whole thing.

Comcast has a 1 terabyte data limit so this is a necessary measure for my efforts around NuGet package analysis.
