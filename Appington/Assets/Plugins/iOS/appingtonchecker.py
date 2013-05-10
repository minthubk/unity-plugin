#!/usr/bin/env python

# Python 2.7 or 3.2+ are needed to run this tool

from __future__ import print_function

# Check Python version
import sys
if sys.version_info<(2,7):
    sys.exit("At least Python 2.7 is needed")

import subprocess
import json
import os
import tempfile
import shutil
import time
import zipfile
import re

try:
    import urllib2
    HTTPError=urllib2.HTTPError
except ImportError:
    import urllib.request as urllib2
    import urllib.error
    HTTPError=urllib.error.HTTPError

try:
    import ConfigParser
except ImportError:
    import configparser as ConfigParser

opj=os.path.join
mydir=os.path.abspath(os.path.dirname(__file__))

# Encoding for files we use
utf8="utf-8" # Python has a staggering number of synonyms for this!

SDK_VERSION="#!AppingtonVersion 0.9.19-737d840"

def run_pipe(*args):
    "Run a bunch of commands piped together returning exit code and stdout"
    assert len(args)
    lastpipe=lastproc=None

    try:
        for cmd in args:
            kwargs={"stdout": subprocess.PIPE}
            if lastpipe is not None:
                kwargs["stdin"]=lastpipe
            lastcmd=subprocess.Popen(cmd, **kwargs)
            if lastpipe is not None:
                lastpipe.close()
            lastpipe=lastcmd.stdout

        out, err=lastcmd.communicate()
        return lastcmd.returncode, out
    except:
        # always fatal
        message(options, "runcmd", cmd=" | ".join(" ".join(cmd) for cmd in args), err=sys.exc_info()[1])

def run(options, *args, **kwargs):
    assert "stdout" not in kwargs and "stderr" not in kwargs
    fatal=True
    if "fatal" in kwargs:
        fatal=kwargs["fatal"]
        del kwargs["fatal"]
    kwargs["stdout"]=subprocess.PIPE
    kwargs["stderr"]=subprocess.PIPE
    try:
        p=subprocess.Popen(args, **kwargs)
        out,err=p.communicate()
    except:
        err=sys.exc_info()[1]
    if fatal and (err or p.returncode):
        message(options, "runcmd", cmd=" ".join(args), err=err)
    return out, err

def update_check(options):
    print("Appington SDK Version:", SDK_VERSION.split()[1])
    if not options.internet:
        return

    cfgname=os.path.expanduser("~/.appingtonsdk.cfg")
    cfg=ConfigParser.SafeConfigParser()
    cfg.read([cfgname])
    lastcheck=cfg.getint("appington", "ioslastcheck") if cfg.has_option("appington", "ioslastcheck") else 0
    # Only check once an hour
    if abs(time.time()-lastcheck)<60*60:
        return

    try:
        v=urllib2.urlopen(options.update_url).read()
        if type(v)==bytes:
            v=v.decode(utf8)
        fullupdateversion=json.loads(v)["version"]
    except:
        e=sys.exc_info()[1]
        if abs(time.time()-lastcheck)<7*24*60*60:
            return
        if lastcheck:
            err="It has been more than 7 days since the last update check.  Last error is "+str(e)
        else:
            err="Unable to check for updates.  Error is "+str(e)
        message(options, "sdk_version", err=err)
        return

    curversion=SDK_VERSION.split()[1].split("-")[0]
    updateversion=fullupdateversion.split("-")[0]

    cversion=[int(x) for x in curversion.split(".")]
    uversion=[int(x) for x in updateversion.split(".")]
    if cversion<uversion:
        message(options, "sdk_update", version=fullupdateversion)
    lastcheck=int(time.time())
    if not cfg.has_section("appington"):
        cfg.add_section("appington")
    cfg.set("appington", "ioslastcheck", str(lastcheck))
    cfg.write(open(cfgname, "w"))

def extract_app(options):
    if os.path.isdir(options.input):
        if not options.input.endswith(".app"):
            message(options, "not_app", directory=options.input)
        options.bundle_dir=options.input
    else:
        try:
            with zipfile.ZipFile(options.input, "r") as z:
                # Did we find Payload directory
                payloadfound=False
                # Dot apps we found
                dotapp=set()
                for n in z.namelist():
                    if n.startswith("/") or "\\" in n or "/../" in n:
                        message(options, "ipa_paths", filename=options.input)
                    if n.startswith("Payload/"):
                        payloadfound=True
                        m=re.match(r"Payload/(.*?\.app)/", n)
                        if m:
                            dotapp.add(m.group(1))
                if not payloadfound:
                    message(options, "ipa_paths", filename=options.input)
                if not dotapp:
                    message(options, "ipa_no_app", filename=options.input)
                if len(dotapp)!=1:
                    message(options, "ipa_apps", filename=options.input, apps=", ".join(sorted(list(dotapp))))

                # Now that we have sanitized input, extract it
                z.extractall(options.tempd)
                options.bundle_dir=opj(options.tempd, "Payload", list(dotapp)[0])
        except SystemExit:
            raise
        except:
            e=sys.exc_info()[1]
            message(options, "ipa_error", error=e)

    assert os.path.isdir(options.bundle_dir)

def examine_plist(options):
    infoplist=opj(options.bundle_dir, "Info.plist")
    if not os.path.isfile(infoplist):
        message(options, "no_plist")

    out, _=run(options, "plutil", "-convert", "json", "-o", "-", infoplist)
    try:
        options.infoplist=json.loads(out)
    except:
        message(options, "plist_json", err=sys.exc_info()[1])

    if not options.bundle_id:
        options.bundle_id=options.infoplist.get("CFBundleIdentifier", None)

    if not options.bundle_id:
        message(options, "no_bundleid")

    options.app_executable=opj(options.bundle_dir, options.infoplist["CFBundleExecutable"])
    if not os.path.isfile(options.app_executable):
        message(options, "no_app_binary", path=options.app_executable)

def check_ios_version(options):
    if "MinimumOSVersion" in options.infoplist:
        supportedver=options.infoplist["MinimumOSVersion"]
        # This will fail when iOS 10 comes out
        if supportedver<"5.0":
            message(options, "ios_version", requested=supportedver)

def check_app_version(options):
    rc, out=run_pipe(["strings", "-", options.app_executable],
                     ["grep", "Appington"])
    assert rc==0
    out=out.split("\n")
    # Check to see if SDK is even integrated
    for line in out:
        if "AppingtonDownloader" in line:
            break
    else:
        message(options, "no_sdk_used")
        return

    for line in out:
        if line.startswith("#!AppingtonVersion"):
            break
    else:
        message(options, "no_sdk_used")
        return

    version=line.split()[1]
    expected=SDK_VERSION.split()[1]
    if version!=expected:
        message(options, "bundle_sdk_version", expected=expected, version=version)

def get_asset(options, name):
    try:
        url="https://cdn.appington.com/updates/%s/config.json?redirect=false" % (options.bundle_id,)
        config=urllib2.urlopen(url).read()
        if type(config)==bytes:
            config=config.decode(utf8)
        config=json.loads(config)
    except HTTPError as e:
        if e.code==404:
            message(options, "not_provisioned", bundle_id=options.bundle_id)
            return
        message(options, "internet_error", url=url, error=e)
        return

    if name=="config.json":
        return config

    assert name=="playback.jmp"

    try:
        url="https://cdn.appington.com/updates/%s/playback.jmp?redirect=false" % (options.bundle_id,)
        playback=urllib2.urlopen(url).read()
    except HTTPError as e:
        if e.code!=404:
            message(options, "internet_error", url=url, error=e)
        return

    return playback

def check_assets(options):
    if not options.internet:
        return

    options.appington_dir=opj(options.bundle_dir, "appington")
    if not os.path.isdir(options.appington_dir):
        return

    config=get_asset(options, "config.json")
    if config is None:
        return

    configjson=opj(options.appington_dir, "config.json")
    if os.path.exists(configjson) and json.load(open(configjson, "rb"))!=config:
        message(options, "freeze_update")
        return

    playbackjmp=opj(options.appington_dir, "playback.jmp")
    if os.path.exists(playbackjmp):
        playback=get_asset(options, "playback.jmp")
        if playback is not None:
            options.frozen=True
            if playback!=open(playbackjmp, "rb").read():
                message(options, "freeze_update")


def main(options):
    update_check(options)
    if options.freeze_campaigns:
        do_freeze(options)
        if not options.messages:
            message(options, "freeze_build_run")
        return
    extract_app(options)
    examine_plist(options)
    check_ios_version(options)
    check_app_version(options)
    options.frozen=False
    check_assets(options)
    if options.frozen:
        message(options, "frozen_true_issues" if options.messages else "frozen_true")
    else:
        message(options, "frozen_false")

def do_freeze(options):
    if not options.internet:
        message(options, "freeze_requires_internet")
    if not os.path.isdir(options.input):
        message(options, "freeze_needs_dir", input=options.input)
    for d in os.listdir(options.input):
        if d.endswith(".xcodeproj") and os.path.isdir(opj(options.input, d)):
            break
    else:
        message(options, "freeze_needs_source", input=options.input)

    if not options.bundle_id:
        message(options, "freeze_bundle_id")

    playback=None
    config=get_asset(options, "config.json")
    if config is None:
        return
    adir=opj(options.input, "appington")
    if not os.path.isdir(adir):
        os.mkdir(adir)
    with open(opj(adir, "config.json"), "wb") as f:
        json.dump(config, f, sort_keys=True)

    playback=get_asset(options, "playback.jmp")
    if not playback:
        message(options, "freeze_no_campaigns")
    with open(opj(adir, "playback.jmp"), "wb") as f:
        f.write(playback)

def message(options, id, **kwargs):
    assert id in messages
    options.messages.append( (id, kwargs) )
    if messages[id].get("fatal", False):
        sys.exit(1)

def print_messages(options):
    if not options.messages:
        return
    for i,(id, args) in enumerate(options.messages):
        print()
        prefix="{{id=%s}} " % id if options.message_ids else ""
        print(prefix+(messages[id]["msg"] % args), file=sys.stderr)

messages={
    "bundle_sdk_version": {
        "msg": "The app is using SDK version %(version)s - this SDK is %(expected)s.  Please update the SDK or app as appropriate."
        },
    "freeze_build_run": {
        "msg": "Your app successfully has current campaigns frozen into it.  Please rebuild the app and rerun the checker on your built .app or .ipa.  Also see the 'Freezing Campaigns' section of the documentation."
        },
    "freeze_bundle_id": {
        "msg": "Please use --bundle-id to specify the bundle id for the final published application",
        "fatal": True
        },
    "freeze_needs_input": {
        "msg": "The parameter needs to be a directory: %(input)s",
        "fatal": True
        },
    "freeze_needs_source": {
        "msg": "The parameter needs to be the top level directory of the project source code: %(input)s",
        "fatal": True
        },
    "freeze_no_campaigns": {
        "msg": "Campaigns are not ready for freezing.  Please work with Appington on your campaigns.",
        "fatal": True
        },
    "freeze_requires_internet": {
        "msg": "Freezing campaigns requires Internet access",
        "fatal": True
        },
    "frozen_false": {
        "msg": "This app should be used for development.  You need to use --freeze-campaigns when readying for app store submission"
        },
    "frozen_true": {
        "msg": "This app has been frozen and can be used in production"
        },
    "frozen_true_issues": {
        "msg": "This app has been frozen, but there are outstanding issues"
        },
    "freeze_update": {
        "msg": "The frozen configuration and campaigns are out of date.  Please rerun with --freeze-campaigns"
        },
    "internet_error": {
        "msg": "Error accessing url %(urls)s: %(error)s",
        "fatal": True
        },
    "ios_version": {
        "msg": "The bundle has a minimum iOS version earlier than Appington supports\nAppington requires iOS 5 and above while the Info.plist says %(requested)s and above."
        },
    "ipa_apps": {
        "msg": "Found more than one application bundle in IPA archive: %(apps)s",
        "fatal": True
        },
    "ipa_no_app": {
        "msg": "Couldn't find any application bundles in IPA archive: %(filename)s",
        "fatal": True
        },
    "ipa_paths": {
        "msg": "Doesn't look like an IPA archive: %(filename)s",
        "fatal": True
        },
    "ipa_error": {
        "msg": "Error working with IPA app archive: %(error)s",
        "fatal": True
        },
    "no_app_binary": {
        "msg": "Couldn't find App main executable.  Was expecting %(path)s",
        "fatal": True
        },
    "not_app": {
        "msg": "Expected the directory to end in .app: %(directory)s",
        "fatal": True,
        },
    "no_bundleid": {
        "msg": "Unable to determine the bundle id from the Info.plist.  Please use --bundle-id to set it manually",
        "fatal": True
        },
    "no_plist": {
        "msg": "Could not find Info.plist in application bundle",
        "fatal": True
        },
    "no_sdk_used": {
        "msg": "The Appington SDK does not appear to be included in this application executable"
        },
    "not_provisioned": {
        "msg": "This bundle id (%(bundle_id)s) has not been provisioned with Appington yet"
        },
    "plist_json": {
        "msg": "Couldn't decode Info.plist in application bundle: %(err)s",
        "fatal": True
        },
    "runcmd": {
        "msg": "Running command failed: %(cmd)s\n  %(err)r",
        "fatal": True
        },
    "sdk_update": {
        "msg": "An update to the Appington SDK is available to version %(version)s from\nhttps://cdn.appington.com/updates/sdk/appington-ios-sdk-%(version)s.zip",
        },
    "sdk_version": {
        "msg": "Warning: %(err)s",
        },
}


if __name__=='__main__':
    import argparse

    p=argparse.ArgumentParser(formatter_class=argparse.RawDescriptionHelpFormatter,
        description="Checks apps and freezes campaigns", epilog="""
Example usage to check an app as an ipa:

  %(prog)s apps/published/myApp.ipa

Example usage to check an app as a directory:

  %(prog)s apps/dev/myapp/myApp.app

Example usage to freeze campaigns:

  %(prog)s --freeze-campaigns --bundle-id com.example.myapp apps/source/myapp

""")

    # Undocumented options, also used during testing
    p.add_argument("--update-url", default="https://cdn.appington.com/updates/sdk/iossdkinfo.json", help=argparse.SUPPRESS)
    # Include message ids to help with testing
    p.add_argument("--message-ids", default=False, action="store_true", help=argparse.SUPPRESS)

    # Documented options
    p.add_argument("--freeze-campaigns", default=False, action="store_true", help="Creates/updates campaigns in your source directory")
    p.add_argument("--bundle-id", help="When running --freeze-campaigns use this bundle id")
    p.add_argument("--no-internet", default=True, action="store_false", dest="internet", help="Disable checks that need Internet connectivity")
    p.add_argument("input", help="The file or directory to act on.  This should be the built .app directory or .ipa archive, unless --freeze-campaigns is specified in which case it should be the top level project source directory")

    try:
        options=p.parse_args()

        if not os.path.isdir(options.input) and not os.path.isfile(options.input):
            p.error("You must supply an IPA file or .app bundle directory to work on")

    except SystemExit as e:
        if "-h" not in sys.argv and "--help" not in sys.argv:
            print("Use --help to see example usages", file=sys.stderr)
        raise

    if os.path.isdir(options.input):
        while options.input.endswith("/"):
            options.input=options.input[:-1]

    tempd=tempfile.mkdtemp(prefix="sdkchecker")
    try:
        options.tempd=tempd
        options.messages=[]

        main(options)
        if len(options.messages):
            sys.exit(2)
    finally:
        print_messages(options)
        shutil.rmtree(tempd)


