"""Independent Blender readback of the authored handheld-tool exports.

Run in a separate, fully isolated background Blender process, never in the UI:
  blender --background --factory-startup --disable-autoexec --python this.py --
      --project-root <VoidForge> --report <report.json>
This script only reads FBX/GLB exports and writes its JSON report.
"""

import argparse
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector


def inspect_export(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix.lower() == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        bpy.ops.import_scene.gltf(filepath=str(path))
    scene = bpy.context.scene
    meshes = [obj for obj in scene.objects if obj.type == "MESH"]
    vertices = []
    triangles = 0
    zero_area = 0
    materials = set()
    pieces = []
    for obj in meshes:
        obj.data.calc_loop_triangles()
        vertices.extend(obj.matrix_world @ vert.co for vert in obj.data.vertices)
        count = len(obj.data.loop_triangles)
        triangles += count
        zero_area += sum(tri.area < 1e-14 for tri in obj.data.loop_triangles)
        materials.update(slot.material.name for slot in obj.material_slots if slot.material)
        pieces.append({"name": obj.name, "triangles": count})
    finite = bool(vertices) and all(math.isfinite(c) for v in vertices for c in v)
    if finite:
        low = Vector(tuple(min(v[i] for v in vertices) for i in range(3)))
        high = Vector(tuple(max(v[i] for v in vertices) for i in range(3)))
        bounds = list(high - low)
    else:
        bounds = [0.0, 0.0, 0.0]
    anchors = [obj.name for obj in scene.objects if obj.type == "EMPTY"]
    problems = []
    if not finite or triangles == 0:
        problems.append("No finite mesh geometry")
    if triangles > 8000:
        problems.append("More than the 8000-triangle authoring cap")
    if not 0.15 <= max(bounds) <= 0.65:
        problems.append("Longest dimension outside handheld range 0.15-0.65 metres")
    if not materials:
        problems.append("No imported materials")
    if not any("GripPrimary" in name for name in anchors):
        problems.append("GripPrimary anchor missing")
    if not any("ToolTip" in name for name in anchors):
        problems.append("ToolTip anchor missing")
    return {
        "path": str(path), "bytes": path.stat().st_size,
        "triangles": triangles, "mesh_count": len(meshes),
        "bounds_m": bounds, "materials": sorted(materials),
        "anchors": anchors, "pieces": pieces,
        "zero_area_triangles": zero_area, "problems": problems,
        "success": not problems,
    }


def main():
    if not bpy.app.background:
        raise RuntimeError("Use a separate background Blender process")
    argv = sys.argv[sys.argv.index("--") + 1:]
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--report", required=True)
    args = parser.parse_args(argv)
    project = Path(args.project_root).resolve()
    report_path = Path(args.report).resolve()
    if not report_path.is_relative_to(project):
        raise RuntimeError("Report must remain inside VoidForge")
    # The fully isolated profile may live in the main project's Library while
    # exports are being validated in its nested sparse worktree.
    scope = project
    for parent in (project, *project.parents):
        if parent.name.lower() == "voidforge" and ".worktrees" not in parent.parts:
            scope = parent
            break
    for resource in ("CONFIG", "SCRIPTS", "EXTENSIONS"):
        actual = Path(bpy.utils.user_resource(resource)).resolve()
        if not actual.is_relative_to(scope):
            raise RuntimeError("Blender resource profile outside VoidForge: " + str(actual))
    root = project / "Assets/VoidForge/Art/HandTools"
    results = []
    errors = []
    for tool in ("MiningDrill", "Grinder", "Welder"):
        pairs = {}
        for extension in ("fbx", "glb"):
            paths = sorted((root / tool).glob("*." + extension))
            if len(paths) != 1:
                errors.append(f"{tool}: expected one {extension} export, found {len(paths)}")
                continue
            try:
                result = inspect_export(paths[0])
                result["path"] = paths[0].relative_to(project).as_posix()
                results.append(result)
                pairs[extension] = result
                errors.extend(tool + ": " + item for item in result["problems"])
            except Exception as exc:
                errors.append(f"{paths[0]}: {type(exc).__name__}: {exc}")
        if len(pairs) == 2:
            a, b = pairs["fbx"], pairs["glb"]
            if a["triangles"] != b["triangles"]:
                errors.append(tool + ": FBX and GLB triangle counts differ")
            if any(abs(x - y) > 0.002 for x, y in zip(a["bounds_m"], b["bounds_m"])):
                errors.append(tool + ": FBX and GLB bounds differ by more than 2 mm")
    report = {
        "success": not errors, "blender_version": bpy.app.version_string,
        "method": "Independent FBX/GLB import in an isolated background Blender",
        "exports": results, "errors": errors,
        "limitations": ["No Unity import or Quest performance test in this check"],
    }
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"success": report["success"], "exports": len(results), "errors": errors}))
    if errors:
        raise RuntimeError("Hand-tool readback failed; see " + str(report_path))


if __name__ == "__main__":
    main()
