"""Read-only Unity YAML audit of the known laboratory bindings; not a general prefab importer.
World origins include parent TRS and the relevant scene overrides. Runtime renderer bounds
are deliberately not guessed from FBX data: Unity computes those for every request.
"""
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
SCENE = ROOT / "Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity"
PREFAB = ROOT / "Assets/Prefabs/Lab/Microscope Exploder.prefab"


def blocks(path):
    return {m[2]: m[3] for m in re.finditer(r"^--- !u!(\d+) &(\d+).*?\n(.*?)(?=^--- !u!|\Z)",
        path.read_text(encoding="utf-8-sig"), re.M | re.S)}


def vector(body, key):
    match = re.search(r"\b" + key + r": \{([^}]+)\}", body)
    if not match:
        raise ValueError("Unresolved transform: " + key)
    return {k: float(v) for k, v in re.findall(r"([xyzw]): ([^,}]+)", match[1])}


def parent(body, key="m_Father"):
    return re.search(key + r": \{fileID: (\d+)", body)[1]


def overrides(body, target):
    return {m[2]: float(m[3]) for m in re.finditer(
        r"- target: \{fileID: (\d+),[^\n]+\n\s+propertyPath: (m_Local(?:Position|Rotation|Scale)\.[xyzw])\n\s+value: ([^\n]+)", body)
        if m[1] == target}


def trs(body, changes=None):
    values = {key: vector(body, key) for key in ("m_LocalPosition", "m_LocalRotation", "m_LocalScale")}
    for key, value in (changes or {}).items():
        field, axis = key.split("."); values[field][axis] = value
    p, q, s = (values[key] for key in ("m_LocalPosition", "m_LocalRotation", "m_LocalScale"))
    x, y, z, w = (q[k] for k in "xyzw")
    rotation = [[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]]
    return [[rotation[i][j] * s["xyz"[j]] for j in range(3)] + [p["xyz"[i]]] for i in range(3)] + [[0,0,0,1]]


IDENTITY = [[int(i == j) for j in range(4)] for i in range(4)]


def mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def point(matrix, p=(0,0,0)):
    return [round(sum(matrix[i][j]*p[j] for j in range(3)) + matrix[i][3], 6) for i in range(3)]


def audit():
    scene, prefab = blocks(SCENE), blocks(PREFAB)
    def world(node):
        if node == "0": return IDENTITY
        body = scene[node]
        return mul(world(parent(body)), trs(body))
    micro_instance = scene["1294495121160519477"]
    micro_changes = overrides(micro_instance, "8339603104678161946")
    micro_local = [micro_changes["m_LocalPosition." + axis] for axis in "xyz"]
    micro_world = point(world(parent(micro_instance, "m_TransformParent")), micro_local)
    exploded_instance = scene["52920573"]
    def exploded_world(node):
        body = prefab[node]; up = parent(body)
        outer = world(parent(exploded_instance, "m_TransformParent")) if up == "0" else exploded_world(up)
        return mul(outer, trs(body, overrides(exploded_instance, node)))
    mode = scene["9014170785358182102"]
    assert "microscopeRoot: {fileID: 831527608}" in mode
    bindings = scene["1062572443"]
    assert re.search(r"partTransform: \{fileID: 52920579\}.*?Start Numerical Aperture Experiment", bindings, re.S)
    assert re.search(r"partTransform: \{fileID: 52920580\}.*?Start Spatial Frequency Experiment", bindings, re.S)
    return {
        "scene": str(SCENE.relative_to(ROOT)).replace("\\", "/"),
        "scene_sha256": hashlib.sha256(SCENE.read_bytes()).hexdigest(),
        "verification": "static_asset_audit_not_playmode",
        "units": "Unity world units; runtime assumes 1 unit/metre unless configured",
        "note": "These are transform origins, not renderer bounds centres or live player coordinates.",
        "microscope": {"scene_gameobject_file_id": "831527608", "prefab": "Assets/FBX/TEMP/Microscope Variant.prefab",
                       "local_origin": micro_local, "world_origin": micro_world,
                       "navigation_ids": ["parts", "na_experiment", "spatial_frequency"]},
        "upper_optical_assembly": {"scene_transform_file_id": "52920579", "world_origin": point(exploded_world("6851547645105698199")),
                                   "button": "Start Numerical Aperture Experiment", "target_id": "na_experiment"},
        "objective_lens": {"scene_transform_file_id": "52920580", "world_origin": point(exploded_world("4602032416816278019")),
                           "button": "Start Spatial Frequency Experiment", "target_id": "spatial_frequency"},
        "snom": {"scene_transform_file_id": "543450651668294328", "world_origin": point(world("543450651668294328")),
                 "name": "TDs_edited_UnityVeryLowPoly", "target_id": "snom_entry"},
        "serialized_camera": {"scene_transform_file_id": "9014170785400826611", "world_origin": point(world("9014170785400826611")),
                              "note": "Runtime uses Camera.main; XR tracking and locomotion replace this pose."}
    }


if __name__ == "__main__":
    result = audit()
    output = ROOT / "docs/assistant_knowledge/navigation_scene_audit.json"
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2))
