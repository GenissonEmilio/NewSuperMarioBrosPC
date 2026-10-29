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

Os resultados finais de compilação, testes da interface e testes locais do núcleo serão registrados na publicação da release. Os testes públicos não recebem ROM. A política de Controle de Aplicativos da máquina de desenvolvimento bloqueou a DLL gerenciada; os testes da interface são executados no Windows do GitHub Actions, e a ROM é testada localmente no núcleo por uma ferramenta independente. Isso não equivale a um teste completo da janela C# com a ROM.

Binários sem assinatura digital. Algumas políticas do Windows podem impedir sua execução. Nenhuma proteção do sistema é alterada pelo aplicativo ou pelos scripts.
