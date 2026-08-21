FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

RUN python -m grpc_tools.protoc \
    -I Protos \
    -I Protos/Inclusive \
    --python_out=src/proto \
    --grpc_python_out=src/proto \
    Protos/agent.proto \
    Protos/vocabulary.proto \
    Protos/Inclusive/vocab.proto

EXPOSE 5131

CMD ["python", "main.py"]
