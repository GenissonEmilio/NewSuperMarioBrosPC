using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MarioPC;

public static class RomImport
{
    public const int MaxSize=64*1024*1024;
    public static byte[] Read(string path)
    {
        byte[] bytes;
        if(Path.GetExtension(path).Equals(".zip",StringComparison.OrdinalIgnoreCase))
        {
            using var zip=ZipFile.OpenRead(path);
            var entries=zip.Entries.Where(e=>e.FullName.EndsWith(".nds",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(entries.Length!=1)throw new InvalidDataException("Escolha um ZIP com exatamente uma ROM .nds.");
            if(entries[0].Length>MaxSize || entries[0].Length<512)throw new InvalidDataException("Tamanho de ROM inválido.");
            using var stream=entries[0].Open();
            bytes=new byte[(int)entries[0].Length];stream.ReadExactly(bytes);
        }
        else
        {
            var info=new FileInfo(path);
            if(!Path.GetExtension(path).Equals(".nds",StringComparison.OrdinalIgnoreCase)|| info.Length>MaxSize || info.Length<512)
                throw new InvalidDataException("Use uma ROM .nds ou um ZIP com uma ROM.");
            bytes=File.ReadAllBytes(path);
        }
        Validate(bytes);
        return bytes;
    }
    public static void Validate(byte[] b)
    {
        if(b.Length<512 || b.Length>MaxSize || Encoding.ASCII.GetString(b,12,4)!="A2DE" || b[30]!=0)
            throw new InvalidDataException("Esta alpha aceita New Super Mario Bros. USA/Australia (A2DE), revisão 0.");
        foreach(int o in new[]{0x20,0x30})
        {
            uint start=BitConverter.ToUInt32(b,o), size=BitConverter.ToUInt32(b,o+12);
            if(start<512 || size==0 || (ulong)start+size>(ulong)b.Length)throw new InvalidDataException("Cabeçalho da ROM inválido ou arquivo truncado.");
        }
    }
    public static string Import(string path,string root)
    {
        var bytes=Read(path);Directory.CreateDirectory(root);
        string target=Path.Combine(root,"game.nds"),temp=target+".tmp";
        if(File.Exists(target) && !SHA256.HashData(File.ReadAllBytes(target)).SequenceEqual(SHA256.HashData(bytes)))
            throw new InvalidDataException("Já existe outra ROM importada. Faça uma cópia dos seus saves antes de trocar a ROM na pasta de dados.");
        File.WriteAllBytes(temp,bytes);File.Move(temp,target,true);return target;
    }
}
