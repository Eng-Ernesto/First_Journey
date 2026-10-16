"""Check the Git index before a Unity backup, without modifying project assets."""
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]


def git(*args, input=None):
    return subprocess.check_output(["git", *args], cwd=ROOT, input=input)


def main():
    entries = git("ls-files", "--stage", "-z").split(b"\0")
    files = {}
    for entry in filter(None, entries):
        descriptor, name = entry.split(b"\t", 1)
        mode, oid, stage = descriptor.decode().split()
        if stage != "0":
            raise RuntimeError("Unresolved index entry")
        files[name.decode("utf-8")] = (mode, oid)
    issues = []
    caches = {"library", "temp", "obj", "logs", "usersettings", "testresults", "build", "builds", ".vs"}
    names = set(files)
    for name, (mode, oid) in files.items():
        if name.split("/")[0].lower() in caches or name.startswith("Assets/DownloadCache"):
            issues.append("Generated/cache file staged: " + name)
        if mode == "160000":
            issues.append("Embedded repository staged: " + name)
        if name.startswith("Assets/") and not name.endswith(".meta"):
            if name + ".meta" not in names:
                issues.append("Asset missing staged meta: " + name)
            for parent in pathlib.PurePosixPath(name).parents:
                if str(parent) in {".", "Assets"}:
                    break
                if str(parent) + ".meta" not in names:
                    issues.append("Folder missing staged meta: " + str(parent))
        if name.startswith("Assets/") and name.endswith(".meta"):
            asset = name[:-5]
            if asset not in names and not any(n.startswith(asset + "/") for n in names):
                is_folder = b"folderAsset: yes" in git("cat-file", "blob", oid)
                if not is_folder:
                    issues.append("Orphan staged meta: " + name)
    oids = sorted({oid for mode, oid in files.values() if mode != "160000"})
    output = git("cat-file", "--batch-check=%(objectname) %(objectsize)", input=("\n".join(oids) + "\n").encode())
    sizes = {oid: int(size) for oid, size in (line.split() for line in output.decode().splitlines())}
    for name, (mode, oid) in files.items():
        if mode != "160000" and sizes[oid] > 50 * 1024 * 1024:
            issues.append("Large raw blob instead of LFS pointer: " + name)
    attribute_output = git("check-attr", "--cached", "-z", "--stdin", "filter",
                           input=b"\0".join(n.encode("utf-8") for n in files) + b"\0").split(b"\0")
    lfs = [attribute_output[i].decode("utf-8") for i in range(0, len(attribute_output)-2, 3)
           if attribute_output[i+2] == b"lfs"]
    for name in lfs:
        if sizes[files[name][1]] > 1024:
            issues.append("LFS-managed file staged without pointer: " + name)
    for name, (mode, oid) in files.items():
        if name.startswith("Assets/") and sizes.get(oid, 0) > 1024**2 and name not in lfs:
            with (ROOT / name).open("rb") as stream:
                binary = b"\0" in stream.read(8192)
            if binary:
                issues.append("Large binary asset outside LFS: " + name)
    summary = {"passed": not issues, "stagedFiles": len(files), "lfsFiles": len(lfs),
               "largestGitBlobMB": round(max(sizes.values(), default=0) / 1024**2, 2),
               "issues": sorted(set(issues))}
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if not issues else 1


if __name__ == "__main__":
    sys.exit(main())
