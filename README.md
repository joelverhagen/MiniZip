# MiniZip

Read the file listing of a .zip archive without downloading the whole thing.

## Introduction

The idea is that ZIP files have their central directory data end the end of the file. This central directory is a
listing of all files in the ZIP archive. Using an HTTP HEAD request to get the file size and HTTP GET with a range
request header, you can determine all of the files in a ZIP archive without downloading the whole thing.

Comcast has a 1 terabyte data limit on my line so this is a necessary measure for my efforts around NuGet package
analysis.

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
