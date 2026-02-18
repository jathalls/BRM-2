#!/usr/bin/env python3
import subprocess, json, time, threading, os, sys
vsdbg_path=os.path.expanduser('~/.vscode/extensions/ms-dotnettools.csharp-2.120.3-darwin-x64/.debugger/x86_64/vsdbg')
log_path='/tmp/vsdbg_dap_session.log'
if not os.path.exists(vsdbg_path):
    print('vsdbg not found at', vsdbg_path); sys.exit(1)
proc=subprocess.Popen([vsdbg_path,'--interpreter=vscode','--engineLogging=/tmp/vsdbg_engine.log'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)

def reader():
    try:
        with open(log_path,'w') as lf:
            while True:
                hdr=''
                while True:
                    line=proc.stdout.readline()
                    if not line:
                        return
                    hdr+=line
                    if line.strip()=='' or line.endswith('\r\n\r\n'):
                        break
                # parse content-length
                clen=0
                for l in hdr.splitlines():
                    if l.lower().startswith('content-length:'):
                        try:
                            clen=int(l.split(':',1)[1].strip())
                        except:
                            clen=0
                body=''
                if clen>0:
                    body=proc.stdout.read(clen)
                entry=(hdr+body)
                lf.write(entry+"\n---\n")
                lf.flush()
                print(entry)
    except Exception as e:
        print('reader exception', e)

thr=threading.Thread(target=reader,daemon=True)
thr.start()

def send(msg):
    b=json.dumps(msg)
    hdr=f"Content-Length: {len(b)}\r\n\r\n"
    proc.stdin.write(hdr+b)
    proc.stdin.flush()

# initialize
init={"seq":1,"type":"request","command":"initialize","arguments":{"clientID":"assistant","adapterID":"coreclr"}}
send(init)
# wait
time.sleep(0.5)

# launch - mirror launch.json
launchArgs={
  "program":"/usr/local/share/dotnet/dotnet",
  "args":["run","--project","/Users/justinHalls/Documents/test/BRM-2/BRM-2.csproj","-f","net10.0-maccatalyst","-c","Debug"],
  "cwd":"/Users/justinHalls/Documents/test/BRM-2",
  "stopAtEntry":True,
  "console":"integratedTerminal"
}
launch={"seq":2,"type":"request","command":"launch","arguments":launchArgs}
send(launch)

# give it time to run; print status
for i in range(60):
    time.sleep(0.5)
    if proc.poll() is not None:
        print('vsdbg exited with', proc.returncode)
        break

print('Finished. Adapter log:', '/tmp/vsdbg_engine.log', 'DAP session log:', log_path)
proc.stdin.close()
proc.terminate()
proc.wait()
