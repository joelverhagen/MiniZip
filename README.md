# MiniZip

Read the file listing of a .zip archive without downloading the whole thing.

## Introduction

The idea is that ZIP files have their central directory data end the end of the file. This central directory is a
listing of all files in the ZIP archive. Using an HTTP HEAD request to get the file size and HTTP GET with a range
request header, you can determine all of the files in a ZIP archive without downloading the whole thing.

Comcast has a 1 terabyte data limit on my line so this is a necessary measure for my efforts around NuGet package
analysis.

## Install

You can install this package via [NuGet](https://www.nuget.org/):

```
dotnet add package Knapcode.MiniZip
```

For more information about the package, see the
[package details page on NuGet.org](https://www.nuget.org/packages/Knapcode.MiniZip).

## Example

Take a look at the following sample code to get an idea of how this thing is used:

https://github.com/joelverhagen/MiniZip/blob/master/MiniZip.Sandbox/Program.cs

Here's a smaller example if you're into that.

```csharp
var url = "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg";

using (var httpClient = new HttpClient())
{
    var httpZipProvider = new HttpZipProvider(httpClient);

    using (var zipDirectoryReader = await httpZipProvider.GetReaderAsync(new Uri(url)))
    {
        var zipDirectory = await zipDirectoryReader.ReadAsync();

        Console.WriteLine("Top 5 ZIP entries by compressed size:");

        var entries = zipDirectory
            .Entries
            .OrderByDescending(x => x.GetCompressedSize())
            .Take(5)
            .ToList();

        for (var i = 0; i < entries.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {entries[i].GetName()}");
        }
    }
}
```

Only a fraction of the file is downloaded to determine the metadata about the ZIP file. From my experiments this is
almost always less than 1% of the total package size. Great savings! Assuming all you care about is metadata about the
ZIP file, not the contained files.

In the future, perhaps I'll provide a wait to tease a single file out of the archive without downloading the whole
thing...

### Sample Output

The truncated output of the longer sample code looks something like this:

```
========================================

==> HEAD https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg

<== 200 OK
    Accept-Ranges: bytes
    Content-Length: 2066865

==> GET https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg
    Range: bytes=2066843-2066864

<== 206 Partial Content
    Accept-Ranges: bytes
    Content-Length: 22
    Content-Range: bytes 2066843-2066864/2066865

==> GET https://api.nuget.org/v3-flatcontainer/newtonsoft.json/10.0.3/newtonsoft.json.10.0.3.nupkg
    Range: bytes=2062769-2066842

<== 206 Partial Content
    Accept-Ranges: bytes
    Content-Length: 4074
    Content-Range: bytes 2062769-2066842/2066865

Top 5 ZIP entries by compressed size:
1. lib/net45/Newtonsoft.Json.dll (243,283 bytes)
2. lib/netstandard1.3/Newtonsoft.Json.dll (238,412 bytes)
3. lib/netstandard1.0/Newtonsoft.Json.dll (232,317 bytes)
4. lib/portable-net45%2Bwin8%2Bwp8%2Bwpa81/Newtonsoft.Json.dll (232,022 bytes)
5. lib/net40/Newtonsoft.Json.dll (201,901 bytes)

========================================

...

========================================

Total ZIP files checked:    5
Total HTTP requests:        15
Total Content-Length bytes: 10,138,058
Actual downloaded bytes:    20,480
Downloaded %:               0.202%
```