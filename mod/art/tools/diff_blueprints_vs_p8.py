"""SEE P9 - prove the blueprint diff against the P8 baseline is art-only.

The P8 install backup (`mod/build/_install-backup-20261106/SteamEngineExpansion
.bak-20261106-214744/`) is a snapshot of the shipped v1 (vanilla-placeholder)
blueprints. P9 is only allowed to change:

  * `Children.#Finished.TimbermeshSpec.Model`
  * `Children.#Unfinished.Children.ConstructionStage0.TimbermeshSpec.Model`
  * `TransputProviderSpec`            (5 ports: left/right/back + 2 bottom)
  * `TemplateAttachmentsSpec`         (smoke anchors `#EmptySmoke1..3`)
  * `PowerGeneratorParticleControllerSpec` (smoke attachment ids)
  * `GoodPoweredGeneratorAnimatorSpec` (flywheel/crankshaft binding)
  * `LabeledEntitySpec.Icon`          (unchanged path, kept for safety)
  * `CollidersSpec`                   (kept unchanged by the generator)

Every other leaf (ratings, fuel/water rates, costs, sizes, loc keys, ...) must
be byte-for-byte value-identical. Writes `mod/art/blueprint-diff.txt`; exits 1
if a value outside the allow-list changed.

Run:
  python mod/art/tools/diff_blueprints_vs_p8.py
"""

from __future__ import annotations

import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)
MOD_ROOT = os.path.dirname(ART)

BASELINE = os.path.join(
    MOD_ROOT, "build", "_install-backup-20261106",
    "SteamEngineExpansion.bak-20261106-214744", "Buildings", "Power", "SteamBoiler")
CURRENT = os.path.join(MOD_ROOT, "data", "Buildings", "Power", "SteamBoiler")
OUTPUT = os.path.join(ART, "blueprint-diff.txt")

ALLOWED_PREFIXES = [
    "Children.#Finished.TimbermeshSpec.Model",
    "Children.#Unfinished.Children.ConstructionStage0.TimbermeshSpec.Model",
    "TransputProviderSpec",
    "TemplateAttachmentsSpec",
    "PowerGeneratorParticleControllerSpec",
    "GoodPoweredGeneratorAnimatorSpec",
    "LabeledEntitySpec.Icon",
    "Children.#Finished.CollidersSpec",
    "Children.#Unfinished.Children.ConstructionStage0.CollidersSpec",
]


def walk(prefix, before, after, changes):
    if type(before) is not type(after):
        changes.append((prefix, before, after))
        return
    if isinstance(before, dict):
        for key in sorted(set(before) | set(after)):
            path = key if not prefix else prefix + "." + key
            if key not in before:
                changes.append((path, "<missing>", after[key]))
            elif key not in after:
                changes.append((path, before[key], "<missing>"))
            else:
                walk(path, before[key], after[key], changes)
    elif isinstance(before, list):
        if len(before) != len(after):
            changes.append((prefix + "[]", "len=%d" % len(before), "len=%d" % len(after)))
        for index in range(min(len(before), len(after))):
            walk(prefix + "[%d]" % index, before[index], after[index], changes)
    elif before != after:
        changes.append((prefix, before, after))


def is_allowed(path):
    return any(path == prefix or path.startswith(prefix + ".")
               or path.startswith(prefix + "[") for prefix in ALLOWED_PREFIXES)


def main():
    lines = ["SEE P9 blueprint diff vs P8 baseline",
             "baseline: " + os.path.relpath(BASELINE, MOD_ROOT).replace("\\", "/"),
             "current : " + os.path.relpath(CURRENT, MOD_ROOT).replace("\\", "/"),
             ""]
    if not os.path.isdir(BASELINE):
        lines.append("RESULT: SKIP (P8 baseline backup not found)")
        _write(lines)
        print("baseline missing: " + BASELINE)
        return 0

    total = 0
    files = 0
    disallowed = []
    for name in sorted(os.listdir(CURRENT)):
        if not name.endswith(".blueprint.json"):
            continue
        backup_path = os.path.join(BASELINE, name)
        if not os.path.isfile(backup_path):
            lines.append("%s: MISSING BASELINE" % name)
            disallowed.append((name, "<file>", "<missing>", "<missing>"))
            continue
        files += 1
        before = json.load(open(backup_path, encoding="utf-8"))
        after = json.load(open(os.path.join(CURRENT, name), encoding="utf-8"))
        changes = []
        walk("", before, after, changes)
        total += len(changes)
        lines.append("%s: %d leaf change(s)" % (name, len(changes)))
        for path, old, new in changes:
            status = "OK " if is_allowed(path) else "BAD"
            if status == "BAD":
                disallowed.append((name, path, old, new))
            lines.append("  [%s] %s: %s => %s" % (status, path, old, new))
        lines.append("")

    lines.extend([
        "SUMMARY",
        "  files compared          : %d" % files,
        "  changed leaf values     : %d" % total,
        "  art-only allow-list     : %s" % ", ".join(ALLOWED_PREFIXES),
        "  disallowed changes      : %d" % len(disallowed),
        "",
        "RESULT: %s" % ("PASS" if not disallowed else "FAIL"),
    ])
    _write(lines)
    print("files=%d changes=%d disallowed=%d -> %s"
          % (files, total, len(disallowed), "PASS" if not disallowed else "FAIL"))
    print("report: " + OUTPUT)
    return 0 if not disallowed else 1


def _write(lines):
    with open(OUTPUT, "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines) + "\n")


if __name__ == "__main__":
    raise SystemExit(main())
