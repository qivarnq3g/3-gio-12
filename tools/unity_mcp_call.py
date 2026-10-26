"""Call MCP for Unity tools from a shell, without an MCP client.

Usage: python -I unity_mcp_call.py <tool> <json-args> [<tool> <json-args> ...]
Needs uv on PATH and the Unity Editor open with Window > MCP for Unity > Stdio > Start Session.
"""
# usage: python -I umcp.py '<tool>' '<json args>' [<tool> <json> ...]
import sys as _s; _s.stdout.reconfigure(encoding="utf-8")
import json, subprocess, sys, time
p = subprocess.Popen(["uvx","--from","mcpforunityserver==10.3.0","mcp-for-unity","--transport","stdio"],
    stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, encoding="utf-8")
def send(m): p.stdin.write(json.dumps(m)+"\n"); p.stdin.flush()
def recv(i):
    while True:
        l = p.stdout.readline()
        if not l: return None
        try: m = json.loads(l)
        except Exception: continue
        if m.get("id") == i: return m
send({"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"cli","version":"0"}}}); recv(1)
send({"jsonrpc":"2.0","method":"notifications/initialized"}); time.sleep(3)
a = sys.argv[1:]; i = 10
for k in range(0, len(a), 2):
    send({"jsonrpc":"2.0","id":i,"method":"tools/call","params":{"name":a[k],"arguments":json.loads(a[k+1])}})
    r = recv(i); txt = r["result"]["content"][0]["text"] if r and "result" in r else json.dumps(r)
    print(f"== {a[k]}:", txt[:3000]); i += 1
p.kill()
