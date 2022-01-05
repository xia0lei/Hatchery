import sys, os, re

proto_path = os.getcwd()
out_path = os.getcwd() + "/CommonMsgID.cs"

config = {
    "common": 900
}

def ParseMsgIDDefine(fs,msgidList):
    fs.writelines("public enum CommonMsgID");
    fs.writelines("{");

    for _msgDef in msgidList:
        fs.writelines("\t%s = %s,"%( _msgDef.msgname.upper().replace(".","_"), _msgDef.msgid));

    fs.writelines("}");
    fs.flush();
    fs.close();

class MsgInfo(object):
    def __init__(self,msgid,msgname):
        self.msgid = msgid
        self.msgname = msgname

        msgid = ""
        msgname = ""

def parse_msgfile():
    msg_info_list = []
    for root, dirs, files in os.walk(proto_path):
        for name in files:
            basename = os.path.splitext(name)[0]
            extname = os.path.splitext(name)[1]
            if extname <> ".proto":
                continue
            if not config.has_key(basename):
                continue
            currentIndex = config[basename] + 1
            f = open("%s/%s" % (proto_path, name))
            content = f.read()
            pattern = re.compile(r'package (\w+);')
            match = pattern.search(content)
            if not match:
                print "not match package name %s" % name
                return
            pattern = re.compile(r'message (\w+)')
            match = pattern.search(content)
            if match:
                for message in re.findall(pattern, content):
                    totalName = "%s.%s" % (basename, message)
                    msg_info_list.append(MsgInfo(currentIndex,totalName))
                    currentIndex = currentIndex + 1
            print "process %s file" % name
        return msg_info_list

class WrapFile:
        fs = None
        def __init__(self,real_file):
            self.fs = real_file
        def writelines(self,s):
            self.fs.write(s + "\n")
        def flush(self):
            self.fs.flush()
        def close(self):
            self.fs.close()

l=parse_msgfile()

f = WrapFile(open(out_path,"w+"))
ParseMsgIDDefine(f,l)
