FROM python:3.11-slim

WORKDIR /app

# System dependencies for EasyOCR (opencv) and pdf2image (poppler)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender-dev \
    libgomp1 \
    poppler-utils \
    wget \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Generate gRPC stubs from the proto contract
COPY proto/ocr.proto proto/ocr.proto
RUN python -m grpc_tools.protoc -I. --python_out=. --grpc_python_out=. proto/ocr.proto

# Pre-download the English EasyOCR model so the first request is not slow
RUN python - <<'PY'
import urllib.request
import time

original_urlretrieve = urllib.request.urlretrieve

def patched_urlretrieve(url, filename, reporthook=None, data=None):
    mirrors = [
        "https://ghp.ci/",
        "https://ghproxy.net/",
        "https://github.moeyy.xyz/",
        ""
    ]
    
    # If it's a GitHub URL, we can prepend a proxy mirror
    path = url.replace("https://github.com/", "") if "github.com" in url else ""
    
    for mirror in mirrors:
        if mirror and path:
            mirror_url = mirror + "https://github.com/" + path
        else:
            mirror_url = url
            
        print(f"Trying to download from {mirror_url}...")
        try:
            return original_urlretrieve(mirror_url, filename, reporthook, data)
        except Exception as e:
            print(f"Error downloading from {mirror_url}: {e}")
            time.sleep(2)
            
    raise RuntimeError(f"Could not download from any mirror: {url}")

urllib.request.urlretrieve = patched_urlretrieve

print("Initializing EasyOCR with patched downloader...")
import easyocr
_ = easyocr.Reader(['en'], gpu=False, verbose=False)
print("Successfully downloaded and initialized models.")
PY

COPY . .

EXPOSE 50052

CMD ["python", "main.py"]
