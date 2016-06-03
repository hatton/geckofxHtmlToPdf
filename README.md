geckofxHtmlToPdf
================

Command line and .net component for making pdfs from html, using the Mozilla Gecko engine (which powers Firefox), as wrapped by [geckofx](https://bitbucket.org/geckofx "geckofx").  It can handle complex scripts and fonts that incorporate [SIL's Graphite](http://graphite.sil.org) rules.

Basic command line:

    GeckofxHtmlToPdf inputpath outputpath

Other options:

    --graphite
	--margin-top (-T) e.g. -T 2.5
	--margin-bottom (-B)
	--margin-left (-L)
	--margin-right (-R)
    --orientation (-o) e.g. "landscape"
	--page-size (-s) e.g. -s A5
	--page-width
	--page-height
    --quiet (-q)
	--debug

NB: Currently the only units that are supported are millimeters.

NB: Currently the input and output paths must precede the parameters (this appears to be a requirement of Args.dll).

##Requirements##

.Net 4.0 Runtime or Mono equivalent

##Building##

msbuild geckofxHtmlToPdf.sln (with any desired command line options like /p:Configuration=Release)

The build process downloads the appropriate Geckofx (and Firefox) executables using NuGet, so an Internet connection is necessary at least to get started.

##Limitation##

- We use this in a production desktop app and are quite happy with it. However it has one significant problem: it appears that gecko decompresses all images of the entire document into RAM, as simple bitmaps, at the same time. In large documents with many pictures (e.g. text books), this RAM exceeds how much a 32-bit program can use.  So far our customers doing these books have found that if they get their images down to 600 dpi, then there is enough RAM to create the PDF.
 
Pull requests for others command line arguments are welcome. Please follow the [wkhtmltopdf](http://code.google.com/p/wkhtmltopdf/ "wkhtmltopdf") conventions.


