"""Generated protobuf and gRPC modules."""

import sys
from pathlib import Path

# Add current directory and subdirectories to sys.path so generated protobuf imports resolve
_proto_dir = Path(__file__).parent
if str(_proto_dir) not in sys.path:
    sys.path.insert(0, str(_proto_dir))

_inclusive_dir = _proto_dir / "Inclusive"
if str(_inclusive_dir) not in sys.path:
    sys.path.insert(0, str(_inclusive_dir))

try:
    import agent_pb2
    import agent_pb2_grpc
    import vocabulary_pb2
    import vocabulary_pb2_grpc
    import vocab_pb2
    import vocab_pb2_grpc
except ImportError:
    pass

__all__ = [
    "agent_pb2",
    "agent_pb2_grpc",
    "vocabulary_pb2",
    "vocabulary_pb2_grpc",
    "vocab_pb2",
    "vocab_pb2_grpc",
]
