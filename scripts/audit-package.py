import argparse, hashlib, json, pathlib, zipfile


def audit(path):
    with zipfile.ZipFile(path) as z:
        files={p.filename:p for p in z.infolist() if not p.is_dir()}
        assert len(files)==sum(not p.is_dir() for p in z.infolist()), 'Duplicate paths'
        for name in files:
            p=pathlib.PurePosixPath(name)
            assert '\\' not in name and ':' not in name and not p.is_absolute() and '..' not in p.parts
            assert p.parts[0]=='MarioPC' and len(p.parts)>1
            assert not set(part.lower() for part in p.parts)&{'local','test-data','saves','.git','artifacts'}
            assert p.suffix.lower() not in {'.nds','.sav','.state','.bin','.png','.jpg','.log','.pdb','.zip'}
            assert p.name.lower() not in {'profile.json','firmware.bin','bios7.bin','bios9.bin'}
        required={'MarioPC.exe','MarioPC.dll','melonds_libretro.dll','coreclr.dll','hostfxr.dll','hostpolicy.dll',
                  'MarioPC.runtimeconfig.json','MarioPC.deps.json','System.Windows.Forms.dll','README.md',
                  'LICENSE','THIRD-PARTY.md','LEIA-ME.txt','RELEASE_NOTES.md','package-manifest.json',
                  'licenses/melonDS-GPL-3.0.txt','licenses/dotnet-LICENSE.txt'}
        assert all('MarioPC/'+p in files for p in required), 'Missing required file'
        for exe in ('MarioPC.exe','MarioPC.dll','melonds_libretro.dll'):
            assert z.read('MarioPC/'+exe).startswith(b'MZ')
        manifest=json.loads(z.read('MarioPC/package-manifest.json').decode('utf-8-sig'))
        assert manifest['GameDataIncluded'] is False
        expected={'MarioPC/'+f['Path'] for f in manifest['Files']}
        assert expected==set(files)-{'MarioPC/package-manifest.json'}, 'Manifest mismatch'
        for f in manifest['Files']:
            data=z.read('MarioPC/'+f['Path'])
            assert len(data)==f['Bytes'] and hashlib.sha256(data).hexdigest()==f['Sha256'], f['Path']
        return dict(valid=True,files=len(files),bytes=sum(p.file_size for p in files.values()),
                    sourceCommit=manifest['SourceCommit'],coreCommit=manifest['CoreCommit'],gameDataIncluded=False)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('zip');p.add_argument('--output');a=p.parse_args()
    result=audit(a.zip)
    if a.output:pathlib.Path(a.output).write_text(json.dumps(result,indent=2))
    print(json.dumps(result))
