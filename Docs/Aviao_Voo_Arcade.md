# Avião: embarque e voo arcade

16 outubro 2026. Integração pelo plugin oficial Unity CLI/Pipeline.

Abrir `Assets/_FirstJourney/Scenes/FirstJourney_EspacoExterior.unity`, carregar em Play e atravessar a ponte até ao avião. Aproximar-se pelo lado de trás da asa esquerda e usar **E / Quadrado**. O piloto fica sentado no cockpit com o clip Humanoid `PilotIdle` já existente.

## Controlos de voo

| Ação | Teclado / rato | PS5 |
| --- | --- | --- |
| Entrar / sair, em terra | E | Quadrado |
| Acelerar | W | R2 |
| Travar até pairar | S | L2 |
| Virar | A / D | Analógico esquerdo, horizontal |
| Subir / descolar | Espaço | Analógico esquerdo para cima, ou X |
| Descer | Ctrl | Analógico esquerdo para baixo |
| Olhar à volta | Botão direito + rato | Analógico direito |
| Aterragem assistida / cancelar | G | Círculo |

O acelerador conserva a velocidade quando é largado. Travar reduz a velocidade até zero; a sustentação assistida permite pairar para orientar e aproximar. Acelerar a partir do estacionamento inicia uma subida curta antes de avançar, para sair das pequenas ilhas. Não é uma simulação aerodinâmica: não há perda de sustentação, combustível ou gravidade orbital.

Seguir o marcador **Asteroide 01**. Aproximar-se a menos de 19 metros na horizontal e entre -1 e 18 metros acima do ponto de estacionamento. Reduzir a velocidade para **7 m/s ou menos (25 km/h)**. O aviso **G / Círculo · Aterrar** aparece quando a aproximação é válida. A assistência sobe/desce até à cota de aproximação, alinha sobre o estacionamento e desce até às rodas assentarem. G / Círculo cancela a assistência. A viagem entre as luas é pilotada pelo jogador; a assistência cobre apenas a aproximação final.

Depois de aterrar, E / Quadrado repõe o personagem no chão. Contornar a traseira do avião para alcançar a porta da casa. A porta leva ao Fantasy Kingdom com as três regiões existentes. Ao regressar por uma das casas desse mundo, o avião continua estacionado no asteroide. Entrar novamente, descolar e seguir **Lua-base** para regressar.

## Integração

- `FJAircraftController` no avião original, com 38 renderers preservados. Estados estacionado, voo e aproximação; velocidade máxima 16 m/s, sustentação assistida e verificações de colisão por varrimentos de esferas em fuselagem, extremidades, nariz, cauda e rodas. Um obstáculo corta a velocidade; a aproximação bloqueada é cancelada.
- `FJAircraftCamera` usa a mesma câmara e listener da sessão. O controlo orbitável e o motor a pé ficam desativados durante a pilotagem; a cápsula do personagem também. O personagem conserva a sua cena e não é colocado sob um avião com física ativa. A pose acompanha o assento pelo osso da bacia.
- A hélice original roda; os assets fonte e o Animator existente são preservados.
- `FJLandingSite` define estacionamento e saída em cada lua. Base: `(24.400,0.247,4.608)`, rumo 0°. Asteroide: `(3.300,0.025,183.100)`, rumo 90°. O avião fica ao lado da casa para deixar o acesso à porta livre. Rodas principais sobre superfície plana, sem sobreposição com a casa.
- Embarque exige proximidade, personagem no chão e avião estacionado. Desembarque exige estacionamento, velocidade zero, chão caminhável e cápsula livre de obstáculos. Não permite sair no vazio ou em voo.
- `FJInteractionInput` impede que a mesma pressão abra uma porta e embarque/desembarque no mesmo frame. Uma porta mais próxima conserva prioridade no chão. Portas não interagem com o personagem sentado.
- `FJWorldSession` conserva a posição do avião durante a visita ao mundo, dentro da mesma sessão. Reiniciar Play repõe a montagem inicial; não foi acrescentada gravação em disco.

## Verificação

Relatório do percurso: `Docs/Aviao_Playtest.json`. Montagem e apoio das rodas: `Docs/Aviao_Instalacao.json`. Inspeção final: `Docs/Aviao_Validacao.json`.

**Percurso aprovado:** caminhada desde a base até ao avião, embarque com E, descolagem, cerca de 198 m de voo até ao asteroide, aterragem, desembarque, caminhada até à porta, visita ao reino, regresso ao avião estacionado, embarque com Quadrado e voo de volta à base. Distância total de voo: **397,17 m**. Mesmos personagem, avião, câmara e listener, um único motor a pé no fim e **zero reposições inesperadas**.

Também passaram os casos negativos: embarque à distância recusado, saída em voo recusada, saída sem piso recusada e bloqueio antes de um obstáculo temporário colocado no caminho do voo. As quatro paragens por obstáculo registadas pertencem a esse ensaio deliberado, que manteve o acelerador premido durante alguns frames contra a barreira; a barreira foi removida no fim do ensaio.

O teste usa dispositivos temporários do Input System, com os dispositivos físicos isolados e restaurados no fim. A ida é pilotada por teclado virtual; a volta por comando virtual. O avião não é deslocado por teletransporte no teste. A aproximação à porta de regresso dentro do Fantasy Kingdom é colocada por código para isolar a transição; o percurso completo no interior pertence à revisão do mundo.

As capturas `Aviao_Piloto_Cockpit.png`, `Aviao_Voo_Teclado.png`, `Aviao_Aterrado_Asteroide.png` e `Aviao_Voo_Comando.png` são da janela Game real. A validação do comando físico com o criador e uma build independente continuam pendentes. Os ajustes de salto/colisão a pé já adiados pelo criador não foram incluídos nesta mecânica.

Inspeção final fora de Play: cenas guardadas, duas zonas de aterragem, 38 peças do avião e 584 renderers da arte original, zero scripts/materiais ausentes, uma câmara/motor/listener e zero erros de Console ou falha de compilação. Permanece um aviso do Unity AI Account API. As três cenas standalone anteriores conservam os mesmos SHA-256.

Autoria: `AgentScripts/AircraftSetup.cs`, `AircraftPlaytest.cs` e `AircraftReview.cs`. Guardado localmente, sem novo envio ao GitHub.
