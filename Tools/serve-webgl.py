"""Serve Build/WebGL with the gzip headers Unity's loader expects.

Unity's WebGL build is pre-compressed (.gz). A plain static server hands those
files over without Content-Encoding, and the loader fails to parse them.
"""
import functools
import http.server
import socketserver

PORT = 8080
ROOT = "Build/WebGL"


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        if self.path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        super().end_headers()


if __name__ == "__main__":
    socketserver.TCPServer.allow_reuse_address = True
    with socketserver.TCPServer(("", PORT), functools.partial(Handler, directory=ROOT)) as httpd:
        print(f"serving {ROOT} on http://localhost:{PORT}", flush=True)
        httpd.serve_forever()
