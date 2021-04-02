#!/usr/bin/env python3
import os
import subprocess
import requests
import json
import re
import traceback
import tempfile
import time

from socketserver import ThreadingMixIn
from http.server import SimpleHTTPRequestHandler,HTTPServer

from fontTools.ttLib import TTFont

if not os.path.exists("results"):
    os.makedirs("results")
if not os.path.exists("results-ocr"):
    os.makedirs("results-ocr")
if not os.path.exists("fonts"):
    os.makedirs("fonts")

PER_LINE = 20

# id: char
known_chars = {}
# id: coord
coord_map = {}

def load_existing_result():
    global known_chars
    global coord_map

    if os.path.exists("fast_compare.json"):
        with open("fast_compare.json") as f:
           use_font, max_agreed, known_chars, coord_map = json.loads(f.read())
        print("loaded %s as trusted charset (%d agreed)" % (use_font, len(max_agreed)))
        return

    result_chars = {}
    for p in os.listdir("results-ocr"):
        if p.endswith(".json"):
            with open(os.path.join("results-ocr", p)) as f:
                try:
                    c = json.loads(f.read())
                    t = os.path.splitext(p)[0]
                    result_chars[t] = c                    
                except:
                    pass
    
    elected = {}
    for c in result_chars:
        ll = "".join(sorted(result_chars[c].values()))
        if ll in elected:
            elected[ll].append(c)
        else:
            elected[ll] = [c,]
    
    max_agreed = []
    for c in elected:
        agreed = elected[c]
        if len(agreed) > len(max_agreed):
            max_agreed = agreed
    
    if max_agreed == 1:
        print("not able to find a trusted charset")
        return
    
    use_font = max_agreed[0]
    print("use %s as trusted charset (%d agreed)" % (use_font, len(max_agreed)))
    known_chars = result_chars[use_font]
    
    ttf = TTFont(os.path.join("fonts", use_font+".ttf"), 0, allowVID=0, ignoreDecompileErrors=True, fontNumber=-1)
    cmap = ttf["cmap"]
    for x in cmap.tables:
        for y in x.cmap.items():
            if y[0] < 10000:
                continue
            coord = ttf['glyf'][y[1]].coordinates
            i = "%x" % y[0]
            if i in coord_map:
                continue
            coord_map[i] = [x for x in coord]
    
    with open("fast_compare.json", "w") as f:
        f.write(json.dumps([use_font, max_agreed, known_chars, coord_map]))

def find_by_coord(ttf, fuzz=40):
    result = {}
    if not coord_map:
        return result

    cmap = ttf["cmap"]
    for x in cmap.tables:
        for y in x.cmap.items():
            tc = "%x" % y[0]
            if tc in result:
                continue
            coord = [x for x in ttf['glyf'][y[1]].coordinates]
            for rc in coord_map:
                if rc in result or len(coord) != len(coord_map[rc]):
                    continue
                ico = coord_map[rc]
                found = True
                for i in range(len(coord)):
                    if abs(coord[i][0] - ico[i][0]) > fuzz or abs(coord[i][1] - ico[i][1]) > fuzz:
                        found = False
                        break
                if found:
                    #print("found ", tc, "==", rc, "(", known_chars[rc], ")")
                    result[tc] = known_chars[rc]
    return result

def find_by_ocr(ttf, font_name, temp, pointsize):
    ttf_chars = {}
    for x in ttf["cmap"].tables:
        for y in x.cmap.items():
            char_unicode = chr(y[0])
            if char_unicode == "x":
                continue
            ttf_chars[char_unicode] = "?"

    img_path = os.path.join(temp.name, "%s.png" % font_name)
    ttf_path = "fonts/%s.ttf" % font_name
    #print(img_path)
    t = time.time()
    chars = list(ttf_chars.keys())
    
    txt_path = ttf_path + ".txt"
    with open(txt_path, "w") as f:
        f.write("\n\n\n".join(["".join(chars[i:i+PER_LINE]) for i in range(0, len(chars), PER_LINE)]))

    subprocess.call(["convert", "-font", ttf_path, "-pointsize", "%d" % pointsize, "-background", "rgba(0,0,0,0)",
        "label:@%s" % txt_path, img_path])
    print("%s %.2f generate image" % (font_name, time.time() - t))

    tesseract_result = os.path.join(temp.name, "result")
    #print(tesseract_result)
    t = time.time()
    subprocess.call(["tesseract", img_path, tesseract_result, "-l", "chi_sim", "--psm", "6"])
    print("%s %.2f in OCR" % (font_name, time.time() - t))

    char_map = {}
    with open(tesseract_result + ".txt") as f:
        # remove single byte characters
        ct = re.sub("[\x00-\x7F]+", "", f.read())
        if len(chars) != len(ct):
            raise Exception("%d chars but %d recognized" % (len(chars), len(ct)))
        for i in range(len(chars)):
            char_map["%x" % ord(chars[i])] = ct[i]
    
    return char_map

def parse(font_name, temp, coord_fuzz=0):
    result_path = os.path.join("results", "%s.json" % font_name)
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
    # try coord
    char_map = find_by_coord(ttf, coord_fuzz)
    if not char_map:
        errors = []
        for sz in (64, 96):
            try:
                find_by_ocr(ttf, font_name, temp, 64)
            except Exception as ex:
                errors.append("%d:%s" % (sz, str(ex)))
            else:
                break
        
        if errors:
            raise Exception(", ".join(errors))
        # save result for fast compare
        with open(os.path.join("results-ocr", "%s.json" % font_name), "wb") as f:
            f.write(json.dumps(char_map).encode("utf-8"))
    else:
        print("solved by fast compare")
    
    with open(result_path, "wb") as f:
        f.write(json.dumps(char_map).encode("utf-8"))
    
    ttf.close()

    # load fast matching maps
    if not coord_map:
        load_existing_result()

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
            tmp = tempfile.TemporaryDirectory()
            try:
                t = time.time()
                r = parse(n, tmp)
            except Exception as ex:
                print(traceback.format_exc())
                ret["status"] = 500
                ret["error"] = str(ex)
            else:
                ret["data"] = r
            #os.system("cp -r %s/* /tmp/a" % tmp.name)
            tmp.cleanup()
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.send_header('X-Proc-Time', "%.2f" % (time.time() - t))
        self.end_headers()

        self.wfile.write(json.dumps(ret).encode("utf-8"))

if __name__ == '__main__':
    load_existing_result()

    server = ThreadingServer(('localhost', 62228), RequestHandler)
    print('Starting server, use <Ctrl-C> to stop')
    server.serve_forever()
