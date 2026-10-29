# Componentes e fontes correspondentes

- **Frontend MarioPC**: Copyright 2026 GenissonEmilio e colaboradores. GPL-3.0-or-later, sem garantia. Texto completo em LICENSE.
- **melonDS/libretro**, revisão `66b5d2634cd0a79030562811e6e05f5532f800ba`: GPL-3.0-or-later. Autoria e cabeçalhos originais preservados em `core/melonds`. https://github.com/libretro/melonDS
- **FreeBIOS**: substitutos livres de BIOS incorporados pelo melonDS; seus fontes e licenças estão em `core/melonds/freebios` e nos cabeçalhos gerados do núcleo. Não são dumps de BIOS da Nintendo.
- **libretro-common**, Teakra, tiny-AES-c, FatFs, xxHash, SHA-1 e emissores de código usados pelo núcleo: componentes incorporados, com seus cabeçalhos de autoria e licenças preservados na árvore completa. Consulte cada diretório em `core/melonds/src`.
- **.NET / Windows Forms**, Microsoft e colaboradores: MIT e avisos de terceiros incluídos pelo runtime self-contained, além de `licenses/dotnet-LICENSE.txt`. https://github.com/dotnet/runtime e https://github.com/dotnet/winforms
- **MinGW-w64 / libgcc / libstdc++**: compilador usado para produzir o núcleo, com exceções de runtime aplicáveis. Fontes, avisos e versões da toolchain estão no [w64devkit 2.11.0](https://github.com/skeeto/w64devkit/releases/tag/v2.11.0). O núcleo é vinculado estaticamente às bibliotecas de runtime e dinamicamente somente a bibliotecas do Windows. Os avisos de runtime da toolchain acompanham o pacote em `licenses/`.

A release oferece `*-source.zip` com os fontes completos do frontend e do núcleo **na mesma versão dos binários** e os scripts para reconstruí-los. Nenhum conteúdo comercial do jogo é incluído. As fontes padrão que o GitHub gera automaticamente não incluem o conteúdo dos submódulos; para redistribuir, use o ZIP de fontes que fornecemos ou um clone recursivo.
