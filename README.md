# New Super Mario Bros. PC

Adaptação independente para **Windows x64**, com **WASD**, **Espaço para pular**, remapeamento de teclas e uma área principal de jogo. Usa o núcleo **melonDS/libretro** para executar a sua ROM de Nintendo DS. **É uma experiência de PC baseada em emulação, não um port nativo nem uma decompilação completa.**

## Jogar

1. Baixe o ZIP de Windows em [Releases](https://github.com/GenissonEmilio/NewSuperMarioBrosPC/releases).
2. Extraia **toda a pasta** e abra `MarioPC.exe`. O runtime .NET acompanha o pacote.
3. Importe sua ROM `.nds` ou um ZIP com uma única ROM. Suporta **A2DE, USA/Australia, revisão 0**.
4. Escolha **Jogar**. Depois, **Continuar aventura** recupera o último snapshot salvo ao sair.

Não distribuímos ROM, firmware/BIOS da Nintendo, saves, texturas ou músicas. A ROM original não é modificada. Os arquivos pessoais ficam em `%LOCALAPPDATA%\GenissonEmilio\NewSuperMarioBrosPC`; o botão **Abrir pasta de dados** abre essa pasta. Faça uma cópia dela para preservar seus saves.

## Controles

| Ação | Padrão |
| --- | --- |
| Mover / portas / canos | WASD |
| Pular / nadar | Espaço |
| Correr / disparar / segurar objetos | Shift |
| Usar item reserva | E ou botão no HUD |
| Confirmar / voltar no jogo | Enter / Backspace |
| Pause original (Start) | P |
| Abrir / fechar mapa e progresso | M ou botão Mapa |
| Menu PC / pausa | Esc |
| Tela cheia | F11 |

Abra **Controles** no início ou na pausa, clique na ação e pressione a tecla desejada. A configuração é salva imediatamente. Se a tecla estiver ocupada, as duas atribuições são trocadas. Esc cancela a captura. Teclas reservadas pelo Windows não são aceitas. O mouse clica nos menus e no conteúdo tátil quando a tela inferior está na área principal.

## Uma tela

A campanha ocupa uma única área de jogo em 4:3, sem esticar a imagem. **Não existe comando de alternar telas nem modo de duas telas.** Os menus originais usam essa mesma área durante a seleção de arquivo; a aventura e o mapa-múndi usam a cena principal.

Nas fases, vidas e mundo aparecem à esquerda, o item reserva vira um botão à direita e a pontuação, moedas estrela e progresso ficam na faixa inferior. Moedas comuns e tempo permanecem sobre a fase. As informações são recortadas da saída ao vivo da ROM e recompostas pelo frontend; não são valores inventados.

**M / Mapa** abre um painel de PC e pausa a simulação: na fase ele mostra o progresso e uma prévia do último mapa visitado; no mapa-múndi mostra a rota e oito botões de seleção de mundo que respeitam os desbloqueios do jogo original. Esc ou M fecha o painel. A prévia do mapa é histórica, e não um mapa atualizado do interior da fase. Configurações oferece filtro de pixels, volume e tela cheia.

Esc e perda de foco congelam a simulação. As teclas mantidas durante a pausa são liberadas, exigindo uma nova pressão ao retomar. Sair normalmente cria um snapshot e mantém uma cópia do anterior. O save de cartucho continua sob responsabilidade do jogo original; use suas opções de salvar. **Iniciar pelo menu original** ignora o snapshot e permite usar esse save.

## Limites da alpha

O foco é a campanha para um jogador. Multiplayer, minigames e a campanha inteira não estão certificados. Não há aumento da resolução interna 256×192, novo campo de visão ou mudanças na física. O aplicativo não tem assinatura digital; políticas locais do Windows podem impedir sua execução. Consulte [RELEASE_NOTES.md](RELEASE_NOTES.md) para separar os testes realizados dos recursos ainda experimentais.

## Compilar

Requer Windows x64, .NET SDK 9, Python 3.10+, Git, PowerShell 7 e uma toolchain MinGW-w64 com GNU Make. A CI usa w64devkit **2.11.0**. Não precisa de ROM para compilar ou testar a interface.

```powershell
git clone --recurse-submodules https://github.com/GenissonEmilio/NewSuperMarioBrosPC.git
cd NewSuperMarioBrosPC
./scripts/build-core.ps1 -ToolchainPath C:/tools/w64devkit
dotnet build src/MarioPC -c Release
dotnet src/MarioPC/bin/Release/net9.0-windows/MarioPC.dll --self-test local/self-test
./scripts/package-windows.ps1 -ToolchainPath C:/tools/w64devkit
```

Para executar a build sem publish, copie `core/melonds/melonds_libretro.dll` para a pasta de saída do frontend. Para testes locais com sua própria ROM, o modo `--verify ROM PASTA [SCRIPT.json]` usa entrada interna e não abre janelas. `scripts/verify-rom.py` permite testar o núcleo separadamente com Python/Pillow. Resultados contêm dados privados do jogo e **não devem ser publicados**. A CI pública nunca recebe ROM.

O pacote `*-source.zip` da release contém o frontend e a árvore completa do núcleo fixada na revisão `66b5d2634cd0a79030562811e6e05f5532f800ba`, inclusive dependências incorporadas. O script de build também funciona com essa árvore extraída, sem submódulos.

## Créditos e licença

Interface e adaptação de PC: GenissonEmilio e colaboradores. Núcleo: [melonDS e seus autores](https://github.com/libretro/melonDS). Código distribuído sob **GPL-3.0-or-later**; consulte [LICENSE](LICENSE) e [THIRD-PARTY.md](THIRD-PARTY.md). As licenças de componentes incorporados são preservadas no código-fonte correspondente.

O projeto [NSMB-Decomp](https://github.com/NSMB-Decomp/nsmb) foi consultado para pesquisar os identificadores de cenas da versão A2DE; seu código não foi incorporado. A decompilação ainda não produz uma ROM completa. New Super Mario Bros., Mario e Nintendo pertencem aos respectivos titulares. Projeto de fãs, sem afiliação com a Nintendo.
