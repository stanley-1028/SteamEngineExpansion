"""Probe winding convention of a real .timbermesh file.

For every triangle compare the right-handed geometric normal cross(p1-p0, p2-p0)
against the average of the stored per-vertex normals. A consistent positive dot
means the stored index order is counter-clockwise in a right-handed reading of
the stored coordinates (and outward normals agree).
"""

from __future__ import annotations

import os
import struct
import sys
import zlib

PLUGIN = r"C:\Users\user\Documents\Timberborn\Mods\HighDensityBoxFarm\art\tools\timbermesh_blender_plugin"
sys.path.insert(0, PLUGIN)
import model_pb2  # noqa: E402


def floats(prop):
    dim = max(prop.scalarTypeDimension, 1)
    count = len(prop.data) // (4 * dim)
    return [struct.unpack_from("<%df" % dim, prop.data, i * 4 * dim) for i in range(count)]


def main(path):
    model = model_pb2.Model()
    model.ParseFromString(zlib.decompress(open(path, "rb").read()))
    pos = neg = zero = 0
    samples = []
    for node in model.nodes:
        props = {p.name: p for p in node.vertexProperties}
        if not node.vertexCount or "position" not in props or "normal" not in props:
            continue
        positions = floats(props["position"])
        normals = floats(props["normal"])
        for mesh in node.meshes:
            idx = list(mesh.indices)
            for i in range(0, len(idx) - 2, 3):
                p0, p1, p2 = positions[idx[i]], positions[idx[i + 1]], positions[idx[i + 2]]
                n = [sum(normals[idx[i + k]][c] for k in range(3)) / 3.0 for c in range(3)]
                e1 = [p1[c] - p0[c] for c in range(3)]
                e2 = [p2[c] - p0[c] for c in range(3)]
                g = [e1[1] * e2[2] - e1[2] * e2[1],
                     e1[2] * e2[0] - e1[0] * e2[2],
                     e1[0] * e2[1] - e1[1] * e2[0]]
                dot = sum(g[c] * n[c] for c in range(3))
                if dot > 1e-8:
                    pos += 1
                elif dot < -1e-8:
                    neg += 1
                else:
                    zero += 1
                if len(samples) < 3:
                    samples.append((node.name, mesh.material, dot, n, g))
    print(os.path.basename(path), "pos=%d neg=%d zero=%d" % (pos, neg, zero))
    for s in samples:
        print("  sample", s)


if __name__ == "__main__":
    for p in sys.argv[1:]:
        main(p)
