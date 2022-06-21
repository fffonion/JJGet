#!/usr/bin/env python3
from configparser import RawConfigParser
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
CONFIDENCY = 0.3
CONFIDENT_MINIMUM_VOTER = 10 # how many minimum voters to reach agreement
FONT_VARIANT_COUNT = 5
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
        except Exception as ex:
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
                if i not in coord_map_candidate:
                    coord_map_candidate[i] = []
                # the character is already learned
                # if coord_map_candidate[i][l] == PLACEHOLDER_LEARNED:
                #     continue
                coord_map_candidate[i].append(list(coord))

    candidate_names.add(name)
    print(name + " loaded into candidates")

def learn_candidate(fuzz):
    global coord_map

    for k in coord_map_candidate:
        cans = coord_map_candidate[k]
        # skip known characters
        if cans == None or cans == PLACEHOLDER_LEARNED:
            continue
        cand_confi = len(cans) / len(candidate_names)
        if len(cans) < 3:
            print("%s doesn't have enough data to learn (has %d, need %d)" % (
                k, len(cans), len(candidate_names) * CONFIDENCY
            ))
            continue

        # for each glyph, found who agree with more than threshold confidency
        vote_collected = set() # remember whose vote already contribute to a group
        for i in range(len(cans)):
            if cans[i] == PLACEHOLDER_LEARNED or i in vote_collected:
                continue

            agreed = 0
            total = 0
            current_agreed = set() # current round of vote group
            current_agreed.add(i) # me agree with myself
            for j in range(len(cans)):
                if i == j or cans[j] == PLACEHOLDER_LEARNED or j in vote_collected:
                    continue
                if is_glpyh_similar(cans[i], cans[j], fuzz):
                    agreed += 1
                    current_agreed.add(j)
                total += 1

            if total == 0:
                continue
            # confidency of currnet group
            confi = agreed / total
            # the confidency over all fonts
            confi_overall = agreed / len(candidate_names)
            if confi > CONFIDENCY:
                if agreed < CONFIDENT_MINIMUM_VOTER:
                    print("%s reached confidency %.2f, but only %d agreed, need at least %d" %
                        (k, confi_overall, agreed, CONFIDENT_MINIMUM_VOTER))
                    continue

                # memoize the result
                if k not in coord_map:
                    coord_map[k] = []
                updated = False
                for n in range(len(coord_map[k])):
                    existing = coord_map[k][n]
                    if str(existing[-1]) == str(cans[i]):
                        updated = True
                        print("+  updated %s confi increased from %.2f to %.2f" % (k, existing[0], confi))
                        coord_map[k][n] = (confi_overall, len(current_agreed), cans[i])
                        break
                if not updated:
                    coord_map[k].append((confi_overall, len(current_agreed), cans[i]))
                coord_map[k] = sorted(coord_map[k], key=lambda x: x[0], reverse=True)
                # truncate
                if len(coord_map[k]) > FONT_VARIANT_COUNT:
                    print("*  drop fast_compare map entry for %s (has %d)" % (k, len(coord_map[k])))
                    coord_map[k] = coord_map[k][:FONT_VARIANT_COUNT]

                print("+ learned %s with %d/%d top 3 confi %s" % (
                    k, agreed, total, ", ".join(["%.2f" % j[0] for j in coord_map[k][:3]])
                ))

                for i in current_agreed:
                    vote_collected.add(i)
                
                # drop
                if confi > HIGH_CONFIDENCY and cand_confi > HIGH_CONFIDENCY:
                    # print("*  mark %d as clear" % len(current_agreed))
                    # mark candidate buffer as clear, clear outside of the loop
                    for i in current_agreed:
                        coord_map_candidate[k][i] = PLACEHOLDER_LEARNED
            else:
                print(k, "- failed", agreed, (len(cans) - 1) * CONFIDENCY)

        # clean up
        coord_map_candidate[k] = [j for j in coord_map_candidate[k] if j != PLACEHOLDER_LEARNED]

    print("+ total characters learned %d (%d variants)" % (len(coord_map), sum([len(coord_map[k]) for k in coord_map])))

    with open("fast_compare_full.json", "w") as f:
        f.write(json.dumps([coord_map, coord_map_candidate, list(candidate_names)]))
    with open("fast_compare.json", "w") as f:
        f.write(json.dumps([coord_map, {}, []]))

def preload(fuzz):
    global coord_map
    global coord_map_candidate
    global candidate_names

    for ff in ["fast_compare_full.json", "fast_compare.json"]:
        if os.path.exists(ff):
            with open(ff) as f:
                [coord_map, coord_map_candidate, candidate_names] = json.loads(f.read())
                print("> loaded %d characters" % (len(coord_map)))
                candidate_names = set(candidate_names)
            print("> loaded " + ff)
            break

    for p in os.listdir("results-ocr"):
        if not p.endswith(".json"):
            continue
        load_candidate(os.path.splitext(p)[0])
    
    print("> preload %d candidates" % len(candidate_names))

    learn_candidate(fuzz)

# FIND
def find_by_coord(ttf, fuzz):
    result = {}
    if not coord_map:
        return result

    cmap = ttf["cmap"]
    for x in cmap.tables:
        for y in x.cmap.items():
            tc = "%x" % y[0]
            if tc in result or y[0] < CHAR_LT:
                continue
            coord = list(ttf['glyf'][y[1]].coordinates)
            cans = []
            for rc in coord_map:
                for group in coord_map[rc]:
                    confi, count, ico = group
                    if len(coord) != len(ico):
                        continue
                    if is_glpyh_similar(coord, ico, fuzz):
                        # print("found ", tc, "==", rc)
                        # confidency first, then compare agreed voters count
                        cans.append((rc, (confi, count)))
            
            if cans:
                if len(cans) > 1:
                    print(cans)
                cans = sorted(cans, key=lambda x: x[1], reverse=True)
                result[tc] = cans[0][0]
    return result

def find_by_ocr(ttf_chars, font_name, temp, pointsize):
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
    ttf_chars = {}
    for x in ttf["cmap"].tables:
        for y in x.cmap.items():
            char_unicode = chr(y[0])
            if char_unicode == "x":
                continue
            ttf_chars[char_unicode] = "?"

    char_map = {}
    # try coord
    char_map = find_by_coord(ttf, coord_fuzz)
    missing = set()
    for k in ttf_chars:
        kk = "%x" % ord(k)
        if kk not in char_map:
            missing.add(kk)
    if not char_map or len(missing) > 0:
        if len(missing) > 0:
            print("fast compare missing %d: %s" % (
                len(missing), ",".join(missing)
            ))
        errors = []
        ocr_char_map = {}
        for sz in (64, 96):
            try:
                ocr_char_map = find_by_ocr(ttf_chars, font_name, temp, 64)
            except Exception as ex:
                errors.append("%d:%s" % (sz, str(ex)))
            else:
                break
        
        if errors:
            raise Exception(", ".join(errors))
        # save result for fast compare
        with open(os.path.join("results-ocr", "%s.json" % font_name), "wb") as f:
            f.write(json.dumps(ocr_char_map).encode("utf-8"))
        
        # only fill in unknowns
        for k in missing:
            char_map[k] = ocr_char_map[k]
        
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
