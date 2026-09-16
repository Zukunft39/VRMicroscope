"""Strict, location-only context. Coordinates are supplied by Unity, not by chat text."""
import math
import re

TARGETS = {
    "parts": ("the microscope learning area", "microscope structure and component functions", "microscope_basics"),
    "na_experiment": ("the microscope learning area", "numerical aperture, with entry linked to Upper Optical Assembly", "numerical_aperture"),
    "spatial_frequency": ("the microscope learning area", "spatial frequency, with entry linked to Objective Lens", "spatial_frequency"),
    "snom_entry": ("the THz s-SNOM instrument", "the demonstration of probe tapping, near-field coupling, and scanning", "snom"),
    "sample_red": ("the red specimen", "picking up the red fluorescence specimen for microscope observation", "microscope_basics"),
    "sample_green": ("the green specimen", "picking up the green fluorescence specimen for microscope observation", "microscope_basics"),
    "sample_blue": ("the blue specimen", "picking up the blue fluorescence specimen for microscope observation", "microscope_basics"),
    "sample_yellow": ("the yellow specimen", "picking up the yellow fluorescence specimen for microscope observation", "microscope_basics"),
}


def require(value):
    if not value:
        raise ValueError("Invalid navigation context")


def vector(value):
    require(isinstance(value, dict) and set(value) == {"x", "y", "z"})
    require(all(type(v) in (float, int) and math.isfinite(v) and abs(v) <= 100000 for v in value.values()))
    return [value[k] for k in ("x", "y", "z")]


def context(snapshot):
    if snapshot is None:
        return {"targets": [], "allowed_actions": []}
    require(isinstance(snapshot, dict) and set(snapshot) == {
        "snapshotId", "playerPosition", "playerForward", "worldUnitsPerMeter", "targets"})
    require(isinstance(snapshot["snapshotId"], str) and re.fullmatch("[a-f0-9]{32}", snapshot["snapshotId"]))
    player = vector(snapshot["playerPosition"])
    forward = vector(snapshot["playerForward"])
    units = snapshot["worldUnitsPerMeter"]
    require(type(units) in (int, float) and math.isfinite(units) and .01 <= units <= 1000)
    require(isinstance(snapshot["targets"], list) and len(snapshot["targets"]) <= len(TARGETS))
    targets = []
    seen = set()
    for item in snapshot["targets"]:
        require(isinstance(item, dict) and set(item) == {"id", "worldPosition"})
        target_id = item["id"]
        require(isinstance(target_id, str) and target_id in TARGETS and target_id not in seen)
        seen.add(target_id)
        point = vector(item["worldPosition"])
        dx, dz = point[0] - player[0], point[2] - player[2]
        require(math.hypot(forward[0], forward[2]) > .0001)
        angle = math.degrees(math.atan2(forward[2] * dx - forward[0] * dz, forward[0] * dx + forward[2] * dz))
        direction = "ahead" if abs(angle) <= 45 else "behind you" if abs(angle) >= 135 else "to your right" if angle > 0 else "to your left"
        name, knowledge, topic = TARGETS[target_id]
        targets.append({**item, "name": name, "learning": knowledge, "knowledge_topic": topic,
                        "direction": direction, "distanceMeters": round(math.hypot(dx, dz) / units, 1),
                        "action_id": "highlight:" + target_id})
    return {**snapshot, "targets": targets, "allowed_actions": [t["action_id"] for t in targets]}


def validate_guide(answer, snapshot):
    available = {t["id"] for t in context(snapshot)["targets"]}
    ids = answer["interaction_ids"]
    require(isinstance(ids, list) and len(ids) == 1 and isinstance(ids[0], str) and ids[0] in available)
    require(answer["suggested_action_ids"] == ["highlight:" + ids[0]])
    require(answer["knowledge_topics"] == [TARGETS[ids[0]][2]])


RULES = """
[Learning location guidance]
Respond in English. NAVIGATION_CONTEXT controls target availability; GUIDANCE_CONTEXT.allowed_actions controls operations.
For a location or specimen request choose a matching current target; prefer a current operation for hands-on study at its interface.
The targets are parts, na_experiment, spatial_frequency, snom_entry, and sample_red/green/blue/yellow.
Red is configured beside the ultrasonic cleaner, green beside the centrifuge, blue/yellow in front of the microscope.
Use live coordinates and availability, not fixed initial positions, for direction and distance.
Conceptual questions use explain without markers; ambiguous learning/specimen goals may need one clarification.
A guide selects one target: interaction_ids=[target.id], suggested_action_ids=[target.action_id], knowledge_topics=[target.knowledge_topic].
Use 'You can use A to learn about B. It is approximately C metres [direction].'
Coordinates use Unity world Y up; playerForward is the viewing direction, not world north.
Prefer validated direction/distanceMeters. Distance is horizontal straight-line distance, not a walking route.
Do not invent identifiers, paths, controls, extra steps, or completion/arrival/marker-success claims.
Only the client can report successful marking after creating the effect, or explain nearby/unavailable targets.
Parts, NA, and spatial frequency share one microscope. NA links to Upper Optical Assembly; spatial frequency to Objective Lens.
These links do not establish that their buttons are currently clickable.
Pinhole and Z-stack have no hands-on target; NA/SNOM are not replacement confocal experiments.
When targets are empty or unmatched, explain supported knowledge and unavailable location guidance without inventing coordinates.
Do not ask the player to grant missing permissions. Mixed unrelated tasks still require fixed refusal.
Output only the original five-field JSON contract.
"""


def mock_guide(question, snapshot):
    q = question.lower()
    if not any(w in q for w in ("哪里", "在哪", "想学", "学习", "带我", "体验", "where", "learn", "find", "take", "get", "sample", "specimen", "slide", "获取", "拿", "找", "取", "样本", "标本", "切片", "载玻片")):
        return None
    # Deliberately narrow fixtures, not an AI or a production intent classifier.
    if any(w in q for w in ("共聚焦", "confocal", "针孔", "z-stack")):
        return None
    words = {
        "sample_red": ("红色样本", "红样本", "红色标本", "红色切片", "红色载玻片", "红载玻片", "红色", "red sample", "red"),
        "sample_green": ("绿色样本", "绿样本", "绿色标本", "绿色切片", "绿色载玻片", "绿载玻片", "绿色", "green sample", "green"),
        "sample_blue": ("蓝色样本", "蓝样本", "蓝色标本", "蓝色切片", "蓝色载玻片", "蓝载玻片", "蓝色", "blue sample", "blue"),
        "sample_yellow": ("黄色样本", "黄样本", "黄色标本", "黄色切片", "黄色载玻片", "黄载玻片", "黄色", "yellow sample", "yellow"),
        "na_experiment": ("数值孔径", "na"),
        "spatial_frequency": ("spatial frequency", "spectrum", "空间频率", "频谱"),
        "snom_entry": ("snom", "near field", "probe", "近场", "探针"),
        "parts": ("microscope", "component", "objective", "显微镜", "部件", "物镜"),
    }
    available = {t["id"] for t in context(snapshot)["targets"]}
    for target_id, keywords in words.items():
        if target_id in available and any(w in q for w in keywords):
            msg = "You can visit this specimen location to pick it up and observe it." if target_id.startswith("sample_") else "You can visit the matching learning area to explore this topic."
            return {"kind": "guide", "answer": msg,
                    "interaction_ids": [target_id], "suggested_action_ids": ["highlight:" + target_id],
                    "knowledge_topics": [TARGETS[target_id][2]]}
    return None
