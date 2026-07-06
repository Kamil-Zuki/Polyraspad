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
import easyocr
_ = easyocr.Reader(['en'], gpu=False, verbose=False)
PY

COPY . .

EXPOSE 50052

CMD ["python", "main.py"]
