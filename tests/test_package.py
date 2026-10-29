import importlib.util, json, pathlib, tempfile, unittest, zipfile, hashlib

spec=importlib.util.spec_from_file_location('audit',pathlib.Path(__file__).parents[1]/'scripts/audit-package.py')
audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)

class PackageTests(unittest.TestCase):
    def make(self, extra=None, corrupt=False):
        names=['MarioPC.exe','MarioPC.dll','melonds_libretro.dll','coreclr.dll','hostfxr.dll','hostpolicy.dll',
               'MarioPC.runtimeconfig.json','MarioPC.deps.json','System.Windows.Forms.dll','README.md','LICENSE',
               'THIRD-PARTY.md','LEIA-ME.txt','RELEASE_NOTES.md','licenses/melonDS-GPL-3.0.txt','licenses/dotnet-LICENSE.txt']
        files={n:b'MZfixture' for n in names}
        if extra:files[extra]=b'private'
        manifest={'GameDataIncluded':False,'SourceCommit':'fixture','CoreCommit':'fixture',
                  'Files':[{'Path':n,'Bytes':len(b),'Sha256':hashlib.sha256(b).hexdigest()} for n,b in files.items()]}
        if corrupt:files['MarioPC.exe']+=b'tampered'
        tmp=tempfile.NamedTemporaryFile(suffix='.zip',delete=False);tmp.close();p=pathlib.Path(tmp.name)
        with zipfile.ZipFile(p,'w') as z:
            for n,b in files.items():z.writestr('MarioPC/'+n,b)
            z.writestr('MarioPC/package-manifest.json',json.dumps(manifest))
        self.addCleanup(p.unlink);return p
    def test_valid(self):self.assertTrue(audit.audit(self.make())['valid'])
    def test_private_data(self):
        for n in ['game.nds','game.sav','resume.state','local/secret.txt','test-data/ram.txt','capture.png','profile.json','debug.pdb']:
            with self.subTest(n=n),self.assertRaises(AssertionError):audit.audit(self.make(n))
    def test_path_escape(self):
        for n in ['../outside.txt','C:/secret.txt','a\\b.txt']:
            with self.subTest(n=n),self.assertRaises(AssertionError):audit.audit(self.make(n))
    def test_tampered(self):
        with self.assertRaises(AssertionError):audit.audit(self.make(corrupt=True))

if __name__=='__main__':unittest.main()
