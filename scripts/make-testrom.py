"""Generate an original minimal DS protocol fixture, with no Nintendo code or logo.

The A2DE identifier only exercises this frontend's import gate. This is NOT NSMB.
ARM9 displays a red VRAM framebuffer; ARM7 idles. Source: GPL-3.0-or-later.
"""
import pathlib, struct, sys


def build():
    rom=bytearray(0x20000)
    rom[:12]=b'PC TEST ROM\0'
    rom[12:16]=b'A2DE'
    rom[16:18]=b'00'
    words=[];literals=[]
    def load(register,value):
        literals.append((len(words),register,value));words.append(0)
    load(0,0x04000304);load(1,0x8003);words.append(0xe1c010b0) # POWCNT1
    load(0,0x04000240);words.extend([0xe3a01080,0xe5c01000]) # VRAM A LCDC
    load(0,0x04000000);load(1,0x00020000);words.append(0xe5801000) # main display VRAM
    load(0,0x06800000);load(1,0x801f);load(2,256*192)
    loop=len(words)
    words.extend([0xe0c010b2,0xe2522001])
    branch_index=len(words);words.append(0x1a000000|((loop-branch_index-2)&0xffffff))
    words.append(0xeafffffe)
    for i,reg,value in literals:
        target=len(words);words[i]=0xe59f0000|(reg<<12)|((target-i-2)*4);words.append(value)
    arm9=struct.pack('<'+'I'*len(words),*words)
    rom[0x200:0x200+len(arm9)]=arm9
    rom[0x1200:0x1204]=struct.pack('<I',0xeafffffe)
    struct.pack_into('<4I',rom,0x20,0x200,0x02000000,0x02000000,len(arm9))
    struct.pack_into('<4I',rom,0x30,0x1200,0x03800000,0x03800000,4)
    struct.pack_into('<I',rom,0x80,len(rom));struct.pack_into('<I',rom,0x84,0x200)
    crc=0xffff
    for byte in rom[:0x15e]:
        crc^=byte
        for _ in range(8):crc=(crc>>1)^0xa001 if crc&1 else crc>>1
    struct.pack_into('<H',rom,0x15e,crc)
    return rom


if __name__=='__main__':
    path=pathlib.Path(sys.argv[1]);path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(build())
