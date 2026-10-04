import contextlib
import json
import os
import sys
import traceback


def emit(value):
    sys.stdout.write(json.dumps(value, ensure_ascii=False, allow_nan=False) + "\n")
    sys.stdout.flush()


def json_default(value):
    if hasattr(value, "tolist"):
        return value.tolist()
    if hasattr(value, "item"):
        return value.item()
    raise TypeError(f"Cannot serialize {type(value).__name__}")


def main():
    # Reserve stdout for the protocol, including native library writes to fd 1.
    protocol = os.fdopen(os.dup(sys.stdout.fileno()), "w", encoding="utf-8", buffering=1)
    os.dup2(sys.stderr.fileno(), sys.stdout.fileno())
    sys.stdout = protocol
    config = json.loads(sys.stdin.readline())
    with contextlib.redirect_stdout(sys.stderr):
        import torch
        from doctr.io import DocumentFile
        from doctr.models import ocr_predictor

        device = torch.device(config["device"])
        model = ocr_predictor(
            det_arch=config["detection_architecture"],
            reco_arch=config["recognition_architecture"],
            pretrained=True,
            assume_straight_pages=config["assume_straight_pages"],
            detect_orientation=config["detect_orientation"],
            straighten_pages=config["straighten_pages"],
            detect_language=config["detect_language"],
        ).to(device).eval()
    emit({"ready": True})

    for line in sys.stdin:
        try:
            request = json.loads(line)
            with contextlib.redirect_stdout(sys.stderr), torch.inference_mode():
                if request["kind"] == "pdf":
                    pages = DocumentFile.from_pdf(request["paths"][0])
                elif request["kind"] == "images":
                    pages = DocumentFile.from_images(request["paths"])
                else:
                    raise ValueError("Unsupported document kind")
                document = model(pages)
                # Normalize NumPy scalars/arrays, including rotated polygon geometry.
                export = json.loads(json.dumps(document.export(), default=json_default, allow_nan=False))
                result = {"text": document.render(), "document": export}
            emit({"result": result})
        except Exception as error:
            traceback.print_exc(file=sys.stderr)
            emit({"error": f"{type(error).__name__}: {error}"})


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        traceback.print_exc(file=sys.stderr)
        emit({"error": f"{type(error).__name__}: {error}"})
        sys.exit(1)
