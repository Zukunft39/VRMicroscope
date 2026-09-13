"""Strict, location-only context. Coordinates are supplied by Unity, not by chat text."""
import math
import re

TARGETS = {
    "parts": ("显微镜学习区域", "显微镜结构和部件功能", "microscope_basics"),
    "na_experiment": ("显微镜学习区域", "数值孔径，入口关联上部光学组件", "numerical_aperture"),
    "spatial_frequency": ("显微镜学习区域", "空间频率，入口关联物镜", "spatial_frequency"),
    "snom_entry": ("THz s-SNOM 装置", "探针敲击、近场耦合与扫描的教学演示", "snom"),
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
        direction = "前方" if abs(angle) <= 45 else "后方" if abs(angle) >= 135 else "右侧" if angle > 0 else "左侧"
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
【学习区域定位规则】
NAVIGATION_CONTEXT 仅决定可定位目标；操作说明另由 GUIDANCE_CONTEXT.allowed_actions 决定。
用户寻找某区域时选择匹配的 targets；想学习且当前有匹配操作时优先操作说明，否则定位。
只解释原理/区别时仍 explain，不无故添加标记。不明确想学哪个主题时 clarify。
定位类型 guide 仅选择一个当前 target：interaction_ids=[target.id]，suggested_action_ids=[target.action_id]，
knowledge_topics=[target.knowledge_topic]。不得创造 ID、按钮、设备路径、控制命令或额外步骤。
answer 用“你可以通过 A 学习 B。它位于你某方向约 C 米处”描述拟定位对象。
坐标为 Unity 世界坐标，Y 向上；playerForward 是玩家视线方向，不是世界固定北向。
请依据坐标理解相对关系，direction/distanceMeters 是程序校验值，应优先采用；距离为水平直线距离，非可行走路线。
不能说标记已成功、到达、学会或完成实验。返回目标只是申请标记。
客户端真正创建标记后，才会把 guide 替换为固定的“通过 A 学习 B；方位距离；已经添加效果”句式。
标记失败或已在附近时客户端另行说明，不得在其他回答类型承诺已标记。
NA、空间频率、部件认识是同一显微镜区域的不同入口；不可描述为三个房间。
NA 入口关联 Upper Optical Assembly；空间频率入口关联 Objective Lens。可以说明关系，不能编造此刻可点击按钮。
没有共聚焦针孔、Z-stack 的可操作学习目标；不得用 NA 或 SNOM 冒充共聚焦实验。
targets 为空或没有匹配目标时，解释已知知识并说明暂不能定位；不照搬历史坐标，不要求玩家批准。
混入独立无关任务仍整条固定拒答。只输出原五字段 JSON。
"""


def mock_guide(question, snapshot):
    q = question.lower()
    if not any(w in q for w in ("哪里", "在哪", "想学", "学习", "带我", "体验", "where", "learn")):
        return None
    # Deliberately narrow fixtures, not an AI or a production intent classifier.
    if any(w in q for w in ("共聚焦", "confocal", "针孔", "z-stack")):
        return None
    words = {"na_experiment": ("数值孔径", "na"), "spatial_frequency": ("空间频率", "频谱"),
             "snom_entry": ("snom", "近场", "探针"), "parts": ("显微镜", "部件", "物镜")}
    available = {t["id"] for t in context(snapshot)["targets"]}
    for target_id, keywords in words.items():
        if target_id in available and any(w in q for w in keywords):
            return {"kind": "guide", "answer": "可以前往对应学习区域了解这一主题。",
                    "interaction_ids": [target_id], "suggested_action_ids": ["highlight:" + target_id],
                    "knowledge_topics": [TARGETS[target_id][2]]}
    return None
