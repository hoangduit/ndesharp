# Introduction #

Largely based on http://code.google.com/p/ndephp/, this is a port of the code to C# (cheers for your hard work). It reads the database used for Winamp's media library and decodes it to a DataTable

# Details #

Most of the stuff is there, I've added song filepath decoding as well.

Uses Mono's DataConvert library because there is some painful data type conversions to do in C#.

It's GPL'd because it's based on another GPL project.

# Requirements #
**Probably .NET 3.5+ (uses a bit of LINQ), assembly was built using 4.0**

# Usage #

Include a reference to the assembly.

```
//The input to NDEDatabase is the path to the database (less the file extension)
NDEDatabase ndedb = new NDEDatabase(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Winamp\Plugins\ml\main");
//here we can access the dataset of music
DataSet songs = ndedb.SongDS;
//have fun with LINQ to query the data!
```