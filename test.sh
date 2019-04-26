#!/bin/bash
export MONO_PREFIX=/opt/mono4-sil
export MONO_SILPKGDIR=/opt/mono4-sil

# Environment settings for running programs with the SIL version of mono
BASE="$(pwd)"
[ -z "$BUILD" ] && BUILD=Debug

# Search for xulrunner and geckofx, select the best, and add its location to LD_LIBRARY_PATH.
# Also determine the location of the geckofx assemblies and shared object libraries.
tmpbasedir=$(pwd | sed s=usr/share/=usr/lib/=)
export XULRUNNER=$(dirname $(find $tmpbasedir -name libxul.so | grep -v /packages/))
export LD_PRELOAD=$(find $tmpbasedir -name libgeckofix.so | grep -v /packages/)
unset tmpbasedir

export MONO_PATH="${BASE}/output/${BUILD}:/usr/lib/cli/gdk-sharp-3.0:${GECKOFX}"
export PATH="${MONO_PREFIX}/bin:${BASE}/output/${BUILD}:${PATH}"
export LD_LIBRARY_PATH="${MONO_PREFIX}/lib:${XULRUNNER}:${BASE}/output/${BUILD}:${LD_LIBRARY_PATH}"
export PKG_CONFIG_PATH="${MONO_PREFIX}/lib/pkgconfig:${PKG_CONFIG_PATH}:/usr/local/lib/pkgconfig:/lib/pkgconfig:/usr/lib/pkgconfig"
export MONO_GAC_PREFIX="${MONO_PREFIX}:/usr"

export MONO_RUNTIME=v4.0.30319
export MONO_DEBUG=explicit-null-checks
export MONO_ENV_OPTIONS="-O=-gshared"
export MONO_TRACE_LISTENER="Console.Out"
export MONO_MWF_SCALING=disable

# prevent Gecko from printing scary message about "double free or corruption" on shutdown
# (See FWNX-1216.)  Tom Hindle suggested this hack as a stopgap.
export MALLOC_CHECK_=0

# set HGRCPATH so that we ignore ~/.hgrc files which might have content that is
# incompatible with our version of Mercurial
export HGRCPATH=

#sets keyboard input method to none
unset XMODIFIERS

time /opt/mono4-sil/bin/mono-sgen --runtime=v4.0 --debug output/Debug/GeckofxHtmlToPdf.exe "${BASE}/test/letterSpacingTest.html" output/letterSpacingTest.pdf --report-memory >test.output
time /opt/mono4-sil/bin/mono-sgen --runtime=v4.0 --debug output/Debug/GeckofxHtmlToPdf.exe "${BASE}/test/letterSpacingTest.html" output/letterSpacingTest-single.pdf --reduce-memory-use --report-memory >>test.output
time /opt/mono4-sil/bin/mono-sgen --runtime=v4.0 --debug output/Debug/GeckofxHtmlToPdf.exe "${BASE}/test/Portable Document Format - Wikipedia.htm" output/pdf-wiki.pdf --report-memory >>test.output
time /opt/mono4-sil/bin/mono-sgen --runtime=v4.0 --debug output/Debug/GeckofxHtmlToPdf.exe "${BASE}/test/Portable Document Format - Wikipedia.htm" output/pdf-wiki-single.pdf --reduce-memory-use --report-memory >>test.output
