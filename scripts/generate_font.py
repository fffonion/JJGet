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
CHAR_LT = 10000
FUZZ = 20
CONFIDENCY = 0.4
HIGH_CONFIDENCY = 0.9

PLACEHOLDER_LEARNED = "-------"

# LEARN
# char: coord
coord_map = {}
coord_map_candidate = {}
candidate_names = set()

def is_glpyh_similar(a, b, fuzz):
    if len(a) != len(b):
        return False
    found = True
    for i in range(len(a)):
        if abs(a[i][0] - b[i][0]) > fuzz or abs(a[i][1] - b[i][1]) > fuzz:
            found = False
            break
    return found

def load_candidate(name):
    global coord_map_candidate
    global candidate_names

    with open(os.path.join("results-ocr", name + ".json")) as f:
        try:
            m = json.loads(f.read())             
        except:
            return
    ttf_path = os.path.join("fonts", name + ".ttf")
    if not os.path.exists(ttf_path):
        return
    if name in candidate_names:
        print("skip already loaded %s" % name)
        return

    with TTFont(ttf_path, 0, allowVID=0, ignoreDecompileErrors=True, fontNumber=-1) as ttf:
        cmap = ttf["cmap"]
        known = set()
        for x in cmap.tables:
            for y in x.cmap.items():
                if y[0] < CHAR_LT:
                    continue
                u = "%x" % y[0]
                # OCR previously failed
                if u not in m:
                    continue
                coord = ttf['glyf'][y[1]].coordinates
                # one coordinates only adds once in each font
                coord_key = str(coord)
                if coord_key in known:
                    continue
                known.add(coord_key)

                i = m[u]
                l = len(coord)
                if i not in coord_map_candidate:
                    coord_map_candidate[i] = {}
                if l not in coord_map_candidate[i]:
                    coord_map_candidate[i][l] = []
                # the character is already learned
                if coord_map_candidate[i][l] == PLACEHOLDER_LEARNED:
                    continue
                coord_map_candidate[i][l].append(list(coord))

    candidate_names.add(name)
    print(name + " loaded into candidates")

def learn_candidate(fuzz):
    global coord_map

    for k in coord_map_candidate:
        cans_font_types = coord_map_candidate[k]
        # lujing changdu
        for path_length in cans_font_types:
            cans = cans_font_types[path_length]
            # skip known characters
            if cans == None:
                continue
            if len(cans) == 1 or len(cans) < len(candidate_names) * CONFIDENCY:
                print("%s doesn't have enough data to learn (has %d, need %d)" % (
                    k, len(cans), len(candidate_names) * CONFIDENCY
                ))
                continue
            agreed = 0
            for i in range(1, len(cans)):
                if is_glpyh_similar(cans[0], cans[i], fuzz):
                    agreed += 1
            if agreed > (len(cans) - 1) * CONFIDENCY:
                print("learned %s with %d/%d" % (k, agreed, len(cans)))
                coord_map[k] = cans[0]
                if agreed > (len(cans) - 1) * HIGH_CONFIDENCY:
                    # clear the candidate buffer, stop further learning
                    cans_font_types[path_length] = PLACEHOLDER_LEARNED
            else:
                print(k, "failed", agreed, (len(cans) - 1) * CONFIDENCY)

    print("total characters learned %d" % (len(coord_map)))

    with open("fast_compare.json", "w") as f:
        f.write(json.dumps([coord_map, coord_map_candidate, list(candidate_names)]))

def preload(fuzz):
    global coord_map
    global coord_map_candidate
    global candidate_names

    if os.path.exists("fast_compare.json"):
        with open("fast_compare.json") as f:
           [coord_map, coord_map_candidate, candidate_names] = json.loads(f.read())
        print("loaded %d characters" % (len(coord_map)))
        candidate_names = set(candidate_names)

    for p in os.listdir("results-ocr"):
        if not p.endswith(".json"):
            continue
        load_candidate(os.path.splitext(p)[0])
    
    print("preload %d candidates" % len(candidate_names))

    learn_candidate(fuzz)

# FIND
def find_by_coord(ttf, fuzz):
    result = {}
    if not coord_map:
        return result

    missing = set()
    cmap = ttf["cmap"]
    for x in cmap.tables:
        for y in x.cmap.items():
            tc = "%x" % y[0]
            if tc in result or y[0] < CHAR_LT:
                continue
            coord = list(ttf['glyf'][y[1]].coordinates)
            for rc in coord_map:
                if len(coord) != len(coord_map[rc]):
                    continue
                ico = coord_map[rc]
                if is_glpyh_similar(coord, ico, fuzz):
                    #print("found ", tc, "==", rc, "(", known_chars[rc], ")")
                    result[tc] = rc
                    if tc in missing:
                        missing.remove(tc)
                    break
                # current char might match a different path length
                # remember it and remove when found
                if tc not in result:
                    missing.add(tc)
    for k in missing:
        result[k] = "?"
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

def parse(font_name, temp, coord_fuzz=40):
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
    missing = set()
    for k in char_map:
        if char_map[k] == "?":
            missing.add(k)
    if not char_map or len(missing):
        if missing:
            print("fast compare missing %d: %s" % (len(missing), ",".join(missing)))
        errors = []
        for sz in (64, 96):
            try:
                char_map = find_by_ocr(ttf, font_name, temp, 64)
            except Exception as ex:
                errors.append("%d:%s" % (sz, str(ex)))
            else:
                break
        
        if errors:
            raise Exception(", ".join(errors))
        # save result for fast compare
        with open(os.path.join("results-ocr", "%s.json" % font_name), "wb") as f:
            f.write(json.dumps(char_map).encode("utf-8"))
        
        load_candidate(font_name)
        learn_candidate(coord_fuzz)
    else:
        print("solved by fast compare")
    
    with open(result_path, "wb") as f:
        f.write(json.dumps(char_map).encode("utf-8"))
    
    ttf.close()

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
    preload(FUZZ)

    server = ThreadingServer(('localhost', 62228), RequestHandler)
    print('Starting server, use <Ctrl-C> to stop')
    server.serve_forever()
