# -*- coding: UTF-8 -*-
import sys, os, re
import pystache
config = {
    "SkynetMessageReceiver" : 100
}


c = {
    'indexToName': []
}



def main():
    for root, dirs, files in os.walk('.'):
        for name in files:
            basename = os.path.splitext(name)[0]
            extname = os.path.splitext(name)[1]
            if extname <> ".proto":
                continue
            if not config.has_key(basename):
                continue

            currentIndex = config[basename] + 1


            f = open(name)
            content = f.read()
            pattern = re.compile(r'package (\w+);')
            match = pattern.search(content)
            if not match:
                print "not match package name %s" % name
                return
            if match.group() <> basename:
                print "not match package name %s != %s" % (name, match.group())

            pattern = re.compile(r'message (\w+)')
            match = pattern.search(content)
            if match:
                for message in re.findall(pattern, content):
                    totalName = "%s.%s" % (basename, message)
                    data1 = {}
                    data1["index"] = currentIndex
                    data1["name"] = totalName
                    currentIndex = currentIndex + 1
                    c["indexToName"].append(data1)
            print "process %s file" % name


    r = pystache.Renderer()
    baseModel = open('protocols.mustache', 'r')
    baseContent = pystache.render(baseModel.read().decode('utf-8'), c)

    f1 = open('protocols.lua','w')
    f1.write(baseContent.encode("utf-8"))
    f1.close()
    print "gen all ok."
if __name__=="__main__":
    main()
