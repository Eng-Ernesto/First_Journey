"""Prepare an assets-only archive; never change Unity assets, GUIDs or project settings.

The original package remains untouched. Import the result with the Unity plugin.
"""
import gzip
import json
import pathlib
import tarfile

PROJECT = pathlib.Path(__file__).resolve().parent.parent
SOURCE = pathlib.Path.home() / "AppData/Roaming/Unity/Asset Store-5.x/Unity Technologies/Complete ProjectsPacks/Fantasy Kingdom 3D Sample Project.unitypackage"
OUTPUT = PROJECT / "Temp/FantasyKingdom_Import.unitypackage"
EXCLUDED_ASSETS = {
    "Assets/Scripts/VerifyProject.cs",
    "Assets/Data/VerifyProjectSettings.asset",
    "Assets/Scripts/TutorialInfo/Editor/FKReadmeEditor.cs",
}


def main():
    skipped, excluded, kept = set(), [], []
    with tarfile.open(SOURCE, "r|gz") as archive:
        for member in archive:
            if member.name.endswith("/pathname"):
                path = archive.extractfile(member).read().decode("utf-8").splitlines()[0]
                if (path != "Assets" and not path.startswith("Assets/")) or path in EXCLUDED_ASSETS:
                    skipped.add(member.name.split("/")[0])
                    excluded.append(path)
                else:
                    kept.append(path)
    OUTPUT.parent.mkdir(exist_ok=True)
    with gzip.open(SOURCE, "rb") as source, OUTPUT.open("wb") as target:
        # Unity uses the embedded GZIP filename to recognize the TAR container.
        with gzip.GzipFile(filename="archtemp.tar", mode="wb", compresslevel=1, fileobj=target) as result:
            while header := source.read(512):
                if header == bytes(512):
                    break
                if len(header) != 512:
                    raise ValueError("Truncated TAR header")
                name = header[:100].split(b"\0")[0].decode("utf-8")
                size = int(header[124:136].strip(b"\0 "), 8)
                remaining = ((size + 511) // 512) * 512
                keep = name.split("/")[0] not in skipped
                if keep:
                    result.write(header)
                while remaining:
                    data = source.read(min(remaining, 1024 * 1024))
                    if not data:
                        raise ValueError("Truncated TAR payload")
                    remaining -= len(data)
                    if keep:
                        result.write(data)
            result.write(bytes(1024))
    report = {"source": str(SOURCE), "output": str(OUTPUT), "assetPaths": len(kept),
              "excluded": excluded, "filteredBytes": OUTPUT.stat().st_size,
              "gzipEmbeddedFilename": "archtemp.tar", "serializedAssetsModified": False}
    (PROJECT / "Docs/FantasyKingdom_Importacao.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
