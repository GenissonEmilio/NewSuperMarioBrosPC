# v0.1.0-alpha.1 — Windows x64

Primeira alpha da adaptação de New Super Mario Bros. para PC, **baseada em emulação melonDS/libretro**.

- WASD para movimento, Espaço para pular, Shift para correr/disparar e E para item reserva.
- Aba Controles com remapeamento persistente e troca de conflitos.
- Uma área principal com HUD recomposto: vidas, moedas, pontuação e item reserva juntos à fase, sem alternância de telas.
- M / botão Mapa abre um painel de progresso e prévia do mapa visitado, com seleção de mundos pelo menu PC no mapa-múndi.
- Pausa PC, pausa ao perder foco, filtros, volume e tela cheia.
- Importação local de .nds/ZIP A2DE revisão 0, save original e snapshot ao sair.
- Runtime .NET, núcleo compilado, licenças e fontes correspondentes disponíveis.

Não inclui jogo, dumps de BIOS/firmware da Nintendo, saves ou recursos extraídos. Não altera a ROM fornecida. A campanha inteira, multiplayer e minigames não estão certificados. Resolução interna original e proporção 4:3 preservadas. Não é port nativo.

## Validação

Os testes públicos não recebem a ROM comercial. A CI do Windows verifica os controles, persistência, menus, layout e botões do mapa, integração C# / libretro com uma ROM sintética original, snapshots e integridade do pacote. Os layouts são inspecionados também em 4:3, 16:10 e 16:9. A release inclui fontes correspondentes e checksums.

Localmente, um replay invisível com a ROM A2DE revisão 0 passou pela introdução, seleção de arquivo, mapa-múndi e fase 1-1; verificou movimento para os dois lados, salto, aterrissagem, coleta de moeda, geração de áudio e restauração da RAM via snapshot. As imagens, saves e a ROM usados nesses testes ficam somente na máquina local.

A política de Controle de Aplicativos da máquina de desenvolvimento bloqueou a DLL gerenciada; os testes da interface são executados no Windows do GitHub Actions, e a ROM é testada localmente no núcleo por uma ferramenta independente. **Isso não equivale a um teste completo da janela C# com a ROM comercial**, nem a uma certificação da campanha inteira. O consumo do item reserva com um item disponível e a troca para mundos ainda bloqueados não foram certificados ponta a ponta; as ações usam as entradas táteis do jogo original, sem liberar conteúdo por código.

Binários sem assinatura digital. Algumas políticas do Windows podem impedir sua execução. Nenhuma proteção do sistema é alterada pelo aplicativo ou pelos scripts.
