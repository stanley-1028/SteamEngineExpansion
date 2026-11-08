"""Read-only inspector for .timbermesh files (pure Python, no Blender).

Usage:
  python inspect_timbermesh.py <file.timbermesh> [...]

Prints node tree, vertex counts, triangle counts, materials, empties and
animation metadata so the art pipeline can be designed against the real format.
"""

from __future__ import annotations

import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
PLUGIN = r"C:\Users\user\Documents\Timberborn\Mods\HighDensityBoxFarm\art\tools\timbermesh_blender_plugin"
if PLUGIN not in sys.path:
    sys.path.insert(0, PLUGIN)

import model_pb2  # noqa: E402


def read_model(path):
    model = model_pb2.Model()
    model.ParseFromString(zlib.decompress(open(path, "rb").read()))
    return model


def floats(prop):
    dim = max(prop.scalarTypeDimension, 1)
    count = len(prop.data) // (4 * dim)
    return [struct.unpack_from("<%df" % dim, prop.data, i * 4 * dim) for i in range(count)]


def describe(path):
    model = read_model(path)
    print("=" * 90)
    print(os.path.basename(path))
    print("  Model.name=%r version=%s nodes=%d bytes=%d"
          % (model.name, model.version, len(model.nodes), os.path.getsize(path)))
    total_tris = 0
    total_verts = 0
    for index, node in enumerate(model.nodes):
        meshes = []
        tris = 0
        for mesh in node.meshes:
            tris += len(mesh.indices) // 3
            meshes.append(mesh.material)
        total_tris += tris
        total_verts += node.vertexCount
        parent = node.parent
        props = [p.name for p in node.vertexProperties]
        anim = [(a.name, a.framerate, len(a.frames)) for a in node.nodeAnimations]
        vanim = [(a.name, a.framerate, len(a.frames)) for a in node.vertexAnimations]
        print("  [%02d] parent=%d name=%r pos=(%.3f,%.3f,%.3f) rot=(%.3f,%.3f,%.3f,%.3f) "
              "verts=%d tris=%d materials=%s props=%s nanim=%s vanim=%s"
              % (index, parent, node.name,
                 node.position.x, node.position.y, node.position.z,
                 node.rotation.x, node.rotation.y, node.rotation.z, node.rotation.w,
                 node.vertexCount, tris, meshes, props, anim, vanim))
    print("  TOTAL verts=%d tris=%d" % (total_verts, total_tris))


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    for path in argv[1:]:
        describe(path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
