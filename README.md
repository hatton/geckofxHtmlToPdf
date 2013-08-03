geckofxHtmlToPdf
================

Command Line and (eventually) .net dll for making pdfs from html, using the gecko engine, as wrapped by [geckofx](https://bitbucket.org/geckofx "geckofx").

This project is just getting started, it's barely working. Currently it always shows a little window telling you its working.

Current command line is just

    geckofxhtmltopdf inputpath outputpath

##Requirements##

.Net 4.0 Runtime (not yet tested with mono equivalent)
XulRunner that matches the version of geckofx dlls (current version 22).
Nuget to get dependencies

##Building##

Use Nuget to pull down the commandline library.

Unzip the XulRunner directory into the distfiles directory, so that you have distfiles/xulrunner.
[http://ftp.mozilla.org/pub/mozilla.org/xulrunner/releases/22.0/runtimes/](http://ftp.mozilla.org/pub/mozilla.org/xulrunner/releases/22.0/runtimes/)

##RoadMap##

- Get rid of the current header that gecko adds

- Add command line parameters for:
 -  page size
 -  orientation
 -  media
 -  perhaps others, following the [wkhtmltopdf](http://code.google.com/p/wkhtmltopdf/ "wkhtmltopdf") conventions


- Add a winforms component that can be used to make PDFs easily from a winforms app without running the command line.

