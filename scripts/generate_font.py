#!/usr/bin/env python3
import os
import subprocess
import requests
import json
import re
import traceback
import tempfile

from socketserver import ThreadingMixIn
from http.server import SimpleHTTPRequestHandler,HTTPServer

from fontTools.ttLib import TTFont

if not os.path.exists("results"):
    os.makedirs("results")
if not os.path.exists("fonts"):
    os.makedirs("fonts")

def parse(font_name, temp):
    result_path = "results/%s.json" % font_name
    if os.path.exists(result_path):
        with open(result_path, "rb") as f:
            return json.loads(f.read())

    ttf_path = "fonts/%s.ttf" % font_name

    if not os.path.exists(ttf_path):
        ttf_content = requests.get("http://static.jjwxc.net/tmp/fonts/jjwxcfont_%s.ttf" % font_name)
        if ttf_content.status_code != 200:
            raise Exception("font %s is not found" % font_name)
        with open(ttf_path, "wb") as f:
            f.write(ttf_content.content)

    ttf = TTFont(ttf_path, 0, allowVID=0, ignoreDecompileErrors=True, fontNumber=-1)

    char_map = {}
    for x in ttf["cmap"].tables:
        for y in x.cmap.items():
            char_unicode = chr(y[0])
            if char_unicode == "x":
                continue
            char_map[char_unicode] = "?"
    ttf.close()

    txt_path = os.path.join(temp.name, "%s.txt" % font_name)
    with open(txt_path, "w") as f:
        all_chars = list(char_map.keys())
        f.write("\n\n\n".join(["".join(all_chars[i:i+20]) for i in range(0, len(char_map), 20)]))

    img_path = os.path.join(temp.name, "%s.png" % font_name)
    subprocess.call(["convert", "-font", ttf_path, "-pointsize", "64", "-background", "rgba(0,0,0,0)",
        "label:@%s" % txt_path, img_path])
    print(img_path)

    tesseract_result = os.path.join(temp.name, "result")
    print(tesseract_result)
    subprocess.call(["tesseract", img_path, tesseract_result, "-l", "chi_sim"])


    char_map = {}
    with open(tesseract_result + ".txt") as f:
        # remove single byte characters
        ct = re.sub("[\x00-\x7F]+", "", f.read())
        if len(all_chars) != len(ct):
            raise Exception("%d chars but %d recognized" % (len(all_chars), len(ct)))
        for i in range(len(all_chars)):
            char_map["%x" % ord(all_chars[i])] = ct[i]
    
    print(len(char_map))
    print(char_map)

    with open(result_path, "wb") as f:
        f.write(json.dumps(char_map).encode("utf-8"))

    return char_map

class ThreadingServer(ThreadingMixIn, HTTPServer):
    pass

class RequestHandler(SimpleHTTPRequestHandler):
    def do_GET(self):
        ret = {
            "status": 0,
            "data": None
        }
        f = re.findall("/(?:jjwxcfont_|)(.+).json", self.path)
        if not f:
            ret["status"] = 400
            ret["error"] = "no font specified"
        else:
            n = f[0] 
            t = tempfile.TemporaryDirectory()
            try:
                r = parse(n, t)
            except Exception as ex:
                print(traceback.format_exc())
                ret["status"] = 500
                ret["error"] = str(ex)
            else:
                ret["data"] = r
            t.cleanup()
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()

        self.wfile.write(json.dumps(ret).encode("utf-8"))

if __name__ == '__main__':
    server = ThreadingServer(('localhost', 62228), RequestHandler)
    print('Starting server, use <Ctrl-C> to stop')
    server.serve_forever()
