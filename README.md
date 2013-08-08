geckofxHtmlToPdf
================

Command line and .net component for making pdfs from html, using the gecko engine, as wrapped by [geckofx](https://bitbucket.org/geckofx "geckofx").

This project is just getting started, consider it alpha quality.

Basic command line:

    geckofxhtmltopdf inputpath outputpath

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

NB: Currently the only units that are supported are millimeters.

##Requirements##

.Net 4.0 Runtime (not yet tested with mono equivalent)
XulRunner that matches the version of geckofx dlls (current version 22).
Nuget to get dependencies

##Building##

Use Nuget to pull down the commandline library.

Unzip the XulRunner directory into the distfiles directory, so that you have distfiles/xulrunner.
[http://ftp.mozilla.org/pub/mozilla.org/xulrunner/releases/22.0/runtimes/](http://ftp.mozilla.org/pub/mozilla.org/xulrunner/releases/22.0/runtimes/)

##RoadMap##

- Add a winforms component that can be used to make PDFs easily from a winforms app without running the command line. (in progress)

- Add command line parameter for:
 -  media
 
Pull requests for  others arguments are welcome. Please follow the [wkhtmltopdf](http://code.google.com/p/wkhtmltopdf/ "wkhtmltopdf") conventions.


