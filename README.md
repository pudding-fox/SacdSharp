# SacdSharp
## A managed interface for extracting DSD audio from SACD iso images.

Example:
```c#
var sacd = SacdFactory.Instance.Create(Path.Combine(Media, "Alan Parsons Project - 1984.iso"));

sacd.InitialiseComponent();

var area = sacd.Areas[0];
var track = area.Tracks[0];
var extractor = SacdFactory.Instance.Create(sacd, area, track);

extractor.Progress += (sender, e) => Console.WriteLine("Progress: {0}%", e.Value);

var directoryName = Path.GetTempPath();
var fileName = default(string);
var result = extractor.Extract(directoryName, out fileName);

...

```
